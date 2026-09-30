using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Helpers;
using SmartProManWebAPI.Models;
using SmartProManWebAPI.Services;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, JwtHelper jwtHelper, IEmailService emailService)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _emailService = emailService;
        }

        /// <summary>
        /// Admin / Staff login — returns JWT token
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest("Email and password are required.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            // Verify BCrypt hashed password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            if (user.Status != "Active")
                return Unauthorized("User account is not active.");

            var token = _jwtHelper.GenerateUserToken(user.UserID, user.Email, user.Role);

            return Ok(new
            {
                user.UserID,
                user.FullName,
                user.Email,
                user.Role,
                Token = token
            });
        }

        /// <summary>
        /// Register a new admin/staff user
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Email and password are required.");

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists.");

            var normalizedRole = string.IsNullOrWhiteSpace(dto.Role) ? "Admin" : dto.Role.Trim();
            if (normalizedRole.Equals("staff", StringComparison.OrdinalIgnoreCase))
                normalizedRole = "Admin";

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = normalizedRole,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully.", UserID = user.UserID });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var email = dto.Identifier?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Ok(new { success = true, message = "If an account exists with this email, an OTP has been sent." });

            var otp = Random.Shared.Next(100000, 1000000).ToString();
            user.PasswordResetToken = otp;
            user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var emailSent = await _emailService.SendAsync(
                user.Email,
                "SmartProMan Password Reset OTP",
                $"<p>Your SmartProMan password reset OTP is: <strong>{otp}</strong></p><p>It is valid for 15 minutes. Do not share this code.</p>");

            if (!emailSent)
                return StatusCode(503, new { message = "OTP email could not be sent. Please check the email service configuration." });

            return Ok(new { success = true, message = "OTP sent successfully to your email address.", expiresIn = "15 minutes" });
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var email = dto.Identifier?.Trim();
            var otp = dto.Otp?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                return BadRequest(new { message = "Email and OTP are required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.PasswordResetToken != otp)
                return BadRequest(new { message = "Invalid OTP." });

            if (user.PasswordResetExpiry == null || user.PasswordResetExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "OTP has expired. Please request a new one." });

            return Ok(new { success = true, message = "OTP verified successfully." });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var email = dto.Identifier?.Trim();
            var otp = dto.Otp?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                return BadRequest(new { message = "Email and OTP are required." });
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long." });
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.PasswordResetToken != otp)
                return BadRequest(new { message = "Invalid OTP." });
            if (user.PasswordResetExpiry == null || user.PasswordResetExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "OTP has expired. Please request a new one." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Password reset successfully. You can now login." });
        }

        /// <summary>
        /// Get current logged-in admin/staff profile
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return Unauthorized(new { message = "Invalid token. Please login again." });

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new
            {
                success = true,
                user = new
                {
                    user.UserID,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.Status
                }
            });
        }

        /// <summary>
        /// Update current logged-in admin/staff profile
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Full name and email are required." });

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return Unauthorized(new { message = "Invalid token. Please login again." });

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var normalizedEmail = dto.Email.Trim();
            if (normalizedEmail != user.Email &&
                await _context.Users.AnyAsync(u => u.Email == normalizedEmail && u.UserID != userId))
            {
                return BadRequest(new { message = "This email is already in use." });
            }

            user.FullName = dto.FullName.Trim();
            user.Email = normalizedEmail;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                user = new
                {
                    user.UserID,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.Status
                }
            });
        }

        /// <summary>
        /// Change password for logged-in admin/staff
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("New password and confirm password do not match.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Identifier { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordDto : VerifyOtpDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
