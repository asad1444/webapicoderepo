using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;

namespace SmartProManWebAPI.Controllers.Technician
{
    [Route("api/technician/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/profile/{technicianId}
        // Full profile page — Personal info, rating, company, stats
        // Matches the My Profile screen in the image
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}")]
        public async Task<IActionResult> GetProfile(int technicianId)
        {
            var tech = await _context.Technicians
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

            if (tech == null) return NotFound("Technician not found.");

            var totalJobs = await _context.Jobs
                .CountAsync(j => j.TechnicianID == technicianId);

            return Ok(new
            {
                // ── Header ───────────────────────────────────────────────────
                TechnicianID   = tech.TechnicianID,
                TechCode       = tech.TechnicianCode != null
                                     ? $"TECH-{tech.TechnicianCode}"
                                     : $"TECH-{tech.TechnicianID}",
                FullName       = tech.FullName,
                Photo          = tech.Photo,
                Designation    = tech.Designation ?? "Technician",
                LiveStatus     = tech.LiveStatus,
                Rating         = tech.Rating ?? 0,
                TotalJobs      = totalJobs,

                // ── Personal Information section ──────────────────────────────
                PersonalInfo = new
                {
                    FullName            = tech.FullName,
                    Email               = tech.Email,
                    Phone               = tech.Phone,
                    Cnic                = tech.Cnic,
                    TechnicianID        = tech.TechnicianCode != null
                                             ? $"TECH-{tech.TechnicianCode}"
                                             : $"TECH-{tech.TechnicianID}",
                    DateOfJoining       = tech.DateOfJoining.HasValue
                                             ? tech.DateOfJoining.Value.ToString("dd MMM yyyy")
                                             : tech.CreatedAt.HasValue
                                                 ? tech.CreatedAt.Value.ToString("dd MMM yyyy")
                                                 : null,
                    ServiceAreas        = tech.ServiceAreas,
                    Skills              = tech.Skills,
                    LicenseCertification = tech.LicenseCertification
                },

                // ── Company info ─────────────────────────────────────────────
                Company = tech.Company != null ? new
                {
                    tech.Company.CompanyName,
                    tech.Company.City,
                    tech.Company.Zone
                } : null,

                WalletBalance  = tech.WalletBalance ?? 0
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT: api/technician/profile/{technicianId}
        // Update editable profile fields
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{technicianId}")]
        public async Task<IActionResult> UpdateProfile(int technicianId, [FromBody] UpdateProfileDto dto)
        {
            var tech = await _context.Technicians.FindAsync(technicianId);
            if (tech == null) return NotFound("Technician not found.");

            // Only update fields that were provided
            if (dto.FullName            != null) tech.FullName             = dto.FullName;
            if (dto.Email               != null) tech.Email                = dto.Email;
            if (dto.Phone               != null) tech.Phone                = dto.Phone;
            if (dto.Photo               != null) tech.Photo                = dto.Photo;
            if (dto.Cnic                != null) tech.Cnic                 = dto.Cnic;
            if (dto.ServiceAreas        != null) tech.ServiceAreas         = dto.ServiceAreas;
            if (dto.Skills              != null) tech.Skills               = dto.Skills;
            if (dto.LicenseCertification != null) tech.LicenseCertification = dto.LicenseCertification;
            if (dto.Designation         != null) tech.Designation          = dto.Designation;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/profile/{technicianId}/jobs-summary?period=monthly
        // Jobs Summary section — Pie chart + Bar chart data
        // period: daily | weekly | monthly
        // Matches the Jobs Summary / Job Overview + Jobs Analytics section
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}/jobs-summary")]
        public async Task<IActionResult> GetJobsSummary(int technicianId, [FromQuery] string period = "monthly")
        {
            var now   = DateTime.Now;
            var today = DateTime.Today;

            // ── Determine date range from period ─────────────────────────────
            DateTime fromDate = period.ToLower() switch
            {
                "daily"   => today,
                "weekly"  => today.AddDays(-(int)today.DayOfWeek),   // start of this week (Sunday)
                "monthly" => new DateTime(now.Year, now.Month, 1),    // start of this month
                "overall" => DateTime.MinValue,
                _         => DateTime.MinValue
            };

            var allJobs = await _context.Jobs
                .Where(j =>
                    j.TechnicianID == technicianId &&
                    j.CreatedAt.HasValue &&
                    j.CreatedAt.Value >= fromDate)
                .ToListAsync();

            var totalJobs      = allJobs.Count;
            var completedJobs  = allJobs.Count(j => j.Status == "Completed");
            var inProgressJobs = allJobs.Count(j => j.Status == "In Progress" || j.Status == "On Route" || j.Status == "Arrived");
            var pendingJobs    = allJobs.Count(j => j.Status == "Assigned" || j.Status == "Open");
            var stuckJobs      = allJobs.Count(j => j.Status == "Stuck");
            var repeatJobs     = allJobs.Count(j => j.Category == "Repeat Call");

            // ── Pie chart data ────────────────────────────────────────────────
            // Calculate total for only Completed, In Progress, and Pending
            var pieTotal = completedJobs + inProgressJobs + pendingJobs;
            var pieChart = new
            {
                TotalJobs  = pieTotal,
                Segments   = new[]
                {
                    new
                    {
                        Label      = "Completed",
                        Count      = completedJobs,
                        Percentage = pieTotal > 0
                                         ? Math.Round((double)completedJobs  / pieTotal * 100, 1)
                                         : 0.0,
                        Color      = "#22c55e"   // green
                    },
                    new
                    {
                        Label      = "In Progress",
                        Count      = inProgressJobs,
                        Percentage = pieTotal > 0
                                         ? Math.Round((double)inProgressJobs / pieTotal * 100, 1)
                                         : 0.0,
                        Color      = "#f59e0b"   // orange
                    },
                    new
                    {
                        Label      = "Pending",
                        Count      = pendingJobs,
                        Percentage = pieTotal > 0
                                         ? Math.Round((double)pendingJobs    / pieTotal * 100, 1)
                                         : 0.0,
                        Color      = "#3b82f6"   // blue
                    }
                }
            };

            // ── Bar chart data — grouped by day label ─────────────────────────
            List<object> barChartData;

            if (period.ToLower() == "daily")
            {
                // Hourly breakdown for today (6AM – 10PM)
                barChartData = Enumerable.Range(6, 17).Select(hour =>
                {
                    var count = allJobs.Count(j =>
                        j.CreatedAt.HasValue && j.CreatedAt.Value.Hour == hour);
                    return (object)new { Label = $"{hour}:00", Count = count };
                }).ToList();
            }
            else if (period.ToLower() == "weekly")
            {
                // Mon – Sun
                var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                barChartData = dayNames.Select((name, i) =>
                {
                    // DayOfWeek: Monday=1, but our array index 0=Mon
                    var dow   = (DayOfWeek)((i + 1) % 7);
                    var count = allJobs.Count(j =>
                        j.CreatedAt.HasValue && j.CreatedAt.Value.DayOfWeek == dow);
                    return (object)new { Label = name, Count = count };
                }).ToList();
            }
            else if (period.ToLower() == "monthly")
            {
                // Monthly — group by week number within the month (Week 1 – 4/5)
                barChartData = allJobs
                    .GroupBy(j => (j.CreatedAt!.Value.Day - 1) / 7 + 1)
                    .OrderBy(g => g.Key)
                    .Select(g => (object)new
                    {
                        Label = $"W{g.Key}",
                        Count = g.Count()
                    })
                    .ToList();
            }
            else
            {
                // Overall — group by month of the current year (or cross-year)
                // Simply grouping by month-year
                barChartData = allJobs
                    .GroupBy(j => new { j.CreatedAt!.Value.Year, j.CreatedAt!.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => (object)new
                    {
                        Label = $"{System.Globalization.DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(g.Key.Month)} {g.Key.Year.ToString().Substring(2)}",
                        Count = g.Count()
                    })
                    .ToList();
            }

            return Ok(new
            {
                Period         = period,
                TotalJobs      = totalJobs,
                CompletedJobs  = completedJobs,
                InProgressJobs = inProgressJobs,
                PendingJobs    = pendingJobs,
                StuckJobs      = stuckJobs,
                RepeatJobs     = repeatJobs,
                PieChart       = pieChart,
                BarChart       = new
                {
                    Title = period.ToLower() switch
                    {
                        "daily"   => "Today's Jobs by Hour",
                        "weekly"  => "This Week's Jobs by Day",
                        "monthly" => "This Month's Jobs by Week",
                        _         => "Overall Jobs by Month"
                    },
                    Data = barChartData
                }
            });
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class UpdateProfileDto
    {
        public string? FullName             { get; set; }
        public string? Email                { get; set; }
        public string? Phone                { get; set; }
        public string? Cnic                 { get; set; }
        public string? Photo                { get; set; }
        public string? Designation          { get; set; }
        public string? ServiceAreas         { get; set; }
        public string? Skills               { get; set; }
        public string? LicenseCertification { get; set; }
    }
}
