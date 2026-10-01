using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyWorkReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DailyWorkReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/DailyWorkReports/auto-data/5
        [HttpGet("auto-data/{technicianId}")]
        public async Task<IActionResult> GetAutoData(int technicianId)
        {
            var tech = await _context.Technicians.FindAsync(technicianId);
            if (tech == null) return NotFound();

            var today = DateTime.Today;

            // Compute job stats for the day
            var jobsToday = await _context.Jobs
                .Where(j => j.TechnicianID == technicianId && j.CreatedAt.HasValue && j.CreatedAt.Value.Date == today)
                .ToListAsync();

            var todaysCollectionRaw = await _context.Payments
                .Where(p =>
                    p.IsSuccessful &&
                    p.PaymentDate.Date == today &&
                    p.Invoice.Job.TechnicianID == technicianId)
                .Select(p => (double)p.AmountReceived)
                .ToListAsync();
            var todaysCollection = (decimal)todaysCollectionRaw.Sum();

            var autoData = new
            {
                tech.Photo,
                tech.FullName,
                tech.TechnicianID,
                ExpectedDutyIn = tech.LastDutyIn,
                ExpectedDutyOut = tech.LastDutyOut,
                Pending = jobsToday.Count(j => j.Status == "Open" || j.Status == "In Progress"),
                Repeat = jobsToday.Count(j => j.Category == "Repeat Call"),
                Ok = jobsToday.Count(j => j.Status == "Completed"),
                CompletedMCIDs = jobsToday.Where(j => j.Status == "Completed").Select(j => $"MCID-{j.JobID:D3}").Distinct().ToList(),
                WalletBalance = todaysCollection
            };

            return Ok(autoData);
        }

        // GET: api/DailyWorkReports/check-submitted-today/5
        [HttpGet("check-submitted-today/{technicianId}")]
        public async Task<IActionResult> CheckSubmittedToday(int technicianId)
        {
            var today = DateTime.Today;
            var submitted = await _context.DailyWorkReports
                .AnyAsync(d => d.TechnicianID == technicianId && d.CreatedAt.HasValue && d.CreatedAt.Value.Date == today);
            
            return Ok(new { submitted });
        }

        // POST: api/DailyWorkReports/remind-missing/{technicianId}
        [HttpPost("remind-missing/{technicianId}")]
        public async Task<IActionResult> RemindMissing(int technicianId)
        {
            var today = DateTime.Today;
            var submitted = await _context.DailyWorkReports
                .AnyAsync(d => d.TechnicianID == technicianId && d.CreatedAt.HasValue && d.CreatedAt.Value.Date == today);

            if (!submitted)
            {
                var notif = new Notification
                {
                    TechnicianId = technicianId,
                    Title = "Daily Report Missing",
                    Message = "You have not submitted your daily work report. Please submit the daily work report.",
                    Type = "dwr_reminder",
                    Date = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();
                return Ok(new { reminded = true });
            }
            return Ok(new { reminded = false });
        }

        // POST: api/DailyWorkReports
        [HttpPost]
        public async Task<ActionResult<DailyWorkReport>> PostDWR([FromBody] DwrSubmitDto dto)
        {
            var report = new DailyWorkReport
            {
                TechnicianID = dto.TechnicianID,
                DutyIn       = dto.DutyIn,
                DutyOut      = dto.DutyOut,
                PendingJobs   = dto.PendingJobs ?? 0,
                RepeatJobs    = dto.RepeatJobs ?? 0,
                CompletedJobs = dto.CompletedJobs ?? 0,
                WalletBalance = dto.WalletBalance ?? 0,
                FinalNote     = dto.FinalNote,
                Record        = dto.Record,
                Status        = "Submitted",
                ReportDate    = DateTime.Now.Date,
                CreatedAt     = DateTime.Now,
            };

            // Auto-assign CompanyID from technician record
            var tech = await _context.Technicians.FindAsync(dto.TechnicianID);
            if (tech != null) report.CompanyID = tech.CompanyID;

            _context.DailyWorkReports.Add(report);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDWR", new { id = report.DWRID }, report);
        }

        // GET: api/DailyWorkReports/search
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? companyName, [FromQuery] DateTime? date)
        {
            var query = _context.DailyWorkReports
                .Include(d => d.Company)
                .Include(d => d.Technician)
                .AsQueryable();

            if (!string.IsNullOrEmpty(companyName))
            {
                query = query.Where(d => d.Company.CompanyName.Contains(companyName));
            }

            if (date.HasValue)
            {
                query = query.Where(d => d.CreatedAt.HasValue && d.CreatedAt.Value.Date == date.Value.Date);
            }

            var results = await query.Select(d => new
            {
                d.CompanyID,
                d.Company.CompanyName,
                d.TechnicianID,
                TechnicianName = d.Technician.FullName,
                d.DWRID,
                ReportDate = d.ReportDate ?? d.CreatedAt,
                d.Status
            }).ToListAsync();

            return Ok(results);
        }

        // GET: api/DailyWorkReports/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDWR(int id)
        {
            var dwr = await _context.DailyWorkReports
                .Include(d => d.Technician)
                .Where(d => d.DWRID == id)
                .Select(d => new
                {
                    d.DWRID,
                    TechnicianMobile = d.Technician.Phone,
                    d.WalletBalance,
                    d.ToolBox,
                    d.SpareParts,
                    d.FinalNote,
                    d.Record,
                    d.DutyIn,
                    d.DutyOut,
                    d.PendingJobs,
                    d.RepeatJobs,
                    d.CompletedJobs,
                    ReportDate = d.ReportDate ?? d.CreatedAt,
                    d.Status
                })
                .FirstOrDefaultAsync();

            if (dwr == null) return NotFound();

            return Ok(dwr);
        }
    }
}
