using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // Password Reset Fields
        [MaxLength(6)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetExpiry { get; set; }
    }
}
