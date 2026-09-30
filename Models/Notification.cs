using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TechnicianId { get; set; }

        /// <summary>Which company this notification belongs to (for company frontend)</summary>
        public int? CompanyId { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string? Type { get; set; } // "delay", "stuck", "info"

        public DateTime Date { get; set; } = DateTime.Now;

        public int? RelatedRecordId { get; set; }

        public string RelatedModule { get; set; }

        public bool IsRead { get; set; } = false;

        [ForeignKey("TechnicianId")]
        public Technician Technician { get; set; }
    }
}
