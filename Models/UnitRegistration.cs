using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class UnitRegistration
    {
        [Key]
        public int UnitID { get; set; }

        [Required]
        public int JobID { get; set; }

        [Required]
        public int TechnicianID { get; set; }

        public int? ClientID { get; set; }  // Optional — from Job/Request

        public DateTime? RegistrationDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(150)]
        public string ModelNumber { get; set; }

        [Required]
        [MaxLength(150)]
        public string SerialNumber { get; set; }

        public string? AdminRemarks { get; set; }

        /// <summary>Service name from request e.g. "AC Not Cooling"</summary>
        public string? ServiceName { get; set; }

        /// <summary>QR code data (JSON string) generated after registration</summary>
        public string? QrCodeData { get; set; }

        /// <summary>Zone</summary>
        public string? Zone { get; set; }

        /// <summary>Area</summary>
        public string? Area { get; set; }

        /// <summary>Street</summary>
        public string? Street { get; set; }

        /// <summary>Floor number</summary>
        public string? Floor { get; set; }

        /// <summary>Floor zone</summary>
        public string? FloorZone { get; set; }

        /// <summary>Top zone</summary>
        public string? TopZone { get; set; }

        [ForeignKey("JobID")]
        public Job Job { get; set; }

        [ForeignKey("TechnicianID")]
        public Technician Technician { get; set; }

        [ForeignKey("ClientID")]
        public Client Client { get; set; }
    }
}
