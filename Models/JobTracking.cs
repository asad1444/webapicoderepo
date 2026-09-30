using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class JobTracking
    {
        [Key]
        public int TrackingID { get; set; }

        [Required]
        public int JobID { get; set; }

        public DateTime? OnWayTime { get; set; }

        public DateTime? UnitRegistrationTime { get; set; }

        public DateTime? FIRTime { get; set; }

        public DateTime? QuoteTime { get; set; }

        public DateTime? FCRTime { get; set; }

        public DateTime? JobStatusTime { get; set; }

        public DateTime? JobClosureTime { get; set; }

        [MaxLength(100)]
        public string CurrentStep { get; set; }

        [MaxLength(50)]
        public string ETA { get; set; }

        public DateTime? LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("JobID")]
        public Job Job { get; set; }
    }
}
