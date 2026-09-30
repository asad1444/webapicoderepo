using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechniciansController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TechniciansController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Technicians/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var summary = new
            {
                TotalFleet = await _context.Technicians.CountAsync(),
                ActiveOnlineNow = await _context.Technicians.CountAsync(t => t.LiveStatus == "Online" && t.ApprovalStatus == "Active"),
                OnAJob = await _context.Technicians.CountAsync(t => t.LiveStatus == "OnJob"),
                OffDutyInactive = await _context.Technicians.CountAsync(t => t.LiveStatus == "Offline")
            };

            return Ok(summary);
        }

        // GET: api/Technicians
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTechnicians([FromQuery] string? liveStatus)
        {
            var query = _context.Technicians
                .Include(t => t.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(liveStatus) && !liveStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.LiveStatus == liveStatus);
            }

            var technicians = await query.Select(t => new
            {
                technicianID = t.TechnicianID,
                fullName = t.FullName,
                photo = t.Photo,
                phone = t.Phone,
                liveStatus = t.LiveStatus,
                approvalStatus = t.ApprovalStatus,
                designation = t.Designation,
                totalJobsSolved = _context.Jobs.Count(j => j.TechnicianID == t.TechnicianID && j.Status == "Completed"),
                wallet = t.WalletBalance,
                companyID = t.CompanyID,
                companyName = t.Company != null ? t.Company.CompanyName : "Unknown Company"
            }).ToListAsync();

            return Ok(technicians);
        }

        // GET: api/Technicians/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTechnicianById(int id)
        {
            var technician = await _context.Technicians
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t => t.TechnicianID == id);

            if (technician == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                technician.TechnicianID,
                technician.FullName,
                technician.Photo,
                technician.Phone,
                technician.Email,
                technician.Designation,
                technician.LiveStatus,
                technician.ApprovalStatus,
                technician.CompanyID,
                CompanyName = technician.Company != null ? technician.Company.CompanyName : "Unknown Company",
                TotalJobsSolved = _context.Jobs.Count(j => j.TechnicianID == technician.TechnicianID && j.Status == "Completed"),
                technician.WalletBalance,
            });
        }

        // PUT: api/Technicians/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string approvalStatus)
        {
            var validStatuses = new[] { "Pending", "Approval", "Suspend" };
            if (!validStatuses.Contains(approvalStatus))
                return BadRequest("Invalid status requested.");

            var tech = await _context.Technicians.FindAsync(id);
            if (tech == null) return NotFound();

            if (approvalStatus == "Approval") tech.ApprovalStatus = "Active";
            else if (approvalStatus == "Suspend") tech.ApprovalStatus = "Suspended";
            else tech.ApprovalStatus = approvalStatus;

            _context.Entry(tech).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Technicians/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTechnician(int id, [FromBody] UpdateTechnicianDto dto)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest(new { message = "Full name, email, and phone are required." });

            technician.FullName = dto.FullName.Trim();
            technician.Email = dto.Email.Trim();
            technician.Phone = dto.Phone.Trim();
            technician.Designation = string.IsNullOrWhiteSpace(dto.Designation) ? technician.Designation : dto.Designation.Trim();
            technician.Photo = dto.Photo ?? technician.Photo;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Technician updated successfully." });
        }

        // DELETE: api/Technicians/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null)
            {
                return NotFound();
            }

            _context.Technicians.Remove(technician);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UpdateTechnicianDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Photo { get; set; }
    }
}
