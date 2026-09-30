using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Clients/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var summary = new
            {
                TotalClients = await _context.Clients.CountAsync(),
                ActiveAccounts = await _context.Clients.CountAsync(c => c.Status == "Active"),
                NewOnboardedThisMonth = await _context.Clients.CountAsync(c => c.CreatedAt.HasValue && c.CreatedAt.Value.Month == currentMonth && c.CreatedAt.Value.Year == currentYear),
                Suspended = await _context.Clients.CountAsync(c => c.Status == "Suspended")
            };

            return Ok(summary);
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetClients([FromQuery] string? zone)
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(zone) && !zone.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Zone == zone || c.TopZone == zone);
            }

            var clients = await query.Select(c => new
            {
                c.ClientID,
                c.ClientCode,
                c.ClientName,
                c.Logo,
                c.Phone,
                c.Email,
                c.Zone,
                LocationZone = c.Zone,
                c.Status
            }).ToListAsync();

            return Ok(clients);
        }

        // PUT: api/Clients/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (status != "Active" && status != "Suspend")
                return BadRequest("Invalid status. Must be 'Active' or 'Suspend'.");

            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            client.Status = status == "Suspend" ? "Suspended" : "Active";
            _context.Entry(client).State = EntityState.Modified;
            
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
