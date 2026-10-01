using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.DTOs.Technician;
using System.Text.Json;

namespace SmartProManWebAPI.Controllers.Technician
{
    [Route("api/technician/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/dashboard/{technicianId}
        // Main dashboard — all KPI cards + notifications
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}")]
        public async Task<IActionResult> GetDashboard(int technicianId)
        {
            var technician = await _context.Technicians
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId);

            if (technician == null) return NotFound("Technician not found.");

            var today = DateTime.Today;

            var jobs = await _context.Jobs
                .Where(j => j.TechnicianID == technicianId)
                .ToListAsync();

            // Today's jobs = assigned or created today
            var todaysJobs = jobs.Count(j =>
                j.Status != "Completed" &&
                ((j.AssignedAt.HasValue && j.AssignedAt.Value.Date == today) ||
                 (j.CreatedAt.HasValue  && j.CreatedAt.Value.Date  == today)));

            var pendingJobs    = jobs.Count(j => j.Status == "Assigned" || j.Status == "Open");
            var completedJobs  = jobs.Count(j => j.Status == "Completed");
            var stuckJobs      = jobs.Count(j => j.Status == "Stuck");
            var inProgressJobs = jobs.Count(j => j.Status == "In Progress" || j.Status == "On Route" || j.Status == "Arrived");
            var repeatJobs     = jobs.Count(j => j.Category == "Repeat Call");

            // Today's collection — SQLite fix: fetch to memory then sum
            var todaysCollectionRaw = await _context.Payments
                .Where(p =>
                    p.IsSuccessful &&
                    p.PaymentDate.Date == today &&
                    p.Invoice.Job.TechnicianID == technicianId)
                .Select(p => (double)p.AmountReceived)
                .ToListAsync();
            var todaysCollection = (decimal)todaysCollectionRaw.Sum();

            var recentNotifications = await _context.Notifications
                .Where(n => n.TechnicianId == technicianId && !n.IsRead && n.Type != "stuck" && n.Type != "delay")
                .OrderByDescending(n => n.Date)
                .Take(5)
                .Select(n => new NotificationDto
                {
                    Id             = n.Id,
                    Title          = n.Title,
                    Message        = n.Message,
                    Date           = n.Date,
                    RelatedRecordId = n.RelatedRecordId,
                    RelatedModule  = n.RelatedModule,
                    IsRead         = n.IsRead
                })
                .ToListAsync();

            var isOnDuty = technician.LiveStatus == "Online" || technician.LiveStatus == "On a Job";

            return Ok(new
            {
                TechnicianName   = technician.FullName,
                TechnicianCode   = technician.TechnicianCode,
                CompanyName      = technician.Company?.CompanyName,
                Photo            = technician.Photo,
                DutyStatus       = technician.LiveStatus ?? "Offline",
                IsOnDuty         = isOnDuty,
                is_on_duty       = isOnDuty,
                // snake_case aliases for the Flutter app
                today_jobs       = todaysJobs,
                pending_jobs     = pendingJobs,
                in_progress_jobs = inProgressJobs,
                completed_jobs   = completedJobs,
                repeat_jobs      = repeatJobs,
                stuck_jobs       = stuckJobs,
                today_collection = todaysCollection,
                notifications    = recentNotifications,
                // legacy/current names
                TodaysJobs       = new { Count = todaysJobs,      Filter = "today" },
                PendingJobs      = new { Count = pendingJobs,     Filter = "assigned" },
                InProgressJobs   = new { Count = inProgressJobs,  Filter = "inprogress" },
                CompletedJobs    = new { Count = completedJobs,   Filter = "completed" },
                StuckJobs        = new { Count = stuckJobs,       Filter = "stuck" },
                RepeatJobs       = new { Count = repeatJobs,      Filter = "repeat" },
                TodaysCollection = new
                {
                    Amount  = todaysCollection,
                    Display = $"PKR {todaysCollection:N0}",
                    Filter  = "today"
                },
                RecentNotifications = recentNotifications
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/technician/dashboard/{technicianId}/notifications
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{technicianId}/notifications")]
        public async Task<IActionResult> GetNotifications(int technicianId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.TechnicianId == technicianId && !n.IsRead && n.Type != "stuck" && n.Type != "delay")
                .OrderByDescending(n => n.Date)
                .Select(n => new NotificationDto
                {
                    Id              = n.Id,
                    Title           = n.Title,
                    Message         = n.Message,
                    Date            = n.Date,
                    RelatedRecordId = n.RelatedRecordId,
                    RelatedModule   = n.RelatedModule,
                    IsRead          = n.IsRead
                })
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);
            return Ok(new { UnreadCount = unreadCount, Notifications = notifications });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/technician/dashboard/notifications/{id}/read
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT: api/technician/dashboard/{technicianId}/duty-status
        // Toggle duty status Online / Offline
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{technicianId}/duty-status")]
        public async Task<IActionResult> UpdateDutyStatus(int technicianId, [FromBody] JsonElement payload)
        {
            string? status = null;

            if (payload.ValueKind == JsonValueKind.String)
            {
                status = payload.GetString();
            }
            else if (payload.ValueKind == JsonValueKind.Object)
            {
                if (payload.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.String)
                    status = statusProp.GetString();
                else if (payload.TryGetProperty("is_on_duty", out var dutyProp))
                    status = dutyProp.ValueKind == JsonValueKind.True ? "Online" : "Offline";
                else if (payload.TryGetProperty("isOnDuty", out var dutyProp2))
                    status = dutyProp2.ValueKind == JsonValueKind.True ? "Online" : "Offline";
            }
            else if (payload.ValueKind == JsonValueKind.True || payload.ValueKind == JsonValueKind.False)
            {
                status = payload.GetBoolean() ? "Online" : "Offline";
            }

            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status must be 'Online' or 'Offline'.");

            var normalized = status.Trim();
            if (normalized.Equals("on", StringComparison.OrdinalIgnoreCase)) normalized = "Online";
            if (normalized.Equals("off", StringComparison.OrdinalIgnoreCase)) normalized = "Offline";

            var allowed = new[] { "Online", "Offline" };
            if (!allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                return BadRequest("Status must be 'Online' or 'Offline'.");

            var tech = await _context.Technicians.FindAsync(technicianId);
            if (tech == null) return NotFound("Technician not found.");

            if (tech.ApprovalStatus == "Suspended")
                return BadRequest("Suspended technicians cannot change their duty status.");

            if (normalized.Equals("Online", StringComparison.OrdinalIgnoreCase))
            {
                tech.LastDutyIn = DateTime.Now;
            }
            else
            {
                tech.LastDutyOut = DateTime.Now;
            }
            
            tech.LiveStatus = normalized;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Duty status updated to {normalized}.", DutyStatus = normalized, is_on_duty = normalized.Equals("Online", StringComparison.OrdinalIgnoreCase) });
        }
    }
}
