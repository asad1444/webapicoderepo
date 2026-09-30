using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class MasterCard
    {
        [Key]
        public int MasterCardID { get; set; }

        [Required]
        public int JobID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int TechnicianID { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "In Progress";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("JobID")]
        public Job Job { get; set; }

        [ForeignKey("ClientID")]
        public Client Client { get; set; }

        [ForeignKey("CompanyID")]
        public Company Company { get; set; }

        [ForeignKey("TechnicianID")]
        public Technician Technician { get; set; }
    }
}
