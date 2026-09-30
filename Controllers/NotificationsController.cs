using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public NotificationsController(AppDbContext context) { _context = context; }

        // ── Helper: create notification ───────────────────────────────────────
        private async Task CreateNotification(int technicianId, int? companyId,
            string title, string message, string type = "info",
            string? module = null, int? recordId = null)
        {
            _context.Notifications.Add(new Notification
            {
                TechnicianId    = technicianId,
                CompanyId       = companyId,
                Title           = title,
                Message         = message,
                Type            = type,
                RelatedModule   = module,
                RelatedRecordId = recordId,
                Date            = DateTime.Now,
                IsRead          = false,
            });
            await _context.SaveChangesAsync();
        }

        // ── GET: Notifications for technician ─────────────────────────────────
        [HttpGet("technician/{technicianId}")]
        public async Task<IActionResult> GetByTechnician(int technicianId)
        {
            var list = await _context.Notifications
                .Where(n => n.TechnicianId == technicianId)
                .OrderByDescending(n => n.Date)
                .Select(n => new {
                    n.Id, n.Title, n.Message, n.Type, n.Date,
                    n.IsRead, n.RelatedModule, n.RelatedRecordId
                })
                .ToListAsync();

            return Ok(new {
                UnreadCount   = list.Count(n => !n.IsRead),
                Notifications = list
            });
        }

        // ── GET: Notifications for company (all technicians of that company) ──
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(int companyId)
        {
            var list = await _context.Notifications
                .Include(n => n.Technician)
                .Where(n => n.CompanyId == companyId)
                .OrderByDescending(n => n.Date)
                .Select(n => new {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.Date,
                    n.IsRead,
                    n.RelatedModule,
                    n.RelatedRecordId,
                    TechnicianName = n.Technician.FullName,
                    n.TechnicianId,
                    TimeAgo = GetTimeAgo(n.Date),
                })
                .ToListAsync();

            return Ok(new {
                UnreadCount   = list.Count(n => !n.IsRead),
                Notifications = list
            });
        }

        // ── POST: Report Delay notification ───────────────────────────────────
        // POST: api/Notifications/report-delay
        [HttpPost("report-delay")]
        public async Task<IActionResult> ReportDelay([FromBody] ReportDto dto)
        {
            if (dto.TechnicianId == 0) return BadRequest("TechnicianId required.");

            // Get technician + job info
            var tech = await _context.Technicians.FindAsync(dto.TechnicianId);
            if (tech == null) return NotFound("Technician not found.");

            var job = dto.JobId > 0 ? await _context.Jobs.FindAsync(dto.JobId) : null;
            var jobRef = job != null ? $" (Job #{job.CRNumber ?? $"{job.JobID}"})" : "";

            var title   = $"🕐 Delay Report — {tech.FullName}";
            var message = $"Technician {tech.FullName} reported a delay{jobRef}.\n" +
                          $"Reason: {dto.Reason}\n" +
                          $"Expected Delay: {dto.ExpectedDelay}";

            // Save notification for company
            await CreateNotification(
                technicianId: dto.TechnicianId,
                companyId:    tech.CompanyID,
                title:        title,
                message:      message,
                type:         "delay",
                module:       "Job",
                recordId:     dto.JobId > 0 ? dto.JobId : null
            );

            return Ok(new { Message = "Delay notification sent.", Title = title });
        }

        // ── POST: Report Stuck notification ───────────────────────────────────
        // POST: api/Notifications/report-stuck
        [HttpPost("report-stuck")]
        public async Task<IActionResult> ReportStuck([FromBody] ReportDto dto)
        {
            if (dto.TechnicianId == 0) return BadRequest("TechnicianId required.");

            var tech = await _context.Technicians.FindAsync(dto.TechnicianId);
            if (tech == null) return NotFound("Technician not found.");

            var job = dto.JobId > 0 ? await _context.Jobs.FindAsync(dto.JobId) : null;
            var jobRef = job != null ? $" (Job #{job.CRNumber ?? $"{job.JobID}"})" : "";

            if (job != null)
            {
                job.Status = "Stuck";
            }

            var title   = $"⚠️ Stuck Report — {tech.FullName}";
            var message = $"Technician {tech.FullName} is stuck{jobRef}.\n" +
                          $"Reason: {dto.Reason}";

            await CreateNotification(
                technicianId: dto.TechnicianId,
                companyId:    tech.CompanyID,
                title:        title,
                message:      message,
                type:         "stuck",
                module:       "Job",
                recordId:     dto.JobId > 0 ? dto.JobId : null
            );

            return Ok(new { Message = "Stuck notification sent.", Title = title });
        }

        // ── POST: Mark single read ─────────────────────────────────────────────
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            n.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Marked as read." });
        }

        // ── POST: Mark all read for company ───────────────────────────────────
        [HttpPost("company/{companyId}/read-all")]
        public async Task<IActionResult> MarkAllReadCompany(int companyId)
        {
            var unread = await _context.Notifications
                .Where(n => n.CompanyId == companyId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{unread.Count} notification(s) marked as read." });
        }

        // ── POST: Mark all read for technician ────────────────────────────────
        [HttpPost("technician/{technicianId}/read-all")]
        public async Task<IActionResult> MarkAllRead(int technicianId)
        {
            var unread = await _context.Notifications
                .Where(n => n.TechnicianId == technicianId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{unread.Count} notification(s) marked as read." });
        }

        // ── DELETE ─────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            _context.Notifications.Remove(n);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Deleted." });
        }

        // ── POST: Send (admin) ─────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendNotificationDto dto)
        {
            if (!dto.TechnicianIds.Any()) return BadRequest("At least one technician required.");
            var notifs = dto.TechnicianIds.Select(id => new Notification {
                TechnicianId    = id,
                Title           = dto.Title,
                Message         = dto.Message,
                Type            = "info",
                RelatedModule   = dto.RelatedModule,
                RelatedRecordId = dto.RelatedRecordId,
                Date            = DateTime.Now,
            }).ToList();
            _context.Notifications.AddRange(notifs);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Sent to {notifs.Count} technician(s)." });
        }

        private static string GetTimeAgo(DateTime date)
        {
            var diff = DateTime.Now - date;
            if (diff.TotalMinutes < 1)  return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours} hr ago";
            if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays} days ago";
            return date.ToString("dd MMM yyyy");
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class ReportDto
    {
        public int    TechnicianId  { get; set; }
        public int    JobId         { get; set; }
        public string Reason        { get; set; } = "";
        public string ExpectedDelay { get; set; } = "";
    }

    public class SendNotificationDto
    {
        public List<int> TechnicianIds  { get; set; } = new();
        public string    Title          { get; set; } = "";
        public string    Message        { get; set; } = "";
        public string?   RelatedModule  { get; set; }
        public int?      RelatedRecordId { get; set; }
    }
}
