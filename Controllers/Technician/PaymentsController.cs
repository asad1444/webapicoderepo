using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models.Enums;

namespace SmartProManWebAPI.Controllers.Technician
{
    [Route("api/technician/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/payments/{technicianId}
        // Payment Collection page — shows all payments with Cash/Online filter
        // Image reference: Payment Collection screen with PKR total at top
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}")]
        public async Task<IActionResult> GetPayments(
            int technicianId,
            [FromQuery] string? filter = "today",   // today | all
            [FromQuery] string? method = "all")      // all | cash | online
        {
            var today = DateTime.Today;

            var query = _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Request)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Client)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Technician)
                .Where(p =>
                    p.IsSuccessful &&
                    p.Invoice.Job.TechnicianID == technicianId)
                .AsQueryable();

            // Date filter
            if (filter?.ToLower() == "today")
                query = query.Where(p => p.PaymentDate.Date == today);

            // Payment method filter
            if (method?.ToLower() == "cash")
                query = query.Where(p => p.PaymentMethod == PaymentMethod.Cash);
            else if (method?.ToLower() == "online")
                query = query.Where(p => p.PaymentMethod == PaymentMethod.Online);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    PaymentID         = p.Id,
                    SlipNumber        = $"SLP-{p.Invoice.JobID}",
                    JobID             = $"JOB-{p.Invoice.JobID}",
                    ClientName        = p.Invoice.Job.Request != null ? p.Invoice.Job.Request.ClientName : (p.Invoice.Job.Client != null ? p.Invoice.Job.Client.ClientName : ""),
                    ServiceAddress    = p.Invoice.Job.Request != null ? $"{p.Invoice.Job.Request.ServiceAddress}, {p.Invoice.Job.Request.Zone}" : (p.Invoice.Job.Client != null ? $"{p.Invoice.Job.Client.ServeLocation}, {p.Invoice.Job.Client.Zone}" : ""),
                    ServiceType       = p.Invoice.Job.Category,
                    Amount            = p.AmountReceived,
                    AmountDisplay     = $"PKR {p.AmountReceived:N0}",
                    PaymentMethod     = p.PaymentMethod.ToString(),
                    PaymentTime       = p.PaymentDate.ToString("hh:mm tt"),
                    PaymentDate       = p.PaymentDate.ToString("dd MMM yyyy"),
                    TransactionRef    = p.TransactionReference
                })
                .ToListAsync();

            // Totals
            var totalAmount  = payments.Sum(p => p.Amount);
            var cashTotal    = payments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            var onlineTotal  = payments.Where(p => p.PaymentMethod == "Online").Sum(p => p.Amount);

            return Ok(new
            {
                Header = new
                {
                    Title           = filter?.ToLower() == "today" ? "Today's Collection" : "Payment Collection",
                    TotalAmount     = totalAmount,
                    TotalDisplay    = $"PKR {totalAmount:N0}",
                    CashTotal       = cashTotal,
                    OnlineTotal     = onlineTotal,
                    ActiveFilter    = filter,
                    ActiveMethod    = method
                },
                Payments = payments
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/payments/{technicianId}/slip/{paymentId}
        // Single Payment Slip — exactly matches the Payment Slip screen in image
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}/slip/{paymentId}")]
        public async Task<IActionResult> GetPaymentSlip(int technicianId, int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Request)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Client)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Technician)
                .FirstOrDefaultAsync(p =>
                    p.Id == paymentId &&
                    p.Invoice.Job.TechnicianID == technicianId);

            if (payment == null) return NotFound("Payment slip not found.");

            var job        = payment.Invoice.Job;
            var request    = job.Request;
            var client     = job.Client;
            var technician = job.Technician;
            
            var clientName = request?.ClientName ?? client?.ClientName ?? "";
            var address    = request != null ? $"{request.ServiceAddress}, {request.Zone}" : (client != null ? $"{client.ServeLocation}, {client.Zone}" : "");

            var slipNumber = $"SLP-{payment.Invoice.JobID}";

            var slip = new
            {
                // ── Header ──────────────────────────────────────────────────
                SlipNumber     = slipNumber,
                Status         = "Payment Received",
                Amount         = payment.AmountReceived,
                AmountDisplay  = $"PKR {payment.AmountReceived:N0}",

                // ── Payment Info ────────────────────────────────────────────
                Date           = payment.PaymentDate.ToString("dd MMM yyyy"),
                Time           = payment.PaymentDate.ToString("hh:mm tt"),
                Method         = payment.PaymentMethod.ToString(),
                TransactionRef = payment.TransactionReference,

                // ── Customer Section ────────────────────────────────────────
                Customer = new
                {
                    Name         = clientName,
                    JobID        = $"JOB-{job.JobID}",
                    ServiceType  = job.Category,
                    Address      = address
                },

                // ── Technician Section ──────────────────────────────────────
                TechnicianInfo = new
                {
                    Name       = technician?.FullName,
                    TechID     = technician != null ? $"TECH-{technician.TechnicianID}" : "",
                    Phone      = technician?.Phone,
                    Photo      = technician?.Photo
                },

                // ── Footer ──────────────────────────────────────────────────
                Footer         = "Official Payment Receipt",
                InvoiceNumber  = payment.Invoice.InvoiceNumber
            };

            return Ok(slip);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/payments/{technicianId}/slip/{paymentId}/download
        // Download Slip — returns slip data structured for PDF/print generation
        // Frontend can use this data to render a downloadable PDF
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}/slip/{paymentId}/download")]
        public async Task<IActionResult> DownloadSlip(int technicianId, int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Request)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Client)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Technician)
                .FirstOrDefaultAsync(p =>
                    p.Id == paymentId &&
                    p.Invoice.Job.TechnicianID == technicianId);

            if (payment == null) return NotFound("Payment slip not found.");

            var job        = payment.Invoice.Job;
            var request    = job.Request;
            var client     = job.Client;
            var technician = job.Technician;
            var slipNumber = $"SLP-{payment.Invoice.JobID}";
            
            var clientName = request?.ClientName ?? client?.ClientName ?? "";
            var address    = request != null ? $"{request.ServiceAddress}, {request.Zone}" : (client != null ? $"{client.ServeLocation}, {client.Zone}" : "");

            // Return complete structured slip data + filename for frontend download
            return Ok(new
            {
                FileName      = $"{slipNumber}_{payment.PaymentDate:yyyyMMdd}.pdf",
                SlipNumber    = slipNumber,
                AmountDisplay = $"PKR {payment.AmountReceived:N0}",
                Date          = payment.PaymentDate.ToString("dd MMM yyyy"),
                Time          = payment.PaymentDate.ToString("hh:mm tt"),
                Method        = payment.PaymentMethod.ToString(),

                Customer = new
                {
                    Name        = clientName,
                    JobID       = $"JOB-{job.JobID}",
                    ServiceType = job.Category,
                    Address     = address
                },

                Technician = new
                {
                    Name  = technician?.FullName,
                    TechID = technician != null ? $"TECH-{technician.TechnicianID}" : ""
                },

                // Template hint for frontend PDF renderer
                Template      = "payment_slip_v1",
                Footer        = "Official Payment Receipt"
            });
        }
    }
}
