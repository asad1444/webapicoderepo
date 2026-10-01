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
                string subject = "🎉 SmartProMan – Your Account Has Been Approved!";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
</head>
<body style='margin:0;padding:0;background-color:#f4f6f9;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9;padding:30px 0;'>
    <tr>
      <td align='center'>
        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
          
          <!-- Header -->
          <tr>
            <td style='background:linear-gradient(135deg,#0D3320,#1E6B3A);padding:36px 40px;text-align:center;'>
              <h1 style='color:#ffffff;margin:0;font-size:26px;font-weight:700;letter-spacing:1px;'>SmartProMan</h1>
              <p style='color:rgba(255,255,255,0.75);margin:6px 0 0;font-size:13px;'>Operations Management Platform</p>
            </td>
          </tr>

          <!-- Success Badge -->
          <tr>
            <td style='text-align:center;padding:30px 40px 10px;'>
              <div style='display:inline-block;background-color:#e8f5e9;border-radius:50%;width:70px;height:70px;line-height:70px;font-size:36px;'>✅</div>
              <h2 style='color:#1E6B3A;font-size:22px;margin:16px 0 6px;'>Account Approved!</h2>
              <p style='color:#666666;font-size:14px;margin:0;'>Your company has been reviewed and activated by the SmartProMan admin team.</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style='padding:20px 40px 30px;'>
              <p style='color:#333333;font-size:15px;margin:0 0 16px;'>Dear <strong>{company.CompanyName}</strong>,</p>
              <p style='color:#555555;font-size:14px;line-height:1.7;margin:0 0 20px;'>
                We are pleased to inform you that your SmartProMan company account is now <strong style='color:#1E6B3A;'>Active</strong>. 
                You can now log in and start managing your field operations, dispatch technicians, and track jobs in real-time.
              </p>

              <!-- Info Box -->
              <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f0faf3;border-left:4px solid #1E6B3A;border-radius:6px;margin-bottom:24px;'>
                <tr>
                  <td style='padding:16px 18px;'>
                    <p style='margin:0 0 6px;font-size:13px;color:#555;'><strong>📧 Registered Email:</strong> {company.Email}</p>
                    <p style='margin:0;font-size:13px;color:#555;'><strong>🏢 Company Name:</strong> {company.CompanyName}</p>
                  </td>
                </tr>
              </table>

              <!-- CTA Button -->
              <table width='100%' cellpadding='0' cellspacing='0'>
                <tr>
                  <td align='center'>
                    <a href='https://smartproman.com/login' 
                       style='display:inline-block;background:linear-gradient(135deg,#0D3320,#1E6B3A);color:#ffffff;text-decoration:none;padding:14px 40px;border-radius:8px;font-size:15px;font-weight:700;letter-spacing:0.5px;'>
                      Login to SmartProMan →
                    </a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style='background-color:#f8faf8;padding:20px 40px;text-align:center;border-top:1px solid #e8f0e8;'>
              <p style='color:#999999;font-size:12px;margin:0;'>
                This is an automated message from SmartProMan. Please do not reply to this email.<br/>
                © 2026 SmartProMan. All rights reserved.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
                await _emailService.SendAsync(company.Email, subject, body);
            }

            return NoContent();
        }
    }
}
