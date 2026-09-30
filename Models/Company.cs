using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? CompanyLogo { get; set; }

        [Required]
        [MaxLength(150)]
        public string InchargeName { get; set; }

        [MaxLength(20)]
        public string InchargePhone { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(100)]
        public string Zone { get; set; }

        [MaxLength(100)]
        public string ServiceType { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // ── Authentication Fields ────────────────────────────────────────────
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        public bool IsDefaultPassword { get; set; } = true;

        [MaxLength(10)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetExpiry { get; set; }
    }
}
