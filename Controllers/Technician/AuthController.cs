using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.DTOs.Technician;
using SmartProManWebAPI.Helpers;
using SmartProManWebAPI.Services;

namespace SmartProManWebAPI.Controllers.Technician
{
    [Route("api/technician/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtHelper    _jwtHelper;
        private readonly ISmsService  _smsService;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, JwtHelper jwtHelper, ISmsService smsService, IEmailService emailService)
        {
            _context     = context;
            _jwtHelper   = jwtHelper;
            _smsService  = smsService;
            _emailService = emailService;
        }

        /// <summary>
        /// Helper to lookup technician by Phone (with Pakistani/International formatting), Email, FullName, or TechnicianCode
        /// </summary>
        private async Task<Models.Technician?> FindTechnicianByIdentifierAsync(string? rawIdentifier)
        {
            if (string.IsNullOrWhiteSpace(rawIdentifier)) return null;

            var identifier = rawIdentifier.Trim();
            var cleanPhone = identifier.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            var phoneVariants = new List<string> { identifier, cleanPhone };
            if (cleanPhone.StartsWith("+92"))
            {
                phoneVariants.Add("0" + cleanPhone.Substring(3));
                phoneVariants.Add(cleanPhone.Substring(3));
                phoneVariants.Add(cleanPhone.Substring(1)); // 923...
            }
            else if (cleanPhone.StartsWith("92"))
            {
                phoneVariants.Add("0" + cleanPhone.Substring(2));
                phoneVariants.Add("+" + cleanPhone);
                phoneVariants.Add(cleanPhone.Substring(2));
            }
            else if (cleanPhone.StartsWith("03"))
            {
                phoneVariants.Add("+92" + cleanPhone.Substring(1));
                phoneVariants.Add("92" + cleanPhone.Substring(1));
                phoneVariants.Add(cleanPhone.Substring(1));
            }

            return await _context.Technicians
                .Include(t => t.Company)
                .FirstOrDefaultAsync(t =>
                    t.FullName == identifier ||
                    t.Email == identifier ||
                    (t.TechnicianCode != null && t.TechnicianCode == identifier) ||
                    phoneVariants.Contains(t.Phone)
                );
        }

        /// <summary>
        /// Technician login using Phone, TechnicianCode, Email or FullName + password
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Identifier and Password are required.");
            }

            var technician = await FindTechnicianByIdentifierAsync(request.Identifier);

            if (technician == null)
                return Unauthorized("Invalid credentials.");

            if (string.IsNullOrWhiteSpace(technician.PasswordHash))
                return Unauthorized("This account is not configured for login. Please contact your company admin.");

