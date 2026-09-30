using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using TechnicianModel = SmartProManWebAPI.Models.Technician;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/Technicians")]
    [ApiController]
    public class TechnicianControlCenterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TechnicianControlCenterController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Company/{companyId}/Technicians
        [HttpGet]
        public async Task<IActionResult> GetTechnicians(int companyId, [FromQuery] string? status = null)
        {
            var query = _context.Technicians.Where(t => t.CompanyID == companyId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.LiveStatus == status || t.ApprovalStatus == status);
            }

            var technicians = await query
                .Select(t => new
                {
                    t.TechnicianID,
                    t.TechnicianCode,
                    t.FullName,
                    t.Phone,
                    t.Designation,
                    t.WalletBalance,
                    t.LiveStatus,
                    t.ApprovalStatus,
                    t.TotalJobsSolved,
                    t.Photo
                })
                .ToListAsync();

            return Ok(technicians);
        }

        // POST: api/Company/{companyId}/Technicians
        [HttpPost]
        public async Task<IActionResult> AddTechnician(int companyId, [FromBody] TechnicianModel technicianDto)
        {
            if (technicianDto == null) return BadRequest("Invalid technician data");

            var companyExists = await _context.Companies.AnyAsync(c => c.CompanyID == companyId);
            if (!companyExists) return NotFound("Company not found");

            var duplicate = await _context.Technicians.AnyAsync(t => t.Phone == technicianDto.Phone);
            if (duplicate) return BadRequest("A technician with this phone number already exists.");

            technicianDto.CompanyID = companyId;
            technicianDto.ApprovalStatus = "Pending";
            technicianDto.LiveStatus = "Offline";
            technicianDto.WalletBalance = 0;
            technicianDto.TotalJobsSolved = 0;
            technicianDto.IsDefaultPassword = true;
            // Set a default password (Phone number hashed)
            technicianDto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(technicianDto.Phone ?? "Pass@1234");
            technicianDto.CreatedAt = DateTime.Now;

            _context.Technicians.Add(technicianDto);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Technician added successfully. Awaiting approval.", technicianId = technicianDto.TechnicianID });
        }

        // PUT: api/Company/{companyId}/Technicians/{technicianId}/status
        [HttpPut("{technicianId}/status")]
        public async Task<IActionResult> UpdateTechnicianStatus(int companyId, int technicianId, [FromBody] string status)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);

            if (technician == null)
                return NotFound("Technician not found or unauthorized access.");

            if (status == "Approved" || status == "Pending" || status == "Suspended")
            {
                technician.ApprovalStatus = status;
                if (status == "Suspended")
                    technician.LiveStatus = "Offline";
            }
            else if (status == "Online" || status == "Offline" || status == "On a Job")
            {
                if (technician.ApprovalStatus != "Approved" && technician.ApprovalStatus != "Active")
                    return BadRequest("Cannot change live status for a technician that is not approved.");

                technician.LiveStatus = status;
            }
            else
            {
                return BadRequest("Invalid status value.");
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Technician status updated successfully", status });
        }

        // GET: api/Company/{companyId}/Technicians/dashboard-summary
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary(int companyId)
        {
            var companyTechnicians = _context.Technicians.Where(t => t.CompanyID == companyId);

            return Ok(new
            {
                TotalFleet = await companyTechnicians.CountAsync(),
                ActiveOnline = await companyTechnicians.CountAsync(t => t.LiveStatus == "Online"),
                OnAJob = await companyTechnicians.CountAsync(t => t.LiveStatus == "On a Job"),
                OffDuty = await companyTechnicians.CountAsync(t => t.LiveStatus == "Offline" || t.ApprovalStatus == "Suspended")
            });
        }
    }
}
