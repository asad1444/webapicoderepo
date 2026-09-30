using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int JobID { get; set; }

        public string InvoiceNumber { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [ForeignKey("JobID")]
        public Job Job { get; set; }
    }
}
