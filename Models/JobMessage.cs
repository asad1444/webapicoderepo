using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class JobMessage
    {
        [Key]
        public int MessageID { get; set; }

        [Required]
        public int JobID { get; set; }

        [MaxLength(30)]
        public string SenderType { get; set; }

        public int? SenderID { get; set; }

        public string Message { get; set; }

        public DateTime? SentAt { get; set; } = DateTime.Now;

        [ForeignKey("JobID")]
        public Job Job { get; set; }
    }
}
