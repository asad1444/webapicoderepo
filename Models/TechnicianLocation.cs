using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class TechnicianLocation
    {
        [Key]
        public int LocationID { get; set; }

        [Required]
        public int TechnicianID { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        public DateTime? RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("TechnicianID")]
        public Technician Technician { get; set; }
    }
}
