using SmartProManWebAPI.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountReceived { get; set; }

        public string TransactionReference { get; set; }

        public bool IsSuccessful { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [ForeignKey("InvoiceId")]
        public Invoice Invoice { get; set; }
    }
}
