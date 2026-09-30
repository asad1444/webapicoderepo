using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Client
    {
        [Key]
        public int ClientID { get; set; }

        [MaxLength(50)]
        public string ClientCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string ClientName { get; set; }

        [MaxLength(500)]
        public string Logo { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(250)]
        public string Home { get; set; }

        [MaxLength(250)]
        public string ServeLocation { get; set; }

        [MaxLength(100)]
        public string Area { get; set; }

        [MaxLength(150)]
        public string Street { get; set; }

        [MaxLength(50)]
        public string Floor { get; set; }

        [MaxLength(100)]
        public string FloorZone { get; set; }

        [MaxLength(100)]
        public string TopZone { get; set; }

        [MaxLength(100)]
        public string Zone { get; set; }

        /// <summary>GPS Latitude for map display — set when client is onboarded</summary>
        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        /// <summary>GPS Longitude for map display — set when client is onboarded</summary>
        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
