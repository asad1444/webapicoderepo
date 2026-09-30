using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class DispatchBid
    {
        [Key]
        public int BidID { get; set; }

        /// <summary>Linked to Request — bid is placed on a Request now</summary>
        [Required]
        public int RequestID { get; set; }

        /// <summary>JobID is set later when technician is assigned</summary>
        public int? JobID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        public int EstimatedArrival { get; set; }

        public DateTime? BidTime { get; set; } = DateTime.Now;

        public bool? IsWinner { get; set; } = false;

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [ForeignKey("RequestID")]
        public Request Request { get; set; }

        [ForeignKey("JobID")]
        public Job? Job { get; set; }

        [ForeignKey("CompanyID")]
        public Company Company { get; set; }
    }
}
