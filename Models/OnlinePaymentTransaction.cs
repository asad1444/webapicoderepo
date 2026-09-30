using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class OnlinePaymentTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int JobId { get; set; }

        [Required]
        [MaxLength(100)]
        public string OrderId { get; set; } // Our internal unique order ID (e.g. SPM-JOB-123-12345)

        [MaxLength(100)]
        public string GatewayTransactionId { get; set; } // PayFast reference ID

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string Currency { get; set; } = "PKR";

        [MaxLength(50)]
        public string PaymentMethod { get; set; } // Easypaisa, CreditCard, BankAccount

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Failed

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        
        [ForeignKey("JobId")]
        public Job Job { get; set; }
    }
}
