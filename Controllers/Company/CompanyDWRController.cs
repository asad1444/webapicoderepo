using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/DailyWorkReports")]
    [ApiController]
    public class CompanyDWRController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyDWRController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Company/{companyId}/DailyWorkReports
        [HttpGet]
        public async Task<IActionResult> GetDailyWorkReports(int companyId)
        {
            var reports = await _context.DailyWorkReports
                .Include(d => d.Technician)
                .Where(d => d.CompanyID == companyId)
                .Select(d => new
                {
                    d.DWRID,
                    ReportDate = d.ReportDate ?? d.CreatedAt, // Fallback for old records
                    d.Status,
                    TechnicianName = d.Technician.FullName,
                    TechnicianCode = d.Technician.TechnicianCode
                })
                .OrderByDescending(d => d.ReportDate)
                .ToListAsync();

            return Ok(reports);
        }

        // GET: api/Company/{companyId}/DailyWorkReports/{dwrId}
        [HttpGet("{dwrId}")]
        public async Task<IActionResult> GetDWRDetails(int companyId, int dwrId)
        {
            var report = await _context.DailyWorkReports
                .Include(d => d.Technician)
                .FirstOrDefaultAsync(d => d.DWRID == dwrId && d.CompanyID == companyId);

            if (report == null)
                return NotFound(new { message = "Report not found or unauthorized access" });

            return Ok(new
            {
                report.DWRID,
                report.DutyIn,
                report.DutyOut,
                report.PendingJobs,
                report.RepeatJobs,
                report.CompletedJobs,
                report.WalletBalance,
                report.ToolBox,
                report.SpareParts,
                report.FinalNote,
                report.Record,
                ReportDate = report.ReportDate ?? report.CreatedAt,
                report.Status,
                Technician = new
                {
                    report.Technician.TechnicianID,
                    report.Technician.TechnicianCode,
                    report.Technician.FullName,
                    report.Technician.Phone,
                    report.Technician.Photo
                }
            });
        }

        // PUT: api/Company/{companyId}/DailyWorkReports/{dwrId}/status
        [HttpPut("{dwrId}/status")]
        public async Task<IActionResult> UpdateDWRStatus(int companyId, int dwrId, [FromBody] string status)
        {
            if (status != "Approved" && status != "Pending")
            {
                return BadRequest(new { message = "Invalid status. Allowed values are 'Approved' or 'Pending'" });
            }

            var report = await _context.DailyWorkReports
                .FirstOrDefaultAsync(d => d.DWRID == dwrId && d.CompanyID == companyId);

            if (report == null)
                return NotFound(new { message = "Report not found or unauthorized access" });

            report.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Report status updated successfully", status = report.Status });
        }
    }
}
