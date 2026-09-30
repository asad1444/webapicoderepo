using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using CompanyModel = SmartProManWebAPI.Models.Company;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/[controller]")]
    [ApiController]
    public class LiveOperatingRoomController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Bid window duration — 2 minutes
        private const int BidWindowMinutes = 2;

        public LiveOperatingRoomController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/LiveOperatingRoom/requests
        //
        // Returns open Requests visible to this company based on zone level.
        // Also auto-expands zone if bid window has expired.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("requests")]
        public async Task<IActionResult> GetBroadcastedRequests(int companyId)
        {
            CompanyModel? company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            // Auto-expand zones for expired bid windows before returning
            await ExpandExpiredBidZones();

            // Load all open requests and requests awarded/assigned/completed to this company
            var openRequests = await _context.Requests
                .Include(r => r.Job)
                .Where(r => r.Status == "Open" || r.Status == null || (r.CompanyID == companyId && (r.Status == "Awarded" || r.Status == "Assigned" || r.Status == "InProgress" || r.Status == "Completed")))
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var visibleRequests = openRequests
                .Select(r =>
                {
                    var bidWindowOpen  = r.BidOpenedAt ?? r.Date;
                    var minutesElapsed = (DateTime.Now - bidWindowOpen).TotalMinutes;
                    var minutesLeft    = Math.Max(0, BidWindowMinutes - minutesElapsed);

                    return new
                    {
                        r.RequestID,
                        r.ClientRequestNumber,
                        r.Category,
                        r.ClientRequest,
                        r.KindOf,
                        r.Date,
                        r.Time,
                        r.ClientName,
                        r.ClientContact,
                        r.Zone,
                        r.Status,
                        AssignedTechId   = r.Job?.TechnicianID,
                        BidOpenedAt      = bidWindowOpen,
                        BidWindowMinutes = BidWindowMinutes,
                        MinutesElapsed   = Math.Round(minutesElapsed, 1),
                        MinutesRemaining = Math.Round(minutesLeft, 1),
                        BidWindowExpired = minutesLeft <= 0,
                        ZoneLevel        = r.BroadcastZoneLevel,
                        ZoneLevelLabel   = r.BroadcastZoneLevel switch
                        {
                            0 => $"Zone: {r.Zone}",
                            1 => $"Zone: {r.Zone} (expanded)",
                            _ => "All Zones (expanded)"
                        }
                    };
                })
                .ToList();

            return Ok(new
            {
                CompanyZone  = company.Zone,
                TotalVisible = visibleRequests.Count,
                Requests     = visibleRequests
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/Company/{companyId}/LiveOperatingRoom/bid/{requestId}
        //
        // Submit ETA bid for a Request.
        // Auto-award logic:
        //   - If 5 bids received → lowest ETA wins immediately
        //   - If bid window expires (15 min) → lowest ETA among existing bids wins
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("bid/{requestId}")]
        public async Task<IActionResult> SubmitBid(int companyId, int requestId, [FromBody] int estimatedArrivalMinutes)
        {
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.RequestID == requestId);

            if (request == null || (request.Status != "Open" && request.Status != null))
                return BadRequest(new { message = "Request is not available for bidding." });

            // Validate company exists
            CompanyModel? company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            // Zone check hata diya — jo request dikh rahi hai uspe bid kar sakte hain

            // No duplicate bids
            var existingBid = await _context.DispatchBids
                .FirstOrDefaultAsync(b => b.RequestID == requestId && b.CompanyID == companyId);
            if (existingBid != null)
                return BadRequest(new { message = "You have already submitted a bid for this request." });

            // ETA must be positive
            if (estimatedArrivalMinutes <= 0)
                return BadRequest(new { message = "Estimated arrival must be greater than 0 minutes." });

            // Validate ETA against Google Maps travel time
            if (!string.IsNullOrEmpty(request.ServiceAddress) && !string.IsNullOrEmpty(company.City))
            {
                try
                {
                    string apiKey = "AIzaSyAqLcduzu_SAbsSPO-SNdind1OxSHgfZqs";
                    string origin = Uri.EscapeDataString($"{company.City}, Pakistan");
                    string destination = Uri.EscapeDataString($"{request.ServiceAddress}, Karachi, Pakistan");
                    string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&departure_time=now&key={apiKey}";
                    
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var response = await httpClient.GetStringAsync(url);
                        using (var doc = System.Text.Json.JsonDocument.Parse(response))
                        {
                            var status = doc.RootElement.GetProperty("status").GetString();
                            if (status == "OK")
                            {
                                var routes = doc.RootElement.GetProperty("routes");
                                if (routes.GetArrayLength() > 0)
                                {
                                    var legs = routes[0].GetProperty("legs");
                                    if (legs.GetArrayLength() > 0)
                                    {
                                        int durationSeconds = 0;
                                        if (legs[0].TryGetProperty("duration_in_traffic", out var trafficProp))
                                            durationSeconds = trafficProp.GetProperty("value").GetInt32();
                                        else
                                            durationSeconds = legs[0].GetProperty("duration").GetProperty("value").GetInt32();
                                            
                                        int durationMinutes = durationSeconds / 60;
                                        if (estimatedArrivalMinutes < durationMinutes)
                                        {
                                            return BadRequest(new { message = $"Estimated arrival time is unrealistic. The minimum travel time is approximately {durationMinutes} minutes." });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Directions API error: {ex.Message}");
                }
            }

            // Ensure request has a BidOpenedAt if it doesn't already
            var existingBidsCount = await _context.DispatchBids.CountAsync(b => b.RequestID == requestId);
            
            // Set bid window start time ONLY on the first bid
            if (existingBidsCount == 0)
            {
                request.BidOpenedAt = DateTime.Now;
            }

            var bid = new DispatchBid
            {
                RequestID        = requestId,
                CompanyID        = companyId,
                EstimatedArrival = estimatedArrivalMinutes,
                BidTime          = DateTime.Now,
                Status           = "Pending"
            };

            _context.DispatchBids.Add(bid);
            await _context.SaveChangesAsync();

            // Check bid count — auto-award at 5 bids
            var totalBids = existingBidsCount + 1;
            bool autoAwarded = false;

            // Agar 5 bids aa gayi ya bid window expire ho chuki hai to auto award karo
            if (totalBids >= 5)
                autoAwarded = await AwardToFastestBidder(requestId);
            else if (request.BidOpenedAt != null)
            {
                // Check agar 2 min ka window expire ho chuka hai (unlikely on first bid)
                var minutesElapsed = (DateTime.Now - request.BidOpenedAt.Value).TotalMinutes;
                if (minutesElapsed >= BidWindowMinutes)
                    autoAwarded = await AwardToFastestBidder(requestId);
            }

            return Ok(new
            {
                message     = autoAwarded
                                  ? "Bid submitted. Request auto-awarded to fastest company!"
                                  : "Bid submitted successfully. Waiting for other bids (2 minute window).",
                BidID       = bid.BidID,
                TotalBids   = totalBids,
                AutoAwarded = autoAwarded,
                YourETA     = estimatedArrivalMinutes
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/Company/{companyId}/LiveOperatingRoom/check-bid-window/{requestId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("check-bid-window/{requestId}")]
        public async Task<IActionResult> CheckBidWindow(int companyId, int requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();
            if (request.Status != "Open")
                return Ok(new { message = "Request is no longer open.", Status = request.Status });

            var bidWindowOpen  = request.BidOpenedAt ?? request.Date;
            var minutesElapsed = (DateTime.Now - bidWindowOpen).TotalMinutes;

            // Bid window not expired yet
            if (minutesElapsed < BidWindowMinutes)
            {
                return Ok(new
                {
                    message          = "Bid window still open.",
                    MinutesRemaining = Math.Round(BidWindowMinutes - minutesElapsed, 1),
                    ZoneLevel        = request.BroadcastZoneLevel
                });
            }

            var bidCount = await _context.DispatchBids.CountAsync(b => b.RequestID == requestId);

            if (bidCount > 0)
            {
                bool awarded = await AwardToFastestBidder(requestId);
                return Ok(new
                {
                    message = awarded
                                  ? "Bid window expired. Request awarded to fastest bidder."
                                  : "Award failed.",
                    Awarded = awarded
                });
            }
            else
            {
                string zoneMsg = await ExpandRequestZone(request);
                return Ok(new
                {
                    message   = zoneMsg,
                    ZoneLevel = request.BroadcastZoneLevel
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/LiveOperatingRoom/won-requests
        //
        // Returns requests awarded to this company — awaiting technician assignment
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("won-requests")]
        public async Task<IActionResult> GetWonRequests(int companyId)
        {
            var wonRequests = await _context.Requests
                .Where(r => r.CompanyID == companyId && (r.Status == "Awarded" || r.Status == "Assigned" || r.Status == "InProgress" || r.Status == "Completed"))
                .OrderByDescending(r => r.Date)
                .Select(r => new
                {
                    r.RequestID,
                    r.ClientRequestNumber,
                    r.Category,
                    r.KindOf,
                    r.ClientRequest,
                    r.ClientName,
                    r.ClientContact,
                    r.Zone,
                    r.Date,
                    r.Time
                })
                .ToListAsync();

            return Ok(new { TotalWon = wonRequests.Count, Requests = wonRequests });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/LiveOperatingRoom/my-bids
        //
        // Returns requestIDs jahan is company ne bid ki hai (Pending status)
        // Frontend refresh ke baad submittedBids restore karne ke liye
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("my-bids")]
        public async Task<IActionResult> GetMyBids(int companyId)
        {
            var biddedRequestIds = await _context.DispatchBids
                .Where(b => b.CompanyID == companyId && b.Status == "Pending")
                .Select(b => b.RequestID)
                .ToListAsync();

            return Ok(new { BiddedRequestIds = biddedRequestIds });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/LiveOperatingRoom/technicians
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("technicians")]
        public async Task<IActionResult> GetCompanyTechnicians(int companyId)
        {
            var technicians = await _context.Technicians
                .Where(t => t.CompanyID == companyId &&
                            (t.ApprovalStatus == "Approved" || t.ApprovalStatus == "Active"))
                .Select(t => new
                {
                    t.TechnicianID,
                    t.FullName,
                    TechCode  = "TECH-" + t.TechnicianID.ToString(),
                    t.Designation,
                    t.Phone,
                    t.Photo,
                    t.LiveStatus,
                    TotalJobs = _context.Jobs.Count(j => j.TechnicianID == t.TechnicianID && j.Status == "Completed")
                })
                .ToListAsync();

            return Ok(technicians);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/Company/{companyId}/LiveOperatingRoom/assign-technician/{requestId}
        //
        // Assign a technician to a won request.
        // THIS creates the Job record in the Jobs table.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("assign-technician/{requestId}")]
        public async Task<IActionResult> AssignTechnician(int companyId, int requestId, [FromBody] int technicianId)
        {
            // Request must exist and be awarded to this company
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.RequestID == requestId && r.CompanyID == companyId);
            if (request == null)
                return NotFound(new { message = "Request not found or not awarded to your company." });

            if (request.Status != "Awarded" && request.Status != "Assigned")
                return BadRequest(new { message = "Request is not in Awarded or Assigned status." });

            // Technician must belong to this company and be online
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);
            if (technician == null)
                return BadRequest(new { message = "Invalid technician or not from your company." });

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.RequestID == requestId);
            var masterCard = job != null ? await _context.MasterCards.FirstOrDefaultAsync(m => m.JobID == job.JobID) : null;
            
            bool isReassignment = job != null;

            if (isReassignment && job.TechnicianID == technicianId)
            {
                return BadRequest(new { message = "This job is already assigned to this technician." });
            }

            if (!isReassignment)
            {
                // ── Create Job record from Request data ───────────────────────
                job = new Job
                {
                    // Request se link karo — yahi primary source hai
                    RequestID        = request.RequestID,

                    // Request se data copy karo
                    ClientRequest    = request.ClientRequest,
                    Category         = request.Category,
                    IssueDescription = request.KindOf,
                    CRNumber         = request.ClientRequestNumber,
                    CRID             = request.ClientRequestNumber != null
                                        ? request.ClientRequestNumber.Replace("REQ-", "CR-")
                                        : $"CR-{request.RequestID}",

                    // Assign karo
                    CompanyID    = companyId,
                    TechnicianID = technicianId,
                    AssignmentSource = "Company",
                    Status       = "In Progress",
                    CreatedAt    = request.Date,
                    AssignedAt   = DateTime.Now,

                    // ClientID create karo agar nahi hai
                    ClientID     = await GetOrCreateClientId(request.ClientContact, request.ClientName, request.Zone, request.ServiceAddress)
                };

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync(); // JobID generate hoga

                // ── MasterCard create karo ────────────────────────────────────
                masterCard = new MasterCard
                {
                    JobID        = job.JobID,
                    ClientID     = job.ClientID ?? 0,
                    CompanyID    = companyId,
                    TechnicianID = technicianId,
                    Status       = "In Progress",
                    CreatedAt    = DateTime.Now
                };
                _context.MasterCards.Add(masterCard);

                // ── Job Tracking start karo ───────────────────────────────────
                _context.JobTracking.Add(new JobTracking
                {
                    JobID       = job.JobID,
                    CurrentStep = "Assigned",
                    ETA         = "Pending",
                    LastUpdated = DateTime.Now
                });
            }
            else
            {
                // ── Reassign existing Job and MasterCard ───────────────────────
                job.TechnicianID = technicianId;
                job.AssignedAt = DateTime.Now;

                if (masterCard != null)
                {
                    masterCard.TechnicianID = technicianId;
                }
            }

            _context.Notifications.Add(new Notification
            {
                TechnicianId = technicianId,
                Title = isReassignment ? "Job Re-assigned" : "New Job Assigned",
                Message = $"You have been assigned a job ({job.CRNumber ?? $"MCID-{job.JobID:D3}"}). Please check your dashboard.",
                RelatedModule = "Jobs",
                RelatedRecordId = job.JobID,
                Date = DateTime.Now,
                IsRead = false
            });

            // ── Request update karo ───────────────────────────────────────
            request.Status = "Assigned";
            request.JobID  = job.JobID;

            // ── DispatchBid mein JobID set karo (winning bid) ─────────────
            var winningBid = await _context.DispatchBids
                .FirstOrDefaultAsync(b => b.RequestID == requestId && b.IsWinner == true);
            if (winningBid != null)
                winningBid.JobID = job.JobID;

            // ── Technician status update ──────────────────────────────────
            technician.LiveStatus = "On a Job";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Database save error: {inner}" });
            }

            return Ok(new
            {
                message          = isReassignment ? "Technician re-assigned successfully." : "Technician assigned. Job created successfully.",
                JobID            = job.JobID,
                RequestID        = requestId,
                TechnicianID     = technicianId,
                TechnicianName   = technician.FullName,
                MasterCardID     = masterCard?.MasterCardID ?? 0
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/LiveOperatingRoom/bid-status/{requestId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("bid-status/{requestId}")]
        public async Task<IActionResult> GetBidStatus(int companyId, int requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null) return NotFound();

            var bids = await _context.DispatchBids
                .Include(b => b.Company)
                .Where(b => b.RequestID == requestId)
                .OrderBy(b => b.EstimatedArrival)
                .ThenBy(b => b.BidTime)
                .Select(b => new
                {
                    b.BidID,
                    b.CompanyID,
                    CompanyName    = b.Company.CompanyName,
                    b.EstimatedArrival,
                    b.BidTime,
                    b.IsWinner,
                    b.Status,
                    IsYou          = b.CompanyID == companyId
                })
                .ToListAsync();

            var bidWindowOpen  = request.BidOpenedAt ?? request.Date;
            var minutesElapsed = (DateTime.Now - bidWindowOpen).TotalMinutes;

            return Ok(new
            {
                RequestID        = requestId,
                RequestStatus    = request.Status,
                TotalBids        = bids.Count,
                MinutesRemaining = Math.Round(Math.Max(0, BidWindowMinutes - minutesElapsed), 1),
                BidWindowExpired = minutesElapsed >= BidWindowMinutes,
                CurrentLeader    = bids.FirstOrDefault()?.CompanyName,
                ZoneLevel        = request.BroadcastZoneLevel,
                Bids             = bids
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Award request to company with lowest EstimatedArrival.
        /// Tie-breaker: earliest BidTime.
        /// </summary>
        private async Task<bool> AwardToFastestBidder(int requestId)
        {
            var request = await _context.Requests.FindAsync(requestId);
            if (request == null || request.Status != "Open") return false;

            var winningBid = await _context.DispatchBids
                .Where(b => b.RequestID == requestId && b.Status == "Pending")
                .OrderBy(b => b.EstimatedArrival)  // fastest ETA first
                .ThenBy(b => b.BidTime)             // tie-breaker: earliest bid
                .FirstOrDefaultAsync();

            if (winningBid == null) return false;

            // Mark winner
            winningBid.IsWinner = true;
            winningBid.Status   = "Accepted";

            // Reject all other bids
            var losingBids = await _context.DispatchBids
                .Where(b => b.RequestID == requestId && b.BidID != winningBid.BidID)
                .ToListAsync();
            losingBids.ForEach(b => b.Status = "Rejected");

            // Request goes to winning company — awaiting technician assignment
            request.CompanyID = winningBid.CompanyID;
            request.Status    = "Awarded";

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Expand zone level if no bids received in 15 minutes.
        /// </summary>
        private async Task<string> ExpandRequestZone(Request request)
        {
            if (request.BroadcastZoneLevel >= 2)
                return "Zone already at maximum broadcast level (all zones).";

            request.BroadcastZoneLevel++;
            request.BidOpenedAt = DateTime.Now; // reset 15-min window

            await _context.SaveChangesAsync();

            return request.BroadcastZoneLevel == 1
                ? "No bids in zone. Broadcast expanded to wider area. New 15-min window started."
                : "No bids in area. Broadcast expanded to ALL zones. New 15-min window started.";
        }

        /// <summary>
        /// Auto-expand zones for all open requests whose bid window expired.
        /// Called on GET so zone expansion happens on-demand.
        /// </summary>
        private async Task ExpandExpiredBidZones()
        {
            var openRequests = await _context.Requests
                .Where(r => r.Status == "Open" || r.Status == null)
                .ToListAsync();

            foreach (var request in openRequests)
            {
                var bidWindowOpen  = request.BidOpenedAt ?? request.Date;
                var minutesElapsed = (DateTime.Now - bidWindowOpen).TotalMinutes;
                if (minutesElapsed < BidWindowMinutes) continue;

                var hasBids = await _context.DispatchBids.AnyAsync(b => b.RequestID == request.RequestID);
                if (!hasBids)
                {
                    if (request.BroadcastZoneLevel < 2)
                        await ExpandRequestZone(request);
                }
                else
                {
                    await AwardToFastestBidder(request.RequestID);
                }
            }
        }

        /// <summary>
        /// Check if a request is visible to a company based on zone broadcast level.
        /// Level 0 → exact zone match (Request.Zone == Company.Zone)
        /// Level 1 → zone OR city match (expanded)
        /// Level 2 → all companies see it
        /// </summary>
        private static bool IsRequestVisibleToCompany(Request request, CompanyModel company)
        {
            // If company has no zone set, show all requests
            if (string.IsNullOrEmpty(company.Zone))
                return true;

            return request.BroadcastZoneLevel switch
            {
                0 => string.Equals(request.Zone, company.Zone, StringComparison.OrdinalIgnoreCase),
                1 => string.Equals(request.Zone, company.Zone, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(request.Zone, company.City, StringComparison.OrdinalIgnoreCase),
                _ => true   // Level 2 = all zones
            };
        }

        /// <summary>
        /// Try to find ClientID from ClientContact, or create a new Client.
        /// </summary>
        private async Task<int> GetOrCreateClientId(string clientContact, string clientName, string zone, string serviceAddress)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Phone == clientContact);
            if (client == null)
            {
                client = new Client
                {
                    ClientCode = $"C-{DateTime.Now:yyMMdd}-{new Random().Next(100, 999)}",
                    ClientName = string.IsNullOrWhiteSpace(clientName) ? "Unknown Client" : clientName,
                    Phone = clientContact ?? "Unknown",
                    ServeLocation = serviceAddress ?? "",
                    Zone = zone ?? "",
                    Logo = "",
                    Email = "",
                    Home = "",
                    Area = "",
                    Street = "",
                    Floor = "",
                    FloorZone = "",
                    TopZone = "",
                    Status = "Active"
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
            }
            return client.ClientID;
        }
    }
}
