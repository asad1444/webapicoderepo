using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace SmartProManWebAPI.Controllers.Company
{
    [Route("api/Company/{companyId}/[controller]")]
    [ApiController]
    public class CompanyTechnicianController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyTechnicianController(AppDbContext context)
        {
            _context = context;
        }

        // ═════════════════════════════════════════════════════════════════════
        // POST: api/Company/{companyId}/CompanyTechnician/add
        // Company adds a new technician
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("add")]
        public async Task<IActionResult> AddTechnician(int companyId, [FromBody] AddTechnicianDto dto)
        {
            // Validate company exists
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            // Validate input
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if phone already exists
            var existingTech = await _context.Technicians
                .FirstOrDefaultAsync(t => t.Phone == dto.Phone);
            if (existingTech != null)
                return BadRequest(new { message = "A technician with this phone number already exists." });

            // Check if email already exists (if provided)
            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existingEmail = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.Email == dto.Email);
                if (existingEmail != null)
                    return BadRequest(new { message = "A technician with this email already exists." });
            }

            // Validate CNIC format (13 digits)
            if (dto.CNIC.Length != 13 || !dto.CNIC.All(char.IsDigit))
                return BadRequest(new { message = "CNIC must be exactly 13 digits." });

            // Validate password match
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { message = "Password and Confirm Password do not match." });

            // Generate unique technician code
            var techCount = await _context.Technicians.CountAsync(t => t.CompanyID == companyId);
            var techCode = $"TECH-{companyId}-{techCount + 1:D4}";

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create technician
            var technician = new Models.Technician
            {
                CompanyID = companyId,
                TechnicianCode = techCode,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Designation = dto.Designation,
                Cnic = dto.CNIC,

                // Database ke liye required hai,
                // lekin user se input nahi lena
                LicenseCertification = "Not Provided",
                PasswordResetToken = "",
                PasswordHash = passwordHash,
                IsDefaultPassword = true,
                ApprovalStatus = "Active",
                LiveStatus = "Offline",
                DateOfJoining = DateTime.Now,
                CreatedAt = DateTime.Now,
                WalletBalance = 0,
                TotalJobsSolved = 0,
                Rating = 0
            };
            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Technician added successfully.",
                TechnicianID = technician.TechnicianID,
                TechnicianCode = techCode,
                FullName = technician.FullName,
                Phone = technician.Phone,
                Email = technician.Email,
                DefaultPassword = dto.Password // Return for company records
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // GET: api/Company/{companyId}/CompanyTechnician/list
        // Get all technicians for a company
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("list")]
        public async Task<IActionResult> GetTechnicians(int companyId, [FromQuery] string status = "all")
        {
            var query = _context.Technicians.Where(t => t.CompanyID == companyId);

            // Filter by status if specified
            if (status.ToLower() != "all")
            {
                query = query.Where(t => t.ApprovalStatus.ToLower() == status.ToLower());
            }

            var technicians = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TechnicianID,
                    t.TechnicianCode,
                    t.FullName,
                    t.Email,
                    t.Phone,
                    t.Photo,
                    t.Designation,
                    t.LiveStatus,
                    t.ApprovalStatus,
                    t.DateOfJoining,
                    TotalJobsSolved = _context.Jobs.Count(j => j.TechnicianID == t.TechnicianID && j.Status == "Completed"),
                    t.Rating,
                    t.WalletBalance,
                    t.ServiceAreas,
                    t.Skills,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                CompanyID = companyId,
                TotalTechnicians = technicians.Count,
                Technicians = technicians
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // GET: api/Company/{companyId}/CompanyTechnician/{technicianId}
        // Get technician details
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("{technicianId}")]
        public async Task<IActionResult> GetTechnicianDetails(int companyId, int technicianId)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);

            if (technician == null)
                return NotFound(new { message = "Technician not found or does not belong to your company." });

            return Ok(new
            {
                technician.TechnicianID,
                technician.TechnicianCode,
                technician.FullName,
                technician.Email,
                technician.Phone,
                technician.Photo,
                technician.Designation,
                technician.LiveStatus,
                technician.ApprovalStatus,
                technician.DateOfJoining,
                technician.ServiceAreas,
                technician.Skills,
                technician.LicenseCertification,
                TotalJobsSolved = _context.Jobs.Count(j => j.TechnicianID == technician.TechnicianID && j.Status == "Completed"),
                technician.Rating,
                technician.WalletBalance,
                technician.CreatedAt
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // PUT: api/Company/{companyId}/CompanyTechnician/{technicianId}/update
        // Update technician details
        // ═════════════════════════════════════════════════════════════════════
        [HttpPut("{technicianId}/update")]
        public async Task<IActionResult> UpdateTechnician(
            int companyId, 
            int technicianId, 
            [FromBody] UpdateTechnicianDto dto)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);

            if (technician == null)
                return NotFound(new { message = "Technician not found or does not belong to your company." });

            // Update fields
            if (!string.IsNullOrEmpty(dto.FullName))
                technician.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                // Check email uniqueness
                var existingEmail = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.Email == dto.Email && t.TechnicianID != technicianId);
                if (existingEmail != null)
                    return BadRequest(new { message = "Email already exists." });
                
                technician.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.Phone))
            {
                // Check phone uniqueness
                var existingPhone = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.Phone == dto.Phone && t.TechnicianID != technicianId);
                if (existingPhone != null)
                    return BadRequest(new { message = "Phone number already exists." });
                
                technician.Phone = dto.Phone;
            }

            if (!string.IsNullOrEmpty(dto.Designation))
                technician.Designation = dto.Designation;

            if (!string.IsNullOrEmpty(dto.ServiceAreas))
                technician.ServiceAreas = dto.ServiceAreas;

            if (!string.IsNullOrEmpty(dto.Skills))
                technician.Skills = dto.Skills;

            if (!string.IsNullOrEmpty(dto.LicenseCertification))
                technician.LicenseCertification = dto.LicenseCertification;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Technician updated successfully.",
                TechnicianID = technician.TechnicianID,
                FullName = technician.FullName
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // DELETE: api/Company/{companyId}/CompanyTechnician/{technicianId}/delete
        // Delete/Deactivate technician
        // ═════════════════════════════════════════════════════════════════════
        [HttpDelete("{technicianId}/delete")]
        public async Task<IActionResult> DeleteTechnician(int companyId, int technicianId)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);

            if (technician == null)
                return NotFound(new { message = "Technician not found or does not belong to your company." });

            // Check if technician has active jobs
            var hasActiveJobs = await _context.Jobs
                .AnyAsync(j => j.TechnicianID == technicianId && 
                             (j.Status == "In Progress" || j.Status == "Pending"));

            if (hasActiveJobs)
                return BadRequest(new { message = "Cannot delete technician with active jobs. Please reassign or complete jobs first." });

            // Soft delete - change status instead of removing
            technician.ApprovalStatus = "Inactive";
            technician.LiveStatus = "Offline";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Technician deactivated successfully." });
        }

        // ═════════════════════════════════════════════════════════════════════
        // POST: api/Company/{companyId}/CompanyTechnician/{technicianId}/reset-password
        // Company can reset technician password
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("{technicianId}/reset-password")]
        public async Task<IActionResult> ResetTechnicianPassword(
            int companyId, 
            int technicianId, 
            [FromBody] TechnicianResetPasswordDto dto)
        {
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.CompanyID == companyId);

            if (technician == null)
                return NotFound(new { message = "Technician not found or does not belong to your company." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            // Hash new password
            technician.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            technician.IsDefaultPassword = true; // Mark as default so tech must change on first login

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Password reset successfully.",
                TechnicianID = technician.TechnicianID,
                NewPassword = dto.NewPassword // Return for company to share with technician
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // GET: api/Company/{companyId}/CompanyTechnician/stats
        // Get technician statistics
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("stats")]
        public async Task<IActionResult> GetTechnicianStats(int companyId)
        {
            var technicians = await _context.Technicians
                .Where(t => t.CompanyID == companyId)
                .ToListAsync();

            var total = technicians.Count;
            var active = technicians.Count(t => t.ApprovalStatus == "Active");
            var online = technicians.Count(t => t.LiveStatus == "Online");
            var onJob = technicians.Count(t => t.LiveStatus == "On a Job");
            var offline = technicians.Count(t => t.LiveStatus == "Offline");
            var inactive = technicians.Count(t => t.ApprovalStatus == "Inactive");

            return Ok(new
            {
                CompanyID = companyId,
                TotalTechnicians = total,
                ActiveTechnicians = active,
                InactiveTechnicians = inactive,
                OnlineTechnicians = online,
                OnJobTechnicians = onJob,
                OfflineTechnicians = offline,
                AvailableForWork = online - onJob
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DTOs
    // ═══════════════════════════════════════════════════════════════════════════

    public class AddTechnicianDto
    {
        [Required(ErrorMessage = "Full Name is required")]
        [MaxLength(150)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        [MaxLength(13, ErrorMessage = "CNIC must be 13 digits")]
        [MinLength(13, ErrorMessage = "CNIC must be 13 digits")]
        public string CNIC { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [MaxLength(100)]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        public string ConfirmPassword { get; set; }
    }

    public class UpdateTechnicianDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Designation { get; set; }
        public string ServiceAreas { get; set; }
        public string Skills { get; set; }
        public string LicenseCertification { get; set; }
    }

    public class TechnicianResetPasswordDto
    {
        [Required(ErrorMessage = "New Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        public string ConfirmPassword { get; set; }
    }
}
