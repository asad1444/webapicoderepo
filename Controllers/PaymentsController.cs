using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using SmartProManWebAPI.Models.Enums;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        // Get all payments with optional filters
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool? isSuccessful)
        {
            var query = _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Request)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Technician)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Client)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(p => p.PaymentDate >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.PaymentDate <= to.Value.AddDays(1));

            if (isSuccessful.HasValue)
                query = query.Where(p => p.IsSuccessful == isSuccessful.Value);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    p.Id,
                    p.PaymentMethod,
                    p.AmountReceived,
                    p.TransactionReference,
                    p.IsSuccessful,
                    p.PaymentDate,
                    p.InvoiceId,
                    InvoiceNumber = p.Invoice.InvoiceNumber,
                    JobID = p.Invoice.JobID,
                    CRNumber = p.Invoice.Job.CRNumber,
                    ClientName = p.Invoice.Job.Request != null ? p.Invoice.Job.Request.ClientName : (p.Invoice.Job.Client != null ? p.Invoice.Job.Client.ClientName : ""),
                    TechnicianName = p.Invoice.Job.Technician != null ? p.Invoice.Job.Technician.FullName : ""
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/Payments/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null) return NotFound("Payment not found.");

            return Ok(payment);
        }

        // POST: api/Payments
        // Record a manual payment (admin side)
        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto dto)
        {
            var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
            if (invoice == null) return NotFound("Invoice not found.");

            // Check amount does not exceed balance
            // SQLite fix: fetch to memory then sum
            var alreadyPaidRaw = await _context.Payments
                .Where(p => p.InvoiceId == dto.InvoiceId && p.IsSuccessful)
                .Select(p => (double)p.AmountReceived)
                .ToListAsync();
            var alreadyPaid = (decimal)alreadyPaidRaw.Sum();

            var balance = invoice.GrandTotal - alreadyPaid;

            if (dto.AmountReceived > balance)
                return BadRequest($"Amount exceeds outstanding balance of {balance:F2}.");

            var payment = new Payment
            {
                InvoiceId = dto.InvoiceId,
                PaymentMethod = dto.PaymentMethod,
                AmountReceived = dto.AmountReceived,
                TransactionReference = dto.TransactionReference,
                IsSuccessful = true,
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);

            // A received payment completes Payment Collection only.
            // Job Closure is handled by the explicit technician close-job action.
            if (dto.AmountReceived >= balance)
            {
                var job = await _context.Jobs.FindAsync(invoice.JobID);
                if (job != null && job.Status != "Completed")
                {
                    job.Status = "In Progress";
                    job.CompletedAt = null;
                    var tracking = await _context.JobTracking
                        .FirstOrDefaultAsync(t => t.JobID == invoice.JobID);
                    if (tracking != null)
                    {
                        tracking.FCRTime ??= DateTime.Now;
                        tracking.CurrentStep = "Payment Collection";
                        tracking.LastUpdated = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Payment recorded successfully.",
                PaymentID = payment.Id,
                RemainingBalance = balance - dto.AmountReceived
            });
        }

        // GET: api/Payments/invoice/{invoiceId}
        // Get all payments for a specific invoice
        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetByInvoice(int invoiceId)
        {
            var payments = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var totalPaid = payments.Where(p => p.IsSuccessful).Sum(p => p.AmountReceived);

            return Ok(new { TotalPaid = totalPaid, Payments = payments });
        }
    }

    public class RecordPaymentDto
    {
        public int InvoiceId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountReceived { get; set; }
        public string? TransactionReference { get; set; }
    }
}
