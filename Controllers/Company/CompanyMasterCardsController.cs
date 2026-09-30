using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/MasterCards")]
    [ApiController]
    public class CompanyMasterCardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyMasterCardsController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/MasterCards/{masterCardId}
        // Full MasterCard detail with structured job tracker timeline
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{masterCardId}")]
        public async Task<IActionResult> GetMasterCard(int companyId, int masterCardId)
        {
            var masterCard = await _context.MasterCards
                .Include(m => m.Client)
                .Include(m => m.Technician)
                .Include(m => m.Job)
                .FirstOrDefaultAsync(m => m.MasterCardID == masterCardId && m.CompanyID == companyId);

            if (masterCard == null)
                return NotFound(new { message = "Master Card not found or unauthorized access" });

            var tracking = await _context.JobTracking
                .FirstOrDefaultAsync(t => t.JobID == masterCard.JobID);

            // ── Build structured tracker timeline ─────────────────────────────
            // Each step: StepName, Time (null = not done yet), IsDone, IsCurrent
            var steps = BuildTrackerSteps(tracking, masterCard.Status);

            return Ok(new
            {
                masterCard.MasterCardID,
                masterCard.JobID,
                CRNumber   = masterCard.Job?.CRNumber,
                JobStatus  = masterCard.Status,
                CreatedAt  = masterCard.CreatedAt,

                Client = new
                {
                    masterCard.Client.ClientID,
                    masterCard.Client.ClientName,
                    masterCard.Client.Phone,
                    masterCard.Client.Area,
                    masterCard.Client.Zone
                },

                Technician = new
                {
                    masterCard.Technician.TechnicianID,
                    TechCode  = $"TECH-{masterCard.Technician.TechnicianID}",
                    masterCard.Technician.FullName,
                    masterCard.Technician.Phone,
                    masterCard.Technician.Photo,
                    masterCard.Technician.LiveStatus
                },

                // ── Job Tracker Timeline ──────────────────────────────────────
                // Frontend loops over Steps array to render the tracker UI
                // IsDone = true  → green/checked
                // IsCurrent = true → highlighted/active
                // IsDone = false → grey/pending
                JobTracker = new
                {
                    CurrentStep = tracking?.CurrentStep ?? "Assigned",
                    ETA         = tracking?.ETA,
                    LastUpdated = tracking?.LastUpdated,
                    Steps       = steps
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/MasterCards/job/{jobId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetMasterCardByJob(int companyId, int jobId)
        {
            var masterCard = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.JobID == jobId && m.CompanyID == companyId);

            if (masterCard == null)
                return NotFound(new { message = "Master Card not found for this job" });

            return Ok(new { masterCard.MasterCardID });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/MasterCards/{masterCardId}/proforma-invoice
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{masterCardId}/proforma-invoice")]
        public async Task<IActionResult> GetProformaInvoice(int companyId, int masterCardId)
        {
            var isOwner = await _context.MasterCards
                .AnyAsync(m => m.MasterCardID == masterCardId && m.CompanyID == companyId);
            if (!isOwner)
                return Unauthorized(new { message = "Unauthorized access to this Master Card" });

            var invoice = await _context.ProformaInvoices
                .FirstOrDefaultAsync(p => p.MasterCardID == masterCardId);

            if (invoice == null)
                return NotFound(new { message = "Proforma Invoice not found" });

            var items = await _context.ProformaInvoiceItems
                .Where(i => i.InvoiceID == invoice.InvoiceID)
                .OrderBy(i => i.ItemID)
                .Select(i => new
                {
                    Sr         = 0,
                    i.ItemID,
                    i.Description,
                    i.Quantity,
                    i.Unit,
                    i.UnitPrice,
                    i.TotalPrice
                })
                .ToListAsync();

            var numberedItems = items
                .Select((item, index) => new
                {
                    Sr          = index + 1,
                    item.ItemID,
                    item.Description,
                    item.Quantity,
                    item.Unit,
                    item.UnitPrice,
                    item.TotalPrice
                }).ToList();

            return Ok(new
            {
                invoice.InvoiceID,
                invoice.Status,
                invoice.InvoiceDate,
                invoice.CircuitAHighPressure,
                invoice.CircuitALowPressure,
                invoice.CircuitAGT,
                invoice.CircuitART,
                invoice.CircuitAPower,
                invoice.CircuitAAmpere,
                invoice.CircuitBHighPressure,
                invoice.CircuitBLowPressure,
                invoice.CircuitBGT,
                invoice.CircuitBRT,
                invoice.CircuitBPower,
                invoice.CircuitBAmpere,
                QuoteItems    = numberedItems,
                invoice.SubTotal,
                invoice.GrandTotal,
                invoice.InvoiceDetails,
                invoice.VoucherNumber,
                invoice.VoucherType,
                invoice.VoucherExpiry,
                invoice.ChatBox,
                invoice.FinalNote,
                invoice.TechnicianPhoto
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/Company/{companyId}/MasterCards/{masterCardId}/proforma-invoice
        // Company side proforma save/update
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("{masterCardId}/proforma-invoice")]
        public async Task<IActionResult> SaveProformaInvoice(
            int companyId, int masterCardId, [FromBody] ProformaInvoice invoiceDto)
        {
            var masterCard = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.MasterCardID == masterCardId && m.CompanyID == companyId);

            if (masterCard == null)
                return Unauthorized(new { message = "Unauthorized access to this Master Card" });

            var existingInvoice = await _context.ProformaInvoices
                .FirstOrDefaultAsync(p => p.MasterCardID == masterCardId);

            if (existingInvoice != null)
            {
                existingInvoice.IncidentDetails = invoiceDto.IncidentDetails;
                existingInvoice.SubTotal        = invoiceDto.SubTotal;
                existingInvoice.Tax             = invoiceDto.Tax;
                existingInvoice.Discount        = invoiceDto.Discount;
                existingInvoice.GrandTotal      = invoiceDto.GrandTotal;
                existingInvoice.Status          = invoiceDto.Status ?? existingInvoice.Status;
                existingInvoice.FinalNote       = invoiceDto.FinalNote;
                _context.ProformaInvoices.Update(existingInvoice);
            }
            else
            {
                invoiceDto.MasterCardID = masterCardId;
                invoiceDto.CreatedAt    = DateTime.Now;
                _context.ProformaInvoices.Add(invoiceDto);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Proforma Invoice saved successfully" });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/Company/{companyId}/MasterCards
        // All mastercards for this company with tracker current step
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllMasterCards(
            int companyId, [FromQuery] string? status = null)
        {
            // ── MasterCards with full data ────────────────────────────────
            var mcQuery = _context.MasterCards
                .Include(m => m.Client)
                .Include(m => m.Technician)
                .Include(m => m.Job)
                    .ThenInclude(j => j != null ? j.Request : null)
                .Where(m => m.CompanyID == companyId && m.Job.AssignmentSource == "Company")
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                mcQuery = mcQuery.Where(m => m.Status == status);

            var masterCards = await mcQuery
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var mcJobIds = masterCards.Select(m => m.JobID).ToHashSet();

            // ── Jobs without MasterCard ───────────────────────────────────
            var jobsQuery = _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .Include(j => j.Technician)
                .Where(j => j.CompanyID == companyId && !mcJobIds.Contains(j.JobID));

            if (!string.IsNullOrEmpty(status))
                jobsQuery = jobsQuery.Where(j => j.Status == status);

            var orphanJobs = await jobsQuery
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            // ── Map MasterCards ───────────────────────────────────────────
            var result = masterCards.Select(m => new
            {
                masterCardID   = m.MasterCardID,
                jobID          = m.JobID,
                jobCRN         = $"MCID-{m.MasterCardID:D3}",   // MCID-001 format
                clientName     = m.Job?.Request?.ClientName
                                 ?? m.Client?.ClientName
                                 ?? "Walk-in Client",
                clientContact  = m.Job?.Request?.ClientContact
                                 ?? m.Client?.Phone ?? "",
                clientArea     = m.Job?.Request?.ServiceAddress
                                 ?? m.Client?.Area ?? "",
                clientZone     = m.Job?.Request?.Zone
                                 ?? m.Client?.Zone ?? "",
                technicianId   = m.TechnicianID,
                technicianName = m.Technician?.FullName ?? "Assigned",
                technicianPhone = m.Technician?.Phone ?? "",
                status         = m.Status,
                createdAt      = m.CreatedAt,
                serviceType    = m.Job?.Category ?? "",
                priority       = m.Job?.Request?.Priority ?? "Normal",
                specialInstructions = m.Job?.Request?.SpecialInstructions ?? "",
                currentStep    = _context.JobTracking
                                    .Where(t => t.JobID == m.JobID)
                                    .Select(t => t.CurrentStep)
                                    .FirstOrDefault() ?? "Assigned"
            }).ToList<object>();

            // ── Map orphan Jobs (no MasterCard) ──────────────────────────
            var orphanResult = orphanJobs.Select(j => new
            {
                masterCardID   = 0,
                jobID          = j.JobID,
                jobCRN         = $"MCID-{j.JobID:D3}",          // MCID-001 format
                clientName     = j.Request?.ClientName
                                 ?? j.Client?.ClientName
                                 ?? "Walk-in Client",
                clientContact  = j.Request?.ClientContact
                                 ?? j.Client?.Phone ?? "",
                clientArea     = j.Request?.ServiceAddress
                                 ?? j.Client?.Area ?? "",
                clientZone     = j.Request?.Zone
                                 ?? j.Client?.Zone ?? "",
                technicianId   = j.TechnicianID,
                technicianName = j.Technician?.FullName ?? "Assigned",
                technicianPhone = j.Technician?.Phone ?? "",
                status         = j.Status,
                createdAt      = j.CreatedAt,
                serviceType    = j.Category ?? "",
                priority       = j.Request?.Priority ?? "Normal",
                specialInstructions = j.Request?.SpecialInstructions ?? "",
                currentStep    = "Assigned"
            }).ToList<object>();

            result.AddRange(orphanResult);

            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Build ordered tracker steps from JobTracking record
        // ─────────────────────────────────────────────────────────────────────
        private static List<object> BuildTrackerSteps(JobTracking? t, string jobStatus)
        {
            var normalizedCurrent = ResolveCurrentStep(t);
            var stepDefs = new[]
            {
                new { Key = "Assigned",         Label = "Job Assigned",         Time = (DateTime?)null },
                new { Key = "OnWay",            Label = "On the Way",           Time = t?.OnWayTime },
                new { Key = "UnitRegistration", Label = "Unit Registration",    Time = t?.UnitRegistrationTime },
                new { Key = "FIR",              Label = "FIR / Proforma",       Time = t?.FIRTime },
                new { Key = "Quote",            Label = "Quote / Invoice",      Time = t?.QuoteTime },
                new { Key = "FCR",              Label = "Payment Collected",    Time = t?.FCRTime },
                new { Key = "Completed",        Label = "Job Closed",           Time = t?.JobClosureTime }
            };

            var indexMap = stepDefs.Select((s, idx) => new { s.Key, idx }).ToDictionary(x => x.Key, x => x.idx);
            var currentIndex = indexMap.TryGetValue(normalizedCurrent, out var stepIndex) ? stepIndex : 0;

            var result = new List<object>();

            foreach (var step in stepDefs)
            {
                var stepIndexValue = indexMap[step.Key];
                var isDone = stepIndexValue < currentIndex || (stepIndexValue == currentIndex && step.Time.HasValue && step.Key != normalizedCurrent && !string.Equals(normalizedCurrent, "Assigned", StringComparison.OrdinalIgnoreCase));
                var isCurrent = step.Key == normalizedCurrent;

                result.Add(new
                {
                    StepKey    = step.Key,
                    StepLabel  = step.Label,
                    IsDone     = isDone,
                    IsCurrent  = isCurrent,
                    IsPending  = !isDone && !isCurrent,
                    CompletedAt = step.Time.HasValue
                                      ? step.Time.Value.ToString("dd MMM yyyy hh:mm tt")
                                      : (string?)null
                });
            }

            return result;
        }

        private static string ResolveCurrentStep(JobTracking? t)
        {
            if (t == null) return "Assigned";

            if (!string.IsNullOrWhiteSpace(t.CurrentStep))
            {
                var normalized = t.CurrentStep.Trim();
                if (normalized.Equals("OnWay", StringComparison.OrdinalIgnoreCase)) return "OnWay";
                if (normalized.Equals("UnitRegistration", StringComparison.OrdinalIgnoreCase)) return "UnitRegistration";
                if (normalized.Equals("FIR", StringComparison.OrdinalIgnoreCase) || normalized.Equals("Proforma", StringComparison.OrdinalIgnoreCase)) return "FIR";
                if (normalized.Equals("Quote", StringComparison.OrdinalIgnoreCase)) return "Quote";
                if (normalized.Equals("FCR", StringComparison.OrdinalIgnoreCase)) return "FCR";
                if (normalized.Equals("Completed", StringComparison.OrdinalIgnoreCase) || normalized.Equals("Closure", StringComparison.OrdinalIgnoreCase)) return "Completed";
            }

            if (t.JobClosureTime.HasValue) return "Completed";
            if (t.FCRTime.HasValue) return "FCR";
            if (t.QuoteTime.HasValue) return "Quote";
            if (t.FIRTime.HasValue) return "FIR";
            if (t.UnitRegistrationTime.HasValue) return "UnitRegistration";
            if (t.OnWayTime.HasValue) return "OnWay";
            return "Assigned";
        }
    }
}
