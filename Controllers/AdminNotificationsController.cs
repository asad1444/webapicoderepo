using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminNotificationsController(AppDbContext context) { _context = context; }

        // GET: api/AdminNotifications
        [HttpGet]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var list = await _context.AdminNotifications
                .OrderByDescending(n => n.Date)
                .Select(n => new {
                    n.Id, n.Title, n.Message, n.Type, n.CompanyId, n.Date, n.IsRead
                })
                .ToListAsync();

            return Ok(new {
                UnreadCount   = list.Count(n => !n.IsRead),
                Notifications = list
            });
        }

        // POST: api/AdminNotifications/{id}/read
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _context.AdminNotifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Marked as read" });
        }
    }
}
