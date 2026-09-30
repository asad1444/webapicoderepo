using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class ProformaInvoice
    {
        [Key]
        public int InvoiceID { get; set; }

        [Required]
        public int MasterCardID { get; set; }

        [MaxLength(50)]
        public string FIRNumber { get; set; }

        [MaxLength(50)]
        public string QuoteNumber { get; set; }

        public string IncidentDetails { get; set; }

        /// <summary>Invoice text box — invoice/complaint notes from technician</summary>
        public string InvoiceDetails { get; set; }

        /// <summary>Chat box — communication notes between tech and client/admin</summary>
        public string ChatBox { get; set; }

        /// <summary>Technician uploaded photo path for this job</summary>
        [MaxLength(500)]
        public string TechnicianPhoto { get; set; }

        /// <summary>User-selected date on the form (can differ from CreatedAt)</summary>
        public DateTime? InvoiceDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitAHighPressure { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitALowPressure { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitAGT { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitART { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitAPower { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitAAmpere { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBHighPressure { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBLowPressure { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBGT { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBRT { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBPower { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CircuitBAmpere { get; set; }

        [MaxLength(50)]
        public string VoucherNumber { get; set; }

        [MaxLength(100)]
        public string VoucherType { get; set; }

        public DateTime? VoucherExpiry { get; set; }

        public string FinalNote { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Draft";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SubTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrandTotal { get; set; } = 0;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MasterCardID")]
        public MasterCard MasterCard { get; set; }
    }
}
