using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/Payments")]
    [ApiController]
    public class CompanyPaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyPaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Company/{companyId}/Payments
        [HttpGet]
        public async Task<IActionResult> GetPayments(int companyId)
        {
            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Request)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Technician)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Job)
                        .ThenInclude(j => j.Client)
                .Where(p => p.Invoice.Job.CompanyID == companyId && p.Invoice.Job.AssignmentSource == "Company")
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
    }
}
