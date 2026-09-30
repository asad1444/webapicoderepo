using System;

namespace SmartProManWebAPI.DTOs.Technician
{
    public class LoginRequest
    {
        // Email, Mobile Number or Name
        public string Identifier { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public int TechnicianId { get; set; }
        public string Name { get; set; }
        public string? CompanyName { get; set; }
        public string Token { get; set; }
        public bool IsDefaultPassword { get; set; }
        public string? Role { get; set; }
        public string? Designation { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Identifier { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Identifier { get; set; }
        public string Otp { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Identifier { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
    
    public class ChangePasswordRequest
    {
        public int TechnicianId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