            try
            {
                var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, technician.PasswordHash);
                if (!passwordValid)
                    return Unauthorized("Invalid credentials.");
            }
            catch
            {
                return Unauthorized("This account is not configured for login. Please contact your company admin.");
            }

            if (technician.ApprovalStatus == "Suspended")
                return Unauthorized("Your account has been suspended. Contact your company admin.");

            if (technician.ApprovalStatus == "Pending")
                return Unauthorized("Your account is pending approval. Please wait for admin approval.");

            var token = _jwtHelper.GenerateTechnicianToken(
                technician.TechnicianID,
                technician.FullName,
                technician.CompanyID
            );

            return Ok(new LoginResponse
            {
                TechnicianId = technician.TechnicianID,
                Name = technician.FullName,
                CompanyName = technician.Company?.CompanyName,
                Token = token,
                IsDefaultPassword = technician.IsDefaultPassword,
                Role = technician.Designation,
                Designation = technician.Designation
            });
        }

        /// <summary>
        /// Change password (first login or regular change)
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var technicianIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(technicianIdClaim) || !int.TryParse(technicianIdClaim, out var technicianId))
            {
                return Unauthorized("Invalid token.");
            }

            var technician = await _context.Technicians.FindAsync(technicianId);
            if (technician == null) return NotFound("Technician not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, technician.PasswordHash))
                return BadRequest("Current password is incorrect.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters long.");

            technician.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            technician.IsDefaultPassword = false;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully." });
        }

        /// <summary>
        /// Step 1 of forgot password — sends OTP via SMS
        /// </summary>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
                return BadRequest(new { success = false, message = "Phone number or email is required." });

            var technician = await FindTechnicianByIdentifierAsync(request.Identifier);

            // Always return friendly response if not found to prevent user enumeration
            if (technician == null)
            {
                return Ok(new
                {
                    success = true,
                    message = "If an account exists, an OTP has been sent to your registered phone number.",
                    note = "Please check your SMS for the OTP."
                });
            }

            // Generate 6-digit OTP
            var otp    = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(15);

            // Save OTP to DB
            technician.PasswordResetToken  = otp;
            technician.PasswordResetExpiry = expiry;
            await _context.SaveChangesAsync();

            // Send OTP via email when available, otherwise fallback to SMS
            var email = technician.Email ?? "";
            var phone = technician.Phone ?? "";
            var smsText = $"Your SmartProMan OTP is: {otp}\nValid for 15 minutes. Do not share.";

            bool emailSent = false;
            bool smsSent = false;

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    emailSent = await _emailService.SendAsync(
                        email,
                        "SmartProMan Password Reset OTP",
                        $"<p>Your SmartProMan OTP is: <strong>{otp}</strong></p><p>Valid for 15 minutes. Do not share this code.</p>"
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
                success   = true,
                message   = !string.IsNullOrWhiteSpace(email)
                    ? "OTP sent successfully to your registered email address."
                    : "OTP sent successfully to your registered phone number.",
                debugOTP  = otp,
                emailSent = emailSent,
                smsSent   = smsSent,
                expiresIn = "15 minutes",
                email     = email,
                phone     = phone.Length > 4 ? string.Concat(new string('*', phone.Length - 4), phone.AsSpan(phone.Length - 4)) : phone,
                identifier = request.Identifier.Trim()
            });
        }

        /// <summary>
        /// Step 2 — verify the OTP entered by the technician
        /// </summary>
        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new { success = false, message = "Identifier and OTP are required." });

            var technician = await FindTechnicianByIdentifierAsync(request.Identifier);

            if (technician == null)
                return BadRequest(new { success = false, message = "Invalid request or technician not found." });

            // Check OTP expired
            if (technician.PasswordResetExpiry == null || technician.PasswordResetExpiry < DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "OTP has expired. Please request a new one.",
                    expired = true
                });
            }

            // Check OTP match
            var inputOtp = request.Otp.Trim();
            if (technician.PasswordResetToken != inputOtp)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Incorrect OTP. Please try again.",
                    expired = false
                });
            }

            return Ok(new
            {
                success = true,
                message = "OTP verified successfully. You can now set a new password.",
                identifier = request.Identifier.Trim(),
                otp = inputOtp
            });
        }

        /// <summary>
        /// Step 3 — reset password after OTP verified
        /// </summary>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { success = false, message = "New password is required." });

            if (request.NewPassword.Length < 6)
                return BadRequest(new { success = false, message = "Password must be at least 6 characters long." });

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { success = false, message = "Passwords do not match." });

            var technician = await FindTechnicianByIdentifierAsync(request.Identifier);

            if (technician == null)
                return BadRequest(new { success = false, message = "Invalid request or technician not found." });

            // Re-verify OTP (security: prevent skipping verify-otp step)
            if (technician.PasswordResetExpiry == null || technician.PasswordResetExpiry < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "OTP has expired. Please start the process again." });

            var inputOtp = request.Otp?.Trim() ?? "";
            if (technician.PasswordResetToken != inputOtp)
                return BadRequest(new { success = false, message = "Invalid or expired OTP." });

            // Set new password
            technician.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            technician.PasswordResetToken  = null;  // clear OTP
            technician.PasswordResetExpiry = null;  // clear expiry
            technician.IsDefaultPassword   = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Password reset successfully. Please login with your new password.",
                phone   = technician.Phone
            });
        }
    }
}
