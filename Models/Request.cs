using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Request
    {
        [Key]
        public int RequestID { get; set; }

        public string ClientRequestNumber { get; set; }

        public DateTime Time { get; set; }

        public DateTime Date { get; set; }

        [Required]
        public string ClientName { get; set; }

        [Required]
        public string ClientContact { get; set; }

        [Required]
        public string ClientRequest { get; set; }

        [Required]
        public string KindOf { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Zone { get; set; }

        // ── Bid / Dispatch fields ─────────────────────────────────────────
        /// <summary>
        /// Open → bidding in progress
        /// Awarded → company won bid, awaiting technician assignment
        /// InProgress → technician assigned, Job record created
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "Open";

        /// <summary>Client account status managed from the Clients page</summary>
        [MaxLength(20)]
        public string ClientStatus { get; set; } = "Active";

        /// <summary>Company that won the bid</summary>
        public int? CompanyID { get; set; }

        /// <summary>When first bid was submitted — starts 15-min countdown</summary>
        public DateTime? BidOpenedAt { get; set; }

        /// <summary>
        /// Zone broadcast level:
        /// 0 = original zone only
        /// 1 = city-wide  (after 15 min, no bids)
        /// 2 = all zones  (after another 15 min)
        /// </summary>
        public int BroadcastZoneLevel { get; set; } = 0;

        /// <summary>Set when technician is assigned and Job is created</summary>
        public int? JobID { get; set; }

        /// <summary>Special instructions for the technician</summary>
        public string? SpecialInstructions { get; set; }

        /// <summary>Service address where technician needs to go</summary>
        public string? ServiceAddress { get; set; }

        /// <summary>Priority: Normal, High, Urgent, VIP</summary>
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal";

        [ForeignKey("CompanyID")]
        public Company? Company { get; set; }

        [ForeignKey("JobID")]
        public Job? Job { get; set; }
    }
}
