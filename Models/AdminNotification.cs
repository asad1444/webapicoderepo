using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class AdminNotification
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string? Type { get; set; } // "company_registered", "info"

        /// <summary>Which company this notification belongs to</summary>
        public int? CompanyId { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
