using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;

namespace SmartProManWebAPI.Controllers.Technician
{
    [Route("api/technician/jobs")]
    [ApiController]
    [AllowAnonymous]
    public class TechnicianJobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TechnicianJobsController(AppDbContext context)
        {
            _context = context;
        }

        // ── Format job ID as MCID-001 ─────────────────────────────────────
        private static string FormatMcid(int id) => $"MCID-{id:D3}";

        // GET: api/technician/jobs/{technicianId}
        [HttpGet("{technicianId}")]
        public async Task<IActionResult> GetJobs(int technicianId, [FromQuery] string? status)
        {
            var query = _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .Where(j => j.TechnicianID == technicianId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "assigned")
                    query = query.Where(j => j.Status == "Assigned");
                else if (status.ToLower() == "inprogress" || status.ToLower() == "in_progress")
                    query = query.Where(j => j.Status == "In Progress" || j.Status == "On Route");
                else if (status.ToLower() == "completed")
                    query = query.Where(j => j.Status == "Completed");
            }

            var raw = await query
                .OrderByDescending(j => j.AssignedAt ?? j.CreatedAt)
                .Select(j => new
                {
                    id              = j.JobID,
                    customer_name   = j.Request != null ? j.Request.ClientName
                                    : j.Client  != null ? j.Client.ClientName
                                    : "Walk-in Client",
                    service_address = j.Request != null ? j.Request.ServiceAddress : null,
                    area            = j.Client  != null ? j.Client.Area   : null,
                    street          = j.Client  != null ? j.Client.Street : null,
                    service_type    = j.Request != null ? j.Request.Category    : j.Category,
                    client_request  = j.Request != null ? j.Request.ClientRequest : j.ClientRequest,
                    special_instructions = j.Request != null ? j.Request.SpecialInstructions : null,
                    contact         = j.Request != null ? j.Request.ClientContact : null,
                    zone            = j.Request != null ? j.Request.Zone    : null,
                    kind_of         = j.Request != null ? j.Request.KindOf  : j.IssueDescription,
                    priority        = j.Request != null ? j.Request.Priority : "Normal",
                    status          = j.Status,
                    category        = j.Category,
                    crn             = j.CRNumber,
                    // AssignedAt — when technician was assigned
                    assigned_at     = j.AssignedAt,
                    created_at      = j.CreatedAt,
                })
                .ToListAsync();

            var result = raw.Select(j => new
            {
                id           = j.id,
                mcid         = FormatMcid(j.id),           // MCID-001
                customer_name = j.customer_name ?? "Walk-in Client",
                address      = !string.IsNullOrWhiteSpace(j.service_address)
                                ? j.service_address
                                : !string.IsNullOrWhiteSpace($"{j.area} {j.street}".Trim())
                                    ? $"{j.area} {j.street}".Trim()
                                    : "N/A",
                service_type          = j.service_type ?? "General",
                client_request        = j.client_request ?? "",
                special_instructions  = j.special_instructions ?? "",
                contact               = j.contact ?? "",
                zone                  = j.zone ?? "",
                kind_of               = j.kind_of ?? "",
                priority              = j.priority ?? "Normal",
                status                = j.status ?? "Assigned",
                job_category         = j.category ?? "",
                // Date/time from AssignedAt (when assigned), fallback CreatedAt
                scheduled_date = (j.assigned_at ?? j.created_at).HasValue
                                    ? (j.assigned_at ?? j.created_at)!.Value.ToString("yyyy-MM-dd")
                                    : DateTime.Now.ToString("yyyy-MM-dd"),
                scheduled_time = (j.assigned_at ?? j.created_at).HasValue
                                    ? (j.assigned_at ?? j.created_at)!.Value.ToString("hh:mm tt")
                                    : "",
                crn          = j.crn ?? "",
            }).ToList();

