using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class ProformaInvoiceItem
    {
        [Key]
        public int ItemID { get; set; }

        [Required]
        public int InvoiceID { get; set; }

        [MaxLength(300)]
        public string Description { get; set; }

        public int? Quantity { get; set; } = 1;

        [MaxLength(50)]
        public string Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPrice { get; set; }

        [ForeignKey("InvoiceID")]
        public ProformaInvoice ProformaInvoice { get; set; }
    }
}
