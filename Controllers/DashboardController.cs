using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Dashboard/kpi
        [HttpGet("kpi")]
        public async Task<IActionResult> GetKPI()
        {
            var kpi = new
            {
                TotalJobs = await _context.Jobs.CountAsync(),
                Companies = await _context.Companies.CountAsync(),
                Foremen = await _context.Technicians.CountAsync(t => t.Designation == "Foreman" || t.Designation == "Supervisor")
            };

            return Ok(kpi);
        }

        // GET: api/Dashboard/charts
        [HttpGet("charts")]
        public async Task<IActionResult> GetCharts([FromQuery] string range = "monthly")
        {
            var now = DateTime.Now;
            range = range.ToLowerInvariant();

            // Job Distribution Pie Chart
            var jobDistribution = new
            {
                Done = await _context.Jobs.CountAsync(j => j.Status == "Completed"),
                InProgress = await _context.Jobs.CountAsync(j => j.Status == "In Progress" || j.Status == "Dispatched"),
                Pending = await _context.Jobs.CountAsync(j => j.Status == "Open"),
                Cancelled = await _context.Jobs.CountAsync(j => j.Status == "Cancelled")
            };

            var from = range switch
            {
                "weekly" => now.Date.AddDays(-6),
                "yearly" => new DateTime(now.Year, 1, 1),
                _ => new DateTime(now.Year, now.Month, 1).AddMonths(-5)
            };
            var jobDates = await _context.Jobs
                .Where(j => j.CreatedAt.HasValue && j.CreatedAt.Value >= from)
                .Select(j => j.CreatedAt!.Value)
                .ToListAsync();

            var monthlyJobs = range == "weekly"
                ? Enumerable.Range(0, 7).Select(i => {
                    var date = now.Date.AddDays(i - 6);
                    return new { Label = date.ToString("ddd"), Count = jobDates.Count(value => value.Date == date) };
                }).ToList()
                : Enumerable.Range(0, range == "yearly" ? 12 : 6).Select(i => {
                    var date = range == "yearly" ? new DateTime(now.Year, i + 1, 1) : now.AddMonths(i - 5);
                    return new { Label = date.ToString("MMM"), Count = jobDates.Count(value => value.Year == date.Year && value.Month == date.Month) };
                }).ToList();

            return Ok(new
            {
                JobDistribution = jobDistribution,
                TotalAccumulatedJobs = await _context.Jobs.CountAsync(), // Show Total Metric Baseline
                MonthlyTrend = monthlyJobs
            });
        }

        [HttpGet("live-map")]
        public async Task<IActionResult> GetLiveMap()
        {
            var activeTechs = await _context.Technicians
                .Where(t => t.LiveStatus == "Online" || t.LiveStatus == "OnJob" || t.LiveStatus == "On a Job")
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
                    FullName = t.FullName,
                    TechnicianName = t.FullName,
                    Latitude = lat,
                    Longitude = lng,
                    RecordedAt = loc != null ? loc.RecordedAt : System.DateTime.Now,
                    LiveStatus = "On a Job", // Force 'On a Job' for anyone appearing on this map
                    LocationName = locationName
                };
            });

            return Ok(latestLocations);
        }

        // GET: api/Dashboard/live-jobs
        [HttpGet("live-jobs")]
        public async Task<IActionResult> GetLiveJobs()
        {
            // Live Job Status Board Snapshot
            var liveJobs = await _context.Jobs
                .Include(j => j.Client)
                .Include(j => j.Request)
                .Include(j => j.Technician)
                .OrderByDescending(j => j.CreatedAt)
                .Take(20) // Limit to top 20 recent live jobs for UI grid
                .Select(j => new
                {
                    JobID = $"SPM-{j.JobID:D4}",
                    CustomerDetails = new 
                    {
                        Name = j.Client != null ? j.Client.ClientName : (j.Request != null ? j.Request.ClientName : null),
                        DisplayID = j.Client != null ? (j.Client.ClientCode ?? $"C-{j.ClientID}") : (j.Request != null ? j.Request.ClientRequestNumber : null),
                        Phone = j.Client != null ? j.Client.Phone : (j.Request != null ? j.Request.ClientContact : null)
                    },
                    AssignedTech = j.Technician != null ? j.Technician.FullName : "Unassigned",
                    StatusFlag = j.Status,
                    FinancialAmount = _context.Set<Invoice>().Where(i => i.JobID == j.JobID).Select(i => i.GrandTotal).FirstOrDefault().ToString("0.##") == "0" ? "PKR 0" : "PKR " + _context.Set<Invoice>().Where(i => i.JobID == j.JobID).Select(i => i.GrandTotal).FirstOrDefault().ToString("0.##"),
                    TimeMetric = j.CreatedAt
                }).ToListAsync();

            return Ok(liveJobs);
        }
    }
}
