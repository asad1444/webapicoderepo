using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class JobStuckHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobID { get; set; }

        [Required]
        public string StuckReason { get; set; }

        public string StuckRemarks { get; set; }

        public string GpsLocation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("JobID")]
        public Job Job { get; set; }
    }
}
