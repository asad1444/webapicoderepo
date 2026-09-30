using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Technician
    {
        [Key]
        public int TechnicianID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [MaxLength(50)]
        public string? TechnicianCode { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; }

        public string? Photo { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        [Required]
        [MaxLength(100)]
        public string Designation { get; set; }

        /// <summary>BCrypt hashed password for mobile app login</summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        /// <summary>True if password has never been changed (first login flow)</summary>
        public bool IsDefaultPassword { get; set; } = true;

        /// <summary>OTP token for forgot-password flow</summary>
        [MaxLength(10)]
        public string? PasswordResetToken { get; set; }

        /// <summary>Expiry time for OTP token</summary>
        public DateTime? PasswordResetExpiry { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WalletBalance { get; set; } = 0;

        [MaxLength(30)]
        public string? LiveStatus { get; set; } = "Offline";

        [MaxLength(30)]
        public string? ApprovalStatus { get; set; } = "Pending";

        /// <summary>Cached count — updated when a job is completed</summary>
        public int? TotalJobsSolved { get; set; } = 0;

        public DateTime? LastDutyIn { get; set; }
        public DateTime? LastDutyOut { get; set; }

        // ── Profile fields ────────────────────────────────────────────────────

        [Required]
        [MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Cnic { get; set; }

        /// <summary>Date technician joined the company</summary>
        public DateTime? DateOfJoining { get; set; }

        /// <summary>Comma-separated service areas e.g. "Lahore, Pakistan"</summary>
        [MaxLength(300)]
        public string? ServiceAreas { get; set; }

        /// <summary>Skills e.g. "AC Repair, Installation, Maintenance"</summary>
        [MaxLength(500)]
        public string? Skills { get; set; }

        /// <summary>License / Certification e.g. "HVAC Certified Technician"</summary>
        [MaxLength(300)]
        public string? LicenseCertification { get; set; }

        /// <summary>Average rating out of 5 (calculated from job reviews)</summary>
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; } = 0;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CompanyID")]
        public Company? Company { get; set; }
    }
}
