using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class DailyWorkReport
    {
        [Key]
        public int DWRID { get; set; }

        public int CompanyID { get; set; }

        [Required]
        public int TechnicianID { get; set; }

        public DateTime? DutyIn { get; set; }

        public DateTime? DutyOut { get; set; }

        public int? PendingJobs { get; set; } = 0;

        public int? RepeatJobs { get; set; } = 0;

        public int? CompletedJobs { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? WalletBalance { get; set; } = 0;

        public string? ToolBox { get; set; }

        public string? SpareParts { get; set; }

        public string? FinalNote { get; set; }

        public string? Record { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Submitted";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReportDate { get; set; } = DateTime.Now.Date;

        [ForeignKey("CompanyID")]
        public Company? Company { get; set; }

        [ForeignKey("TechnicianID")]
        public Technician? Technician { get; set; }
    }
}
