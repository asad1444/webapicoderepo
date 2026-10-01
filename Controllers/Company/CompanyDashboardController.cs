using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/Dashboard")]
    [ApiController]
    public class CompanyDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyDashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Company/{companyId}/Dashboard/kpi
        [HttpGet("kpi")]
        public async Task<IActionResult> GetDashboardKPIs(int companyId)
        {
            var companyJobs = _context.Jobs.Where(j => j.CompanyID == companyId && j.AssignmentSource == "Company");
            
            var totalJobs = await companyJobs.CountAsync();
            var inProgressJobs = await companyJobs.CountAsync(j =>
                j.Status == "In Progress" || j.Status == "On Route" || j.Status == "Arrived");
            var pendingJobs = await companyJobs.CountAsync(j =>
                j.Status == "Pending" || j.Status == "Assigned" || j.Status == "Open");
            var completedJobs = await companyJobs.CountAsync(j => j.Status == "Completed");
            var canceledJobs = await companyJobs.CountAsync(j =>
                j.Status == "Canceled" || j.Status == "Cancelled");

            var activeTechnicians = await _context.Technicians
                .CountAsync(t => t.CompanyID == companyId && t.LiveStatus == "Online");

            // Calculate Monthly Revenue (Assuming from MasterCards or Invoices)
            // Simplified logic for example: summing GrandTotal of ProformaInvoices belonging to this company's jobs
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // SQLite fix: fetch to memory then sum as double
            var monthlyRevenueRaw = await _context.ProformaInvoices
                .Include(p => p.MasterCard)
                .Where(p => p.MasterCard.CompanyID == companyId && p.CreatedAt.HasValue && p.CreatedAt.Value.Month == currentMonth && p.CreatedAt.Value.Year == currentYear)
                .Select(p => (double)(p.GrandTotal ?? 0))
                .ToListAsync();
            var monthlyRevenue = (decimal)monthlyRevenueRaw.Sum();

            return Ok(new
            {
                TotalJobs = totalJobs,
                MonthlyRevenue = monthlyRevenue,
                ActiveTechnicians = activeTechnicians,
                InProgress = inProgressJobs,
                Pending = pendingJobs,
                Completed = completedJobs,
                Canceled = canceledJobs
            });
        }

        // GET: api/Company/{companyId}/Dashboard/job-status-chart
        [HttpGet("job-status-chart")]
        public async Task<IActionResult> GetJobStatusChart(int companyId)
        {
            var companyJobs = _context.Jobs.Where(j => j.CompanyID == companyId && j.AssignmentSource == "Company");
            
            var completed = await companyJobs.CountAsync(j => j.Status == "Completed");
            var inProgress = await companyJobs.CountAsync(j =>
                j.Status == "In Progress" || j.Status == "On Route" || j.Status == "Arrived");
            var pending = await companyJobs.CountAsync(j =>
                j.Status == "Pending" || j.Status == "Assigned" || j.Status == "Open");
            var canceled = await companyJobs.CountAsync(j =>
                j.Status == "Canceled" || j.Status == "Cancelled");

            return Ok(new
            {
                Completed = completed,
                InProgress = inProgress,
                Pending = pending,
                Canceled = canceled
            });
        }

        [HttpGet("live-job-locations")]
        public async Task<IActionResult> GetLiveJobLocations(int companyId)
        {
            var activeTechs = await _context.Technicians
                .Where(t => t.CompanyID == companyId && (t.LiveStatus == "Online" || t.LiveStatus == "OnJob" || t.LiveStatus == "On a Job"))
                .ToListAsync();

            var locationRecords = await _context.TechnicianLocations
                .Where(l => activeTechs.Select(t => t.TechnicianID).Contains(l.TechnicianID))
                .ToListAsync();

            var activeJobs = await _context.Jobs
                .Include(j => j.Request)
                .Where(j => activeTechs.Select(t => (int?)t.TechnicianID).Contains(j.TechnicianID) && (j.Status == "In Progress" || j.Status == "On Route" || j.Status == "Arrived"))
                .ToListAsync();

            var random = new System.Random();
            var latestLocations = activeTechs.Select(t =>
            {
                var loc = locationRecords.Where(l => l.TechnicianID == t.TechnicianID)
                                         .OrderByDescending(l => l.RecordedAt)
                                         .FirstOrDefault();

                var job = activeJobs.Where(j => j.TechnicianID == t.TechnicianID)
                                    .OrderByDescending(j => j.CreatedAt)
                                    .FirstOrDefault();
                
                string[] dummyCities = { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Peshawar", "Quetta", "Multan", "Faisalabad" };
                string locationName = job?.Request?.ServiceAddress ?? job?.Request?.Zone;
                if (string.IsNullOrWhiteSpace(locationName) || locationName == "Unknown Location") 
                {
                    locationName = dummyCities[random.Next(dummyCities.Length)] + ", Pakistan";
                }
                
                // If no real location exists, assign a dummy one around Islamabad but spread them widely so they don't overlap
                decimal lat = loc != null && loc.Latitude.HasValue ? loc.Latitude.Value : (decimal)(33.6844 + (random.NextDouble() * 8.0 - 4.0));
                decimal lng = loc != null && loc.Longitude.HasValue ? loc.Longitude.Value : (decimal)(73.0479 + (random.NextDouble() * 8.0 - 4.0));

                return new
                {
                    TechnicianID = t.TechnicianID,
                    TechnicianName = t.FullName,
                    FullName = t.FullName,
                    Latitude = lat,
                    Longitude = lng,
                    RecordedAt = loc != null ? loc.RecordedAt : System.DateTime.Now,
                    LiveStatus = "On a Job", // Force 'On a Job' for anyone appearing on this map
                    LocationName = locationName
                };
            });

            return Ok(latestLocations);
        }

        // GET: api/Company/{companyId}/Dashboard/recent-jobs
        [HttpGet("recent-jobs")]
        public async Task<IActionResult> GetRecentJobs(int companyId)
        {
            var recentJobs = await _context.Jobs
                .Include(j => j.Client)
                .Include(j => j.Request)
                .Include(j => j.Technician)
                .Where(j => j.CompanyID == companyId && j.AssignmentSource == "Company")
                .OrderByDescending(j => j.CreatedAt)
                .Take(10)
                .Select(j => new
                {
                    j.JobID,
                    j.CRNumber,
                    ClientName = j.Request != null ? j.Request.ClientName : j.Client != null ? j.Client.ClientName : "Unknown client",
                    Category = j.Request != null ? j.Request.Category : j.Category,
                    TechnicianName = j.Technician != null ? j.Technician.FullName : "Unassigned",
                    j.Status,
                    j.CreatedAt,
                    Amount = _context.MasterCards
                                .Where(m => m.JobID == j.JobID)
                                .Join(_context.ProformaInvoices, m => m.MasterCardID, p => p.MasterCardID, (m, p) => p.GrandTotal)
                                .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(recentJobs);
        }

        // GET: api/Company/{companyId}/Dashboard/monthly-distribution
        // Bar chart data — last 6 months job counts
        [HttpGet("monthly-distribution")]
        public async Task<IActionResult> GetMonthlyDistribution(int companyId, [FromQuery] string range = "monthly")
        {
            var now = DateTime.Now;
            range = range.ToLowerInvariant();
            var from = range switch
            {
                "weekly" => now.Date.AddDays(-6),
                "yearly" => new DateTime(now.Year, 1, 1),
                _ => new DateTime(now.Year, now.Month, 1).AddMonths(-5)
            };

            var raw = await _context.Jobs
                .Where(j => j.CompanyID == companyId && j.AssignmentSource == "Company"
                         && j.CreatedAt.HasValue
                         && j.CreatedAt.Value >= from)
                .Select(j => j.CreatedAt!.Value)
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<int>();
            if (range == "weekly")
            {
                for (int i = 6; i >= 0; i--)
                {
                    var target = now.Date.AddDays(-i);
                    labels.Add(target.ToString("ddd"));
                    values.Add(raw.Count(date => date.Date == target));
                }
            }
            else if (range == "yearly")
            {
                for (int month = 1; month <= 12; month++)
                {
                    labels.Add(new DateTime(now.Year, month, 1).ToString("MMM"));
                    values.Add(raw.Count(date => date.Year == now.Year && date.Month == month));
                }
            }
            else
            {
                for (int i = 5; i >= 0; i--)
                {
                    var target = now.AddMonths(-i);
                    labels.Add(target.ToString("MMM"));
                    values.Add(raw.Count(date => date.Year == target.Year && date.Month == target.Month));
                }
            }

            return Ok(new { labels, values });
        }

        // GET: api/Company/{companyId}/Dashboard/job-trend
        // Sparkline data for Total Jobs card — last 12 months job counts
        [HttpGet("job-trend")]
        public async Task<IActionResult> GetJobTrend(int companyId)
        {
            var now  = DateTime.Now;
            var from = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var raw = await _context.Jobs
                .Where(j => j.CompanyID == companyId && j.AssignmentSource == "Company"
                         && j.CreatedAt.HasValue
                         && j.CreatedAt.Value >= from)
                .Select(j => new { j.CreatedAt!.Value.Year, j.CreatedAt.Value.Month })
                .ToListAsync();

            var trend = Enumerable.Range(0, 12)
                .Select(i => {
                    var t = now.AddMonths(-(11 - i));
                    return (int)raw.Count(r => r.Year == t.Year && r.Month == t.Month);
                })
                .ToList();

            return Ok(trend);
        }

        // GET: api/Company/{companyId}/Dashboard/revenue-trend
        // Sparkline data for Revenue card — last 12 months revenue
        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend(int companyId)
        {
            var now  = DateTime.Now;
            var from = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var raw = await _context.ProformaInvoices
                .Include(p => p.MasterCard)
                .Where(p => p.MasterCard.CompanyID == companyId
                         && p.CreatedAt.HasValue
                         && p.CreatedAt.Value >= from)
                .Select(p => new
                {
                    p.CreatedAt!.Value.Year,
                    p.CreatedAt.Value.Month,
                    Revenue = p.GrandTotal ?? 0
                })
                .ToListAsync();

            var trend = Enumerable.Range(0, 12)
                .Select(i => {
                    var t = now.AddMonths(-(11 - i));
                    return (int)raw.Where(r => r.Year == t.Year && r.Month == t.Month)
                                    .Sum(r => (double)r.Revenue);
                })
                .ToList();

            return Ok(trend);
        }

        // GET: api/Company/{companyId}/Dashboard/technician-stats
        // "On Leave" count for technician card
        [HttpGet("technician-stats")]
        public async Task<IActionResult> GetTechnicianStats(int companyId)
        {
            var techs = await _context.Technicians
                .Where(t => t.CompanyID == companyId)
                .Select(t => new {
                    t.TechnicianID,
                    t.FullName,
                    t.LiveStatus,
                    t.ApprovalStatus,
                    t.Photo
                })
                .ToListAsync();

            return Ok(new
            {
                total      = techs.Count,
                online     = techs.Count(t => t.LiveStatus == "Online"),
                onJob      = techs.Count(t => t.LiveStatus == "On a Job"),
                onLeave    = techs.Count(t => t.LiveStatus == "On Leave"),
                offline    = techs.Count(t => t.LiveStatus == "Offline" || t.LiveStatus == null),
                technicians = techs
            });
        }
    }
}
