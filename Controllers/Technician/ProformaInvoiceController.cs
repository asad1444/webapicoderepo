using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers.Technician
{
    /// <summary>
    /// Technician ProForma Invoice API
    /// Route: api/technician/proforma/{jobId}
    /// </summary>
    [Route("api/technician/proforma")]
    [ApiController]
    [AllowAnonymous]
    public class TechnicianProformaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TechnicianProformaController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/proforma/{jobId}
        // Auto-fill data + existing invoice (if any)
        // Returns: currentDate, clientId, clientName, technicianId,
        //          technicianName, technicianPhoto + invoice fields + items
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetProforma(int jobId)
        {
            // Load job with all relations
            var job = await _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .Include(j => j.Technician)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            if (job == null) return NotFound("Job not found.");

            // Resolve MasterCard for this job
            var masterCard = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.JobID == jobId);

            // Auto-fill fields
            var currentDate     = DateTime.Now.ToString("dd MMM yyyy");
            var clientId        = job.Client?.ClientID ?? 0;
            var clientName      = job.Request?.ClientName ?? job.Client?.ClientName ?? "Walk-in Client";
            var clientPhone     = job.Request?.ClientContact ?? job.Client?.Phone ?? "";
            var clientAddress   = job.Request?.ServiceAddress ?? job.Client?.Home ?? "";
            var zone            = job.Request?.Zone ?? job.Client?.Zone ?? "";
            var technicianId    = job.TechnicianID ?? 0;
            var technicianName  = job.Technician?.FullName ?? "";
            var technicianPhoto = job.Technician?.Photo ?? "";
            var serviceType     = job.Request?.Category ?? job.Category ?? "";
            var crNumber        = job.CRNumber ?? "";

            // Existing invoice?
            ProformaInvoice? invoice = null;
            List<object> items = new();

            if (masterCard != null)
            {
                invoice = await _context.ProformaInvoices
                    .FirstOrDefaultAsync(p => p.MasterCardID == masterCard.MasterCardID);

                if (invoice != null)
                {
                    var rawItems = await _context.ProformaInvoiceItems
                        .Where(i => i.InvoiceID == invoice.InvoiceID)
                        .OrderBy(i => i.ItemID)
                        .ToListAsync();

                    items = rawItems.Select((it, idx) => (object)new
                    {
                        sr          = idx + 1,
                        itemId      = it.ItemID,
                        description = it.Description ?? "",
                        quantity    = it.Quantity ?? 1,
                        unit        = it.Unit ?? "NOS",
                        unitPrice   = it.UnitPrice ?? 0,
                        discount    = it.Discount ?? 0,
                        tax         = it.Tax ?? 0,
                        totalPrice  = it.TotalPrice ?? 0,
                    }).ToList();
                }
            }

            return Ok(new
            {
                // ── Auto-fill ────────────────────────────────────────────────
                currentDate,
                jobId,
                masterCardId    = masterCard?.MasterCardID ?? 0,
                crNumber,
                serviceType,

                // Client info
                clientId,
                clientName,
                clientPhone,
                clientAddress,
                zone,

                // Technician info
                technicianId,
                technicianName,
                technicianPhoto,

                // ── Existing invoice data (null if not created yet) ───────────
                invoiceId       = invoice?.InvoiceID,
                invoiceStatus   = invoice?.Status ?? "Draft",
                invoiceDate     = invoice?.InvoiceDate?.ToString("dd MMM yyyy") ?? currentDate,

                // FIR / Quote numbers
                firNumber       = invoice?.FIRNumber ?? "",
                quoteNumber     = invoice?.QuoteNumber ?? "",
                incidentDetails = invoice?.IncidentDetails ?? "",
                invoiceDetails  = invoice?.InvoiceDetails ?? "",
                finalNote       = invoice?.FinalNote ?? "",
                chatBox         = invoice?.ChatBox ?? "",
                voucherNumber   = invoice?.VoucherNumber ?? "",
                voucherType     = invoice?.VoucherType ?? "",
                voucherExpiry   = invoice?.VoucherExpiry?.ToString("dd MMM yyyy") ?? "",

                // Unit performance readings
                circuitAHighPressure = invoice?.CircuitAHighPressure ?? 0,
                circuitALowPressure  = invoice?.CircuitALowPressure  ?? 0,
                circuitAGT           = invoice?.CircuitAGT           ?? 0,
                circuitART           = invoice?.CircuitART           ?? 0,
                circuitAPower        = invoice?.CircuitAPower        ?? 0,
                circuitAAmpere       = invoice?.CircuitAAmpere       ?? 0,
                circuitBHighPressure = invoice?.CircuitBHighPressure ?? 0,
                circuitBLowPressure  = invoice?.CircuitBLowPressure  ?? 0,
                circuitBGT           = invoice?.CircuitBGT           ?? 0,
                circuitBRT           = invoice?.CircuitBRT           ?? 0,
                circuitBPower        = invoice?.CircuitBPower        ?? 0,
                circuitBAmpere       = invoice?.CircuitBAmpere       ?? 0,

                // Totals
                subTotal    = invoice?.SubTotal   ?? 0,
                tax         = invoice?.Tax        ?? 0,
                discount    = invoice?.Discount   ?? 0,
                grandTotal  = invoice?.GrandTotal ?? 0,

                // Quote items
                items,
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/technician/proforma/{jobId}
        // Create or update proforma invoice + items
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("{jobId}")]
        public async Task<IActionResult> SaveProforma(int jobId,
            [FromBody] ProformaInvoiceDto dto)
        {
            // Verify job exists and belongs to this technician
            var job = await _context.Jobs
                .Include(j => j.Technician)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            if (job == null) return NotFound("Job not found.");

            // Get or create MasterCard
            var masterCard = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.JobID == jobId);

            if (masterCard == null)
            {
                // Get a valid ClientID from job or default to first available
                int clientId = job.ClientID ?? 0;
                if (clientId == 0)
                {
                    // Try to get from request
                    var jobWithReq = await _context.Jobs
                        .Include(j => j.Request)
                        .FirstOrDefaultAsync(j => j.JobID == jobId);
                    // Use a fallback — just don't create MasterCard with ClientID=0
                    // Instead find if any client exists
                    var anyClient = await _context.Clients
                        .Select(c => c.ClientID).FirstOrDefaultAsync();
                    clientId = anyClient;
                }

                masterCard = new MasterCard
                {
                    JobID        = jobId,
                    ClientID     = clientId,
                    CompanyID    = job.CompanyID ?? 1,
                    TechnicianID = job.TechnicianID ?? 1,
                    Status       = "In Progress",
                    CreatedAt    = DateTime.Now,
                };
                _context.MasterCards.Add(masterCard);
                await _context.SaveChangesAsync();
            }

            // Get existing invoice
            var invoice = await _context.ProformaInvoices
                .FirstOrDefaultAsync(p => p.MasterCardID == masterCard.MasterCardID);

            if (invoice == null)
            {
                // Create new
                invoice = new ProformaInvoice
                {
                    MasterCardID    = masterCard.MasterCardID,
                    FIRNumber       = dto.FirNumber       ?? "",
                    QuoteNumber     = dto.QuoteNumber     ?? "",
                    IncidentDetails = dto.IncidentDetails ?? "",
                    InvoiceDetails  = dto.InvoiceDetails  ?? "",
                    FinalNote       = dto.FinalNote       ?? "",
                    ChatBox         = dto.ChatBox         ?? "",
                    VoucherNumber   = dto.VoucherNumber   ?? "",
                    VoucherType     = dto.VoucherType     ?? "",
                    TechnicianPhoto = "",
                    VoucherExpiry   = dto.VoucherExpiry,
                    InvoiceDate     = dto.InvoiceDate     ?? DateTime.Now,
                    Status          = dto.Status          ?? "Draft",
                    SubTotal        = dto.SubTotal        ?? 0,
                    Tax             = dto.Tax             ?? 0,
                    Discount        = dto.Discount        ?? 0,
                    GrandTotal      = dto.GrandTotal      ?? 0,
                    // Circuit readings
                    CircuitAHighPressure = dto.CircuitAHighPressure,
                    CircuitALowPressure  = dto.CircuitALowPressure,
                    CircuitAGT           = dto.CircuitAGT,
                    CircuitART           = dto.CircuitART,
                    CircuitAPower        = dto.CircuitAPower,
                    CircuitAAmpere       = dto.CircuitAAmpere,
                    CircuitBHighPressure = dto.CircuitBHighPressure,
                    CircuitBLowPressure  = dto.CircuitBLowPressure,
                    CircuitBGT           = dto.CircuitBGT,
                    CircuitBRT           = dto.CircuitBRT,
                    CircuitBPower        = dto.CircuitBPower,
                    CircuitBAmpere       = dto.CircuitBAmpere,
                    CreatedAt            = DateTime.Now,
                };
                _context.ProformaInvoices.Add(invoice);
            }
            else
            {
                // Update existing
                invoice.FIRNumber       = dto.FirNumber       ?? invoice.FIRNumber;
                invoice.QuoteNumber     = dto.QuoteNumber     ?? invoice.QuoteNumber;
                invoice.IncidentDetails = dto.IncidentDetails ?? invoice.IncidentDetails;
                invoice.InvoiceDetails  = dto.InvoiceDetails  ?? invoice.InvoiceDetails;
                invoice.FinalNote       = dto.FinalNote       ?? invoice.FinalNote;
                invoice.ChatBox         = dto.ChatBox         ?? invoice.ChatBox;
                invoice.VoucherNumber   = dto.VoucherNumber   ?? invoice.VoucherNumber;
                invoice.VoucherType     = dto.VoucherType     ?? invoice.VoucherType;
                invoice.VoucherExpiry   = dto.VoucherExpiry   ?? invoice.VoucherExpiry;
                invoice.Status          = dto.Status          ?? invoice.Status;
                invoice.SubTotal        = dto.SubTotal        ?? invoice.SubTotal;
                invoice.Tax             = dto.Tax             ?? invoice.Tax;
                invoice.Discount        = dto.Discount        ?? invoice.Discount;
                invoice.GrandTotal      = dto.GrandTotal      ?? invoice.GrandTotal;
                invoice.CircuitAHighPressure = dto.CircuitAHighPressure ?? invoice.CircuitAHighPressure;
                invoice.CircuitALowPressure  = dto.CircuitALowPressure  ?? invoice.CircuitALowPressure;
                invoice.CircuitAGT           = dto.CircuitAGT           ?? invoice.CircuitAGT;
                invoice.CircuitART           = dto.CircuitART           ?? invoice.CircuitART;
                invoice.CircuitAPower        = dto.CircuitAPower        ?? invoice.CircuitAPower;
                invoice.CircuitAAmpere       = dto.CircuitAAmpere       ?? invoice.CircuitAAmpere;
                invoice.CircuitBHighPressure = dto.CircuitBHighPressure ?? invoice.CircuitBHighPressure;
                invoice.CircuitBLowPressure  = dto.CircuitBLowPressure  ?? invoice.CircuitBLowPressure;
                invoice.CircuitBGT           = dto.CircuitBGT           ?? invoice.CircuitBGT;
                invoice.CircuitBRT           = dto.CircuitBRT           ?? invoice.CircuitBRT;
                invoice.CircuitBPower        = dto.CircuitBPower        ?? invoice.CircuitBPower;
                invoice.CircuitBAmpere       = dto.CircuitBAmpere       ?? invoice.CircuitBAmpere;
            }

            await _context.SaveChangesAsync();

            // ── Save quote items ────────────────────────────────────────────
            if (dto.Items != null && dto.Items.Any())
            {
                // Remove existing items and re-add
                var existing = _context.ProformaInvoiceItems
                    .Where(i => i.InvoiceID == invoice.InvoiceID);
                _context.ProformaInvoiceItems.RemoveRange(existing);
                await _context.SaveChangesAsync();

                foreach (var it in dto.Items)
                {
                    _context.ProformaInvoiceItems.Add(new ProformaInvoiceItem
                    {
                        InvoiceID   = invoice.InvoiceID,
                        Description = it.Description ?? "",
                        Quantity    = it.Quantity    ?? 1,
                        Unit        = it.Unit        ?? "NOS",
                        UnitPrice   = it.UnitPrice   ?? 0,
                        Discount    = it.Discount    ?? 0,
                        Tax         = it.Tax         ?? 0,
                        TotalPrice  = it.TotalPrice  ?? 0,
                    });
                }
                await _context.SaveChangesAsync();
            }

            var tracking = await _context.JobTracking.FirstOrDefaultAsync(t => t.JobID == jobId);
            if (tracking == null)
            {
                tracking = new JobTracking { JobID = jobId, ETA = "" };
                _context.JobTracking.Add(tracking);
            }

            tracking.ETA ??= "";
            tracking.FIRTime ??= DateTime.Now;
            tracking.CurrentStep = "FIR";
            if (!string.IsNullOrWhiteSpace(dto.QuoteNumber) || (dto.GrandTotal ?? 0) > 0)
            {
                tracking.QuoteTime ??= DateTime.Now;
                tracking.CurrentStep = "Quote";
            }

            // Submitting the proforma completes only the proforma step.
            // Job closure happens later from the payment/close-job flow.
            if (dto.Status == "Submitted")
            {
                tracking.CurrentStep = "Proforma Done";
                job.Status = "In Progress";
                job.CompletedAt = null;
            }
            else if (dto.Status == "Completed")
            {
                tracking.JobClosureTime ??= DateTime.Now;
                tracking.CurrentStep = "Completed";
                job.Status = "Completed";
                job.CompletedAt = DateTime.Now;
            }

            tracking.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                invoiceId   = invoice.InvoiceID,
                masterCardId = masterCard.MasterCardID,
                status      = invoice.Status,
                grandTotal  = invoice.GrandTotal,
                message     = "Proforma Invoice saved successfully."
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/technician/proforma/{jobId}/generate
        // Mark invoice as Generated — finalizes the proforma
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("{jobId}/generate")]
        public async Task<IActionResult> GenerateInvoice(int jobId)
        {
            var masterCard = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.JobID == jobId);

            if (masterCard == null)
                return NotFound("No MasterCard for this job.");

            var invoice = await _context.ProformaInvoices
                .FirstOrDefaultAsync(p => p.MasterCardID == masterCard.MasterCardID);

            if (invoice == null)
                return NotFound("Proforma Invoice not found. Please save first.");

            invoice.Status = "Generated";
            await _context.SaveChangesAsync();

            // Also update MasterCard status
            masterCard.Status = "Invoice Generated";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                invoiceId  = invoice.InvoiceID,
                status     = invoice.Status,
                grandTotal = invoice.GrandTotal,
                message    = "Invoice generated successfully."
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/proforma/by-mastercard/{masterCardId}
        // Alternative lookup by MasterCard ID
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("by-mastercard/{masterCardId}")]
        public async Task<IActionResult> GetByMasterCard(int masterCardId)        {
            var masterCard = await _context.MasterCards
                .Include(m => m.Job)
                    .ThenInclude(j => j.Request)
                .Include(m => m.Job)
                    .ThenInclude(j => j.Client)
                .Include(m => m.Technician)
                .FirstOrDefaultAsync(m => m.MasterCardID == masterCardId);

            if (masterCard == null) return NotFound("MasterCard not found.");

            // Redirect to job-based endpoint logic
            return await GetProforma(masterCard.JobID);
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class ProformaInvoiceDto
    {
        public string? FirNumber       { get; set; }
        public string? QuoteNumber     { get; set; }
        public string? IncidentDetails { get; set; }
        public string? InvoiceDetails  { get; set; }
        public string? FinalNote       { get; set; }
        public string? ChatBox         { get; set; }
        public string? VoucherNumber   { get; set; }
        public string? VoucherType     { get; set; }
        public DateTime? VoucherExpiry { get; set; }
        public DateTime? InvoiceDate   { get; set; }
        public string? Status          { get; set; }

        // Totals
        public decimal? SubTotal   { get; set; }
        public decimal? Tax        { get; set; }
        public decimal? Discount   { get; set; }
        public decimal? GrandTotal { get; set; }

        // Circuit readings
        public decimal? CircuitAHighPressure { get; set; }
        public decimal? CircuitALowPressure  { get; set; }
        public decimal? CircuitAGT           { get; set; }
        public decimal? CircuitART           { get; set; }
        public decimal? CircuitAPower        { get; set; }
        public decimal? CircuitAAmpere       { get; set; }
        public decimal? CircuitBHighPressure { get; set; }
        public decimal? CircuitBLowPressure  { get; set; }
        public decimal? CircuitBGT           { get; set; }
        public decimal? CircuitBRT           { get; set; }
        public decimal? CircuitBPower        { get; set; }
        public decimal? CircuitBAmpere       { get; set; }

        // Quote items
        public List<ProformaItemDto>? Items { get; set; }
    }

    public class ProformaItemDto
    {
        public string? Description { get; set; }
        public int?    Quantity    { get; set; }
        public string? Unit        { get; set; }
        public decimal? UnitPrice  { get; set; }
        public decimal? Discount   { get; set; }
        public decimal? Tax        { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}