            return Ok(new { data = result });
        }

        // GET: api/technician/jobs/detail/{jobId}
        [HttpGet("detail/{jobId}")]
        public async Task<IActionResult> GetJobDetail(int jobId)
        {
            var job = await _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            if (job == null) return NotFound("Job not found.");

            var req    = job.Request;
            var client = job.Client;

            string customerName  = req?.ClientName    ?? client?.ClientName ?? "Walk-in Client";
            string phone         = req?.ClientContact ?? client?.Phone ?? "";
            string address       = !string.IsNullOrWhiteSpace(req?.ServiceAddress)
                                    ? req!.ServiceAddress!
                                    : $"{client?.Area ?? ""} {client?.Street ?? ""}".Trim() is string a && a.Length > 0
                                        ? a : "N/A";

            decimal? lat = client?.Latitude;
            decimal? lng = client?.Longitude;

            if ((lat == null || lng == null) && address != "N/A")
            {
                try
                {
                    string apiKey = "AIzaSyAqLcduzu_SAbsSPO-SNdind1OxSHgfZqs";
                    string query = Uri.EscapeDataString(address + ", Karachi, Pakistan");
                    string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={query}&key={apiKey}";
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonStr = await response.Content.ReadAsStringAsync();
                            var json = System.Text.Json.JsonDocument.Parse(jsonStr);
                            if (json.RootElement.GetProperty("status").GetString() == "OK")
                            {
                                var location = json.RootElement.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
                                lat = location.GetProperty("lat").GetDecimal();
                                lng = location.GetProperty("lng").GetDecimal();
                                
                                if (client != null)
                                {
                                    client.Latitude = lat;
                                    client.Longitude = lng;
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore geocoding errors
                }
            }

            string serviceType   = req?.Category      ?? job.Category ?? "General";
            string complaintTitle = req?.ClientRequest ?? job.ClientRequest ?? "";
            string description   = req?.KindOf        ?? job.IssueDescription ?? "";
            string specialInstr  = req?.SpecialInstructions ?? "";
            string zone          = req?.Zone          ?? "";
            string priority      = req?.Priority      ?? "Normal";
            var    assignedAt    = job.AssignedAt ?? job.CreatedAt;

            return Ok(new
            {
                mcid                  = FormatMcid(job.JobID),   // MCID-001
                job_id                = job.JobID,
                request_id            = job.RequestID,
                customer_name         = customerName,
                // contact_person removed as requested
                phone                 = phone,
                address               = address,
                zone                  = zone,
                service_type          = serviceType,
                priority              = priority,
                status                = job.Status ?? "Assigned",
                latitude              = lat,
                longitude             = lng,
                // Auto date/time from AssignedAt
                scheduled_date        = assignedAt?.ToString("yyyy-MM-dd") ?? "",
                scheduled_time        = assignedAt?.ToString("hh:mm tt")   ?? "",
                complaint_title       = complaintTitle,
                complaint_description = description,
                special_instructions  = specialInstr,
                job_category          = serviceType,
                crn                   = job.CRNumber ?? ""
            });
        }

        // POST: api/technician/jobs/{jobId}/start
        [HttpPost("{jobId}/start")]
        public async Task<IActionResult> StartJob(int jobId)
        {
            var job = await _context.Jobs
                .Include(j => j.Technician)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            if (job == null) return NotFound();

            var tech = job.Technician;
            if (tech == null)
                return BadRequest(new { message = "Technician record not found for this job." });

            if (tech.LiveStatus == "Offline" || tech.LiveStatus == "Off Duty")
                return BadRequest(new { message = "You are currently off duty. Please switch to Online before starting this job." });

            if (job.Status == "Completed")
                return BadRequest(new { message = "This job is already completed." });

            job.Status = "In Progress";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Job started successfully." });
        }

        // POST: api/technician/jobs/{jobId}/on-way
        [HttpPost("{jobId}/on-way")]
        public async Task<IActionResult> OnWay(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();
            job.Status = "On Route";
            await _context.SaveChangesAsync();
            return Ok(new { message = "On the way." });
        }

        // POST: api/technician/jobs/{jobId}/arrived
        [HttpPost("{jobId}/arrived")]
        public async Task<IActionResult> Arrived(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();
            job.Status = "Arrived";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Arrived at location." });
        }

        // GET: api/technician/jobs/proxy-directions
        [HttpGet("proxy-directions")]
        public async Task<IActionResult> ProxyDirections([FromQuery] double originLat, [FromQuery] double originLng, [FromQuery] double destLat, [FromQuery] double destLng)
        {
            string apiKey = "AIzaSyAqLcduzu_SAbsSPO-SNdind1OxSHgfZqs";
            string oLat = originLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string oLng = originLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string dLat = destLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string dLng = destLng.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // departure_time=now is required to get traffic data and duration_in_traffic
            string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={oLat},{oLng}&destination={dLat},{dLng}&departure_time=now&key={apiKey}";
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
        }
    }
}
