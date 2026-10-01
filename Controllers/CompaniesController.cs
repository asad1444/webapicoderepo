using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly SmartProManWebAPI.Services.IEmailService _emailService;

        public CompaniesController(AppDbContext context, SmartProManWebAPI.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: api/Companies/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var summary = new
            {
                TotalCompanies = await _context.Companies.CountAsync(),
                Active = await _context.Companies.CountAsync(c => c.Status == "Active"),
                PendingApproval = await _context.Companies.CountAsync(c => c.Status == "Pending" || c.Status == "Approval"),
                Suspended = await _context.Companies.CountAsync(c => c.Status == "Suspended")
            };

            return Ok(summary);
        }

        // GET: api/Companies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCompanies(
            [FromQuery] string? status, 
            [FromQuery] string? cityZone, 
            [FromQuery] string? serviceType)
        {
            var query = _context.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(status) && !status.Equals("All Statuses", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(cityZone))
            {
                query = query.Where(c => c.City == cityZone || c.Zone == cityZone);
            }

            if (!string.IsNullOrEmpty(serviceType))
            {
                query = query.Where(c => c.ServiceType == serviceType);
            }

            // SQLite decimal Sum fix: pehle companies fetch karo, phir client side par calculate karo
            var companiesList = await query.ToListAsync();

            var companyIds = companiesList.Select(c => c.CompanyID).ToList();

            // Sab data ek baar mein fetch karo (N+1 se bachne ke liye)
            var techCounts = await _context.Technicians
                .Where(t => companyIds.Contains(t.CompanyID))
                .GroupBy(t => t.CompanyID)
                .Select(g => new { CompanyID = g.Key, Count = g.Count() })
                .ToListAsync();

            var jobCounts = await _context.Jobs
                .Where(j => j.CompanyID.HasValue && companyIds.Contains(j.CompanyID.Value) && j.Status == "Completed")
                .GroupBy(j => j.CompanyID!.Value)
                .Select(g => new { CompanyID = g.Key, Count = g.Count() })
                .ToListAsync();

            // Completed job IDs per company
            var completedJobIds = await _context.Jobs
                .Where(j => j.CompanyID.HasValue && companyIds.Contains(j.CompanyID.Value) && j.Status == "Completed")
                .Select(j => new { j.JobID, CompanyID = j.CompanyID!.Value })
                .ToListAsync();

            // Invoices jo in jobs se related hain - GrandTotal ko double mein laao (SQLite fix)
            var jobIdList = completedJobIds.Select(j => j.JobID).ToList();
            var invoices = await _context.Set<Invoice>()
                .Where(i => jobIdList.Contains(i.JobID))
                .Select(i => new { i.JobID, GrandTotal = (double)i.GrandTotal })
                .ToListAsync();

            var companies = companiesList.Select(c =>
            {
                var techCount = techCounts.FirstOrDefault(t => t.CompanyID == c.CompanyID)?.Count ?? 0;
                var jobCount = jobCounts.FirstOrDefault(j => j.CompanyID == c.CompanyID)?.Count ?? 0;
                var compJobIds = completedJobIds.Where(j => j.CompanyID == c.CompanyID).Select(j => j.JobID).ToHashSet();
                var revenue = (decimal)invoices.Where(i => compJobIds.Contains(i.JobID)).Sum(i => i.GrandTotal);

                return new
                {
                    c.CompanyID,
                    c.CompanyLogo,
                    c.CompanyName,
                    c.ServiceType,
                    c.InchargeName,
                    c.InchargePhone,
                    c.City,
                    c.Zone,
                    TechniciansCount = techCount,
                    JobsCompleted = jobCount,
                    Revenue = revenue,
                    c.Status
                };
            }).ToList();

            return Ok(companies);

        }

        // GET: api/Companies/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompanyById(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return Ok(company);
        }

        // POST: api/Companies
        [HttpPost]
        public async Task<ActionResult<SmartProManWebAPI.Models.Company>> CreateCompany([FromBody] SmartProManWebAPI.Models.Company company)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCompanyById), new { id = company.CompanyID }, company);
        }

        // PUT: api/Companies/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] SmartProManWebAPI.Models.Company company)
        {
            var existing = await _context.Companies.FindAsync(id);
            if (existing == null) return NotFound();

            if (!string.IsNullOrEmpty(company.CompanyName)) existing.CompanyName = company.CompanyName;
            if (!string.IsNullOrEmpty(company.InchargeName)) existing.InchargeName = company.InchargeName;
            if (!string.IsNullOrEmpty(company.InchargePhone)) existing.InchargePhone = company.InchargePhone;
            if (!string.IsNullOrEmpty(company.City)) existing.City = company.City;
            if (company.Zone != null) existing.Zone = company.Zone;
            if (!string.IsNullOrEmpty(company.ServiceType)) existing.ServiceType = company.ServiceType;
            if (!string.IsNullOrEmpty(company.Status)) existing.Status = company.Status;
            if (!string.IsNullOrEmpty(company.CompanyLogo)) existing.CompanyLogo = company.CompanyLogo;
            if (!string.IsNullOrEmpty(company.Email)) existing.Email = company.Email;

            _context.Entry(existing).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE: api/Companies/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            var techs = _context.Technicians.Where(t => t.CompanyID == id);
            _context.Technicians.RemoveRange(techs);

            var jobs = _context.Jobs.Where(j => j.CompanyID == id);
            _context.Jobs.RemoveRange(jobs);

            var masterCards = _context.MasterCards.Where(m => m.CompanyID == id);
            _context.MasterCards.RemoveRange(masterCards);

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Companies/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var validStatuses = new[] { "Pending", "Approval", "Active", "Suspend" };
            if (!validStatuses.Contains(status))
                return BadRequest("Invalid status requested.");

            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();

            bool wasNotActive = company.Status != "Active";

            if (status == "Approval") company.Status = "Active";
            else if (status == "Suspend") company.Status = "Suspended";
            else company.Status = status;

            _context.Entry(company).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            // Send email if activated
            if (company.Status == "Active" && wasNotActive && !string.IsNullOrEmpty(company.Email))
            {
                string subject = "Smart Proman - Company Activated";
                string body = $@"
                    <html>
                    <body>
                        <h2>Welcome to Smart Proman!</h2>
                        <p>Dear {company.CompanyName},</p>
                        <p>Your account is approved successfully. Now You can Login.</p>
                        <br/>
                        <p>Regards,<br/>Smart Proman Team</p>
                    </body>
                    </html>";
                await _emailService.SendAsync(company.Email, subject, body);
            }

            return NoContent();
        }
    }
}
