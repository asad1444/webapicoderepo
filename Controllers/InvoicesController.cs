using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Invoices
        // Get all invoices with optional filter by date range
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? status)
        {
            var query = _context.Invoices
                .Include(i => i.Job)
                    .ThenInclude(j => j.Client)
                .Include(i => i.Job)
                    .ThenInclude(j => j.Technician)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(i => i.InvoiceDate >= from.Value);

            if (to.HasValue)
                query = query.Where(i => i.InvoiceDate <= to.Value.AddDays(1));

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new
                {
                    i.Id,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    i.GrandTotal,
                    i.JobID,
                    CRNumber = i.Job.CRNumber,
                    ClientName = i.Job.Client.ClientName,
                    TechnicianName = i.Job.Technician != null ? i.Job.Technician.FullName : "Unassigned",
                    JobStatus = i.Job.Status
                })
                .ToListAsync();

            return Ok(invoices);
        }

        // GET: api/Invoices/{id}
        // Get single invoice detail with payment history
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Job)
                    .ThenInclude(j => j.Client)
                .Include(i => i.Job)
                    .ThenInclude(j => j.Technician)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound("Invoice not found.");

            var payments = await _context.Payments
                .Where(p => p.InvoiceId == id)
                .Select(p => new
                {
                    p.Id,
                    p.PaymentMethod,
                    p.AmountReceived,
                    p.TransactionReference,
                    p.IsSuccessful,
                    p.PaymentDate
                })
                .ToListAsync();

            var successfulPayments = payments.Where(p => p.IsSuccessful).ToList();
            decimal totalPaid = (decimal)successfulPayments.Sum(p => (double)p.AmountReceived);
            decimal balance = invoice.GrandTotal - totalPaid;

            return Ok(new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.InvoiceDate,
                invoice.GrandTotal,
                TotalPaid = totalPaid,
                Balance = balance,
                PaymentStatus = balance <= 0 ? "Paid" : totalPaid > 0 ? "Partial" : "Unpaid",
                Job = new
                {
                    invoice.Job.JobID,
                    invoice.Job.CRNumber,
                    invoice.Job.ClientRequest,
                    invoice.Job.Status
                },
                Client = new
                {
                    invoice.Job.Client.ClientName,
                    invoice.Job.Client.Phone,
                    invoice.Job.Client.Email
                },
                Technician = invoice.Job.Technician != null
                    ? new { invoice.Job.Technician.FullName, invoice.Job.Technician.Phone }
                    : null,
                Payments = payments
            });
        }

        // GET: api/Invoices/job/{jobId}
        // Get invoice for a specific job
        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetByJob(int jobId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.JobID == jobId);
            if (invoice == null) return NotFound("No invoice found for this job.");

            return Ok(invoice);
        }

        // GET: api/Invoices/summary
        // Revenue summary for dashboard
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // SQLite fix: fetch to memory then sum as double
            var allInvoices = await _context.Invoices.Select(i => (double)i.GrandTotal).ToListAsync();
            var totalRevenue = (decimal)allInvoices.Sum();

            var monthlyInvoices = await _context.Invoices
                .Where(i => i.InvoiceDate.Month == currentMonth && i.InvoiceDate.Year == currentYear)
                .Select(i => (double)i.GrandTotal)
                .ToListAsync();
            var monthlyRevenue = (decimal)monthlyInvoices.Sum();

            var totalInvoices = await _context.Invoices.CountAsync();

            var paymentsRaw = await _context.Payments
                .Where(p => p.IsSuccessful)
                .Select(p => (double)p.AmountReceived)
                .ToListAsync();
            var totalPayments = (decimal)paymentsRaw.Sum();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalInvoices = totalInvoices,
                TotalCollected = totalPayments,
                OutstandingBalance = totalRevenue - totalPayments
            });
        }
    }
}
