using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class UnitRegistrationMedia
    {
        [Key]
        public int MediaID { get; set; }

        [Required]
        public int UnitID { get; set; }

        [Required]
        [MaxLength(20)]
        public string MediaType { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public string FilePath { get; set; }  // stores base64 or file path

        public DateTime? UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("UnitID")]
        public UnitRegistration UnitRegistration { get; set; }
    }
}
