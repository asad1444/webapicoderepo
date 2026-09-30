using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice Invoice { get; set; }
    }
}
