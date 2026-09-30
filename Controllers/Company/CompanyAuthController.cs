using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Helpers;
using SmartProManWebAPI.Services;
using CompanyModel = SmartProManWebAPI.Models.Company;
using AdminNotification = SmartProManWebAPI.Models.AdminNotification;

namespace SmartProManWebAPI.Controllers.Company
{
    /// <summary>
    /// Company Authentication Controller
    /// Complete JWT-based authentication system for companies
    /// Routes: /api/company/auth/*
    /// </summary>
    [Route("api/company/auth")]
    [ApiController]
    public class CompanyAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;

        public CompanyAuthController(AppDbContext context, JwtHelper jwtHelper, ISmsService smsService, IEmailService emailService)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _smsService = smsService;
            _emailService = emailService;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 📝 REGISTER - Company Registration
        // POST: /api/company/auth/register
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Register new company account
        /// Status will be "Pending" until admin approves
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CompanyRegisterDto dto)
        {
            // ── Validation ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                return BadRequest(new { message = "Company name is required." });

            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Password is required." });

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long." });

            if (string.IsNullOrWhiteSpace(dto.InchargeName))
                return BadRequest(new { message = "Incharge name is required." });

            if (string.IsNullOrWhiteSpace(dto.InchargePhone))
                return BadRequest(new { message = "Incharge phone is required." });

            // ── Check Duplicates ──────────────────────────────────────────────
            if (await _context.Companies.AnyAsync(c => c.Email == dto.Email))
                return BadRequest(new { message = "Email already registered. Please use a different email." });

            if (await _context.Companies.AnyAsync(c => c.InchargePhone == dto.InchargePhone))
                return BadRequest(new { message = "Phone number already registered." });

            // ── Hash Password ─────────────────────────────────────────────────
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // ── Create Company ────────────────────────────────────────────────
            var company = new CompanyModel
            {
                CompanyName = dto.CompanyName,
                CompanyLogo = dto.CompanyLogo ?? "",
                InchargeName = dto.InchargeName,
                InchargePhone = dto.InchargePhone,
                Email = dto.Email,
                PasswordHash = passwordHash,
                City = dto.City ?? "",
                Zone = dto.Zone ?? "",
                ServiceType = dto.ServiceType ?? "",
                Status = "Pending",  // Requires admin approval
                IsDefaultPassword = false,  // User set their own password
                CreatedAt = DateTime.Now
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create admin notification
            var adminNotif = new AdminNotification
            {
                Title = "New Company Registered",
                Message = $"Company {company.CompanyName} has registered and requires approval.",
                Type = "company_registered",
                CompanyId = company.CompanyID,
                Date = DateTime.Now,
                IsRead = false
            };
            _context.AdminNotifications.Add(adminNotif);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Account Created Successfully Admin Approval required to signin",
                data = new
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    Email = company.Email,
                    Status = company.Status
                },
                note = "Your account is created and will be approved by the admin before you can sign in."
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // 🔐 LOGIN - Company Login
        // POST: /api/company/auth/login
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Company login using Email or Phone + Password
        /// Returns JWT token for authenticated requests
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] CompanyLoginDto dto, [FromQuery] string? client)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Identifier) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Email/Phone and password are required."
                });
            }

            // Find Company
            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Email == dto.Identifier ||
                    c.InchargePhone == dto.Identifier);

            if (company == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials. Please check your email/phone and password."
                });
            }

            // Verify Password
            if (string.IsNullOrWhiteSpace(company.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Password not set. Please contact admin or reset your password."
                });
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, company.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid credentials. Please check your email/phone and password."
                });
            }

            // Only block Suspended accounts
            if (company.Status == "Suspended")
            {
                return Unauthorized(new
                {
                    message = "Your account has been suspended. Please contact admin support.",
                    status = "Suspended"
                });
            }

            if (string.Equals(client, "companycc", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(company.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    message = "Your account is not approved yet. Please wait for admin approval.",
                    status = company.Status
                });
            }

            // Generate JWT Token immediately
            var token = _jwtHelper.GenerateCompanyToken(
                company.CompanyID,
                company.CompanyName,
                company.Email ?? ""
            );

            // Get Statistics
            var totalTechnicians = await _context.Technicians
                .CountAsync(t => t.CompanyID == company.CompanyID);

            var activeTechnicians = await _context.Technicians
                .CountAsync(t =>
                    t.CompanyID == company.CompanyID &&
                    t.ApprovalStatus == "Active");

            var onlineTechnicians = await _context.Technicians
                .CountAsync(t =>
                    t.CompanyID == company.CompanyID &&
                    t.LiveStatus == "Online");

            var totalJobs = await _context.Jobs
                .CountAsync(j => j.CompanyID == company.CompanyID);

            var completedJobs = await _context.Jobs
                .CountAsync(j =>
                    j.CompanyID == company.CompanyID &&
                    j.Status == "Completed");

            return Ok(new
            {
                success = true,
                message = "Login successful!",
                Token = token,

                company = new
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    InchargeName = company.InchargeName,
                    Email = company.Email,
                    Phone = company.InchargePhone,
                    CompanyLogo = company.CompanyLogo,
                    City = company.City,
                    Zone = company.Zone,
                    ServiceType = company.ServiceType,
                    Status = company.Status,
                    IsDefaultPassword = company.IsDefaultPassword
                },

                statistics = new
                {
                    TotalTechnicians = totalTechnicians,
                    ActiveTechnicians = activeTechnicians,
                    OnlineTechnicians = onlineTechnicians,
                    TotalJobs = totalJobs,
                    CompletedJobs = completedJobs,
                    SuccessRate = totalJobs > 0
                        ? Math.Round((double)completedJobs / totalJobs * 100, 1)
                        : 0
                },

                note = company.IsDefaultPassword
                    ? "Please change your default password for security."
                    : null
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // 🔒 CHANGE PASSWORD
        // POST: /api/company/auth/change-password
        // Requires JWT Token (Authorization header)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Change password for logged-in company
        /// Requires Authorization: Bearer {token}
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // ── Get Company ID from JWT Token ─────────────────────────────────
            var companyIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(companyIdClaim))
                return Unauthorized(new { message = "Invalid token. Please login again." });

            var companyId = int.Parse(companyIdClaim);
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
                return NotFound(new { message = "Company not found." });

            // ── Verify Current Password ───────────────────────────────────────
            if (string.IsNullOrWhiteSpace(company.PasswordHash))
                return BadRequest(new { message = "Password not set. Cannot change password." });

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, company.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect." });

            // ── Validate New Password ─────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "New password is required." });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "New password and confirm password do not match." });

            // ── Update Password ───────────────────────────────────────────────
            company.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            company.IsDefaultPassword = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Password changed successfully!",
                CompanyID = company.CompanyID,
                CompanyName = company.CompanyName
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // 📧 FORGOT PASSWORD - Step 1: Request OTP
        // POST: /api/company/auth/forgot-password
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Request password reset OTP
        /// OTP sent via SMS to registered phone
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Identifier))
                return BadRequest(new { message = "Email or phone number is required." });

            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Email == dto.Identifier ||
                    c.InchargePhone == dto.Identifier);

            // ── Always return same message (prevent user enumeration) ─────────
            if (company == null)
                return Ok(new
                {
                    success = true,
                    message = "If an account exists with this email/phone, an OTP has been sent.",
                    note = "Please check your SMS for the OTP."
                });

            // ── Generate 6-digit OTP ──────────────────────────────────────────
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.Now.AddMinutes(15);

            // ── Save OTP to Database ──────────────────────────────────────────
            company.PasswordResetToken = otp;
            company.PasswordResetExpiry = expiry;
            await _context.SaveChangesAsync();

            // ── Send OTP via email first, fallback to SMS if email isn't available ───
            var email = company.Email ?? "";
            var phone = company.InchargePhone ?? "";
            var smsText = $"Your SmartProMan password reset OTP is: {otp}\nValid for 15 minutes. Do not share with anyone.";

            bool emailSent = false;
            bool smsSent = false;

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    emailSent = await _emailService.SendAsync(
                        email,
                        "SmartProMan Password Reset OTP",
                        $"<p>Your SmartProMan password reset OTP is: <strong>{otp}</strong></p><p>Valid for 15 minutes. Do not share this code.</p>"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email Send Failed: {ex.Message}");
                }
            }

            if (!emailSent && !string.IsNullOrWhiteSpace(phone))
            {
                try
                {
                    smsSent = await _smsService.SendAsync(phone, smsText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SMS Send Failed: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                message = emailSent
                    ? "OTP sent successfully to your registered email address."
                    : "OTP sent successfully to your registered phone number.",
                // ONLY FOR DEVELOPMENT - Remove in production
                DebugOTP = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? otp
                    : null,
                EmailSent = emailSent,
                SmsSent = smsSent,
                ExpiresIn = "15 minutes",
                note = emailSent
                    ? "Please check your email and enter the OTP."
                    : "Please check your SMS and enter the OTP."
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // ✅ VERIFY OTP - Step 2: Verify OTP
        // POST: /api/company/auth/verify-otp
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Verify OTP before password reset
        /// </summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Identifier) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(new { message = "Email/Phone and OTP are required." });

            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Email == dto.Identifier ||
                    c.InchargePhone == dto.Identifier);

            if (company == null)
                return BadRequest(new { message = "Invalid request." });

            // ── Check OTP Expired ─────────────────────────────────────────────
            if (company.PasswordResetExpiry == null || company.PasswordResetExpiry < DateTime.Now)
                return BadRequest(new
                {
                    success = false,
                    message = "OTP has expired. Please request a new one.",
                    expired = true
                });

            // ── Check OTP Match ───────────────────────────────────────────────
            if (company.PasswordResetToken != dto.Otp)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid OTP. Please check and try again.",
                    expired = false
                });

            return Ok(new
            {
                success = true,
                message = "OTP verified successfully! You can now reset your password.",
                Identifier = dto.Identifier,
                Otp = dto.Otp
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // 🔓 RESET PASSWORD - Step 3: Reset Password
        // POST: /api/company/auth/reset-password
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Reset password using verified OTP
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            // ── Validation ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "New password is required." });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long." });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Email == dto.Identifier ||
                    c.InchargePhone == dto.Identifier);

            if (company == null)
                return BadRequest(new { message = "Invalid request." });

            // ── Re-verify OTP (security: prevent skipping verify-otp step) ────
            if (company.PasswordResetToken != dto.Otp)
                return BadRequest(new { message = "Invalid OTP." });

            if (company.PasswordResetExpiry == null || company.PasswordResetExpiry < DateTime.Now)
                return BadRequest(new { message = "OTP has expired. Please start the process again." });

            // ── Set New Password ──────────────────────────────────────────────
            company.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            company.PasswordResetToken = null;  // Clear OTP
            company.PasswordResetExpiry = null;  // Clear expiry
            company.IsDefaultPassword = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Password reset successfully! You can now login with your new password.",
                CompanyName = company.CompanyName,
                Email = company.Email
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // 👤 GET PROFILE
        // GET: /api/company/auth/profile
        // Requires JWT Token (Authorization header)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Get current logged-in company profile
        /// Requires Authorization: Bearer {token}
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // ── Get Company ID from JWT Token ─────────────────────────────────
            var companyIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(companyIdClaim))
                return Unauthorized(new { message = "Invalid token. Please login again." });

            var companyId = int.Parse(companyIdClaim);
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
                return NotFound(new { message = "Company not found." });

            // ── Get Statistics ────────────────────────────────────────────────
            var totalTechnicians = await _context.Technicians.CountAsync(t => t.CompanyID == companyId);
            var activeTechnicians = await _context.Technicians.CountAsync(t =>
                t.CompanyID == companyId && t.ApprovalStatus == "Active");
            var onlineTechnicians = await _context.Technicians.CountAsync(t =>
                t.CompanyID == companyId && t.LiveStatus == "Online");

            var totalJobs = await _context.Jobs.CountAsync(j => j.CompanyID == companyId);
            var completedJobs = await _context.Jobs.CountAsync(j =>
                j.CompanyID == companyId && j.Status == "Completed");
            var pendingJobs = await _context.Jobs.CountAsync(j =>
                j.CompanyID == companyId && j.Status == "Pending");

            return Ok(new
            {
                success = true,
                company = new
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    CompanyLogo = company.CompanyLogo,
                    InchargeName = company.InchargeName,
                    InchargePhone = company.InchargePhone,
                    Email = company.Email,
                    City = company.City,
                    Zone = company.Zone,
                    ServiceType = company.ServiceType,
                    Status = company.Status,
                    CreatedAt = company.CreatedAt,
                    IsDefaultPassword = company.IsDefaultPassword
                },
                statistics = new
                {
                    // Technicians
                    TotalTechnicians = totalTechnicians,
                    ActiveTechnicians = activeTechnicians,
                    OnlineTechnicians = onlineTechnicians,
                    OfflineTechnicians = totalTechnicians - onlineTechnicians,

                    // Jobs
                    TotalJobs = totalJobs,
                    CompletedJobs = completedJobs,
                    PendingJobs = pendingJobs,
                    SuccessRate = totalJobs > 0
                        ? Math.Round((double)completedJobs / totalJobs * 100, 1)
                        : 0
                }
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // ✏️ UPDATE PROFILE
        // PUT: /api/company/auth/profile
        // Requires JWT Token (Authorization header)
        // ═════════════════════════════════════════════════════════════════════

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyProfileDto dto)
        {
            var companyIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(companyIdClaim))
                return Unauthorized(new { message = "Invalid token. Please login again." });

            var companyId = int.Parse(companyIdClaim);
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
                return NotFound(new { message = "Company not found." });

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                return BadRequest(new { message = "Company name is required." });

            if (string.IsNullOrWhiteSpace(dto.InchargeName))
                return BadRequest(new { message = "Incharge name is required." });

            if (string.IsNullOrWhiteSpace(dto.InchargePhone))
                return BadRequest(new { message = "Incharge phone is required." });

            if (!Regex.IsMatch(dto.InchargePhone.Replace(" ", string.Empty), "^\\d{10,11}$"))
                return BadRequest(new { message = "Enter a valid phone number (10-11 digits)." });

            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required." });

            if (!Regex.IsMatch(dto.Email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$"))
                return BadRequest(new { message = "Enter a valid email address." });

            if (string.IsNullOrWhiteSpace(dto.City))
                return BadRequest(new { message = "Please select a city." });

            if (string.IsNullOrWhiteSpace(dto.Zone))
                return BadRequest(new { message = "Zone is required." });


            var emailExists = await _context.Companies
                .AnyAsync(c => c.Email == dto.Email && c.CompanyID != companyId);
            if (emailExists)
                return BadRequest(new { message = "Email already registered with another company." });

            var phoneExists = await _context.Companies
                .AnyAsync(c => c.InchargePhone == dto.InchargePhone && c.CompanyID != companyId);
            if (phoneExists)
                return BadRequest(new { message = "Phone number already registered with another company." });

            company.CompanyName = dto.CompanyName.Trim();
            company.InchargeName = dto.InchargeName.Trim();
            company.InchargePhone = dto.InchargePhone.Trim();
            company.Email = dto.Email.Trim();
            company.City = dto.City.Trim();
            company.Zone = dto.Zone.Trim();
            
            if (!string.IsNullOrWhiteSpace(dto.ServiceType))
                company.ServiceType = dto.ServiceType.Trim();
                
            company.CompanyLogo = dto.CompanyLogo ?? company.CompanyLogo ?? "";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                company = new
                {
                    CompanyID = company.CompanyID,
                    CompanyName = company.CompanyName,
                    CompanyLogo = company.CompanyLogo,
                    InchargeName = company.InchargeName,
                    InchargePhone = company.InchargePhone,
                    Email = company.Email,
                    City = company.City,
                    Zone = company.Zone,
                    ServiceType = company.ServiceType
                }
            });
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // DTOs (Data Transfer Objects)
    // ═════════════════════════════════════════════════════════════════════

    public class CompanyRegisterDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string InchargeName { get; set; } = string.Empty;
        public string InchargePhone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Zone { get; set; }
        public string? ServiceType { get; set; }
    }

    public class CompanyLoginDto
    {
        public string Identifier { get; set; } = string.Empty;  // Email or Phone
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Identifier { get; set; } = string.Empty;  // Email or Phone
    }

    public class VerifyOtpDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateCompanyProfileDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string InchargeName { get; set; } = string.Empty;
        public string InchargePhone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
    }
}
