using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProManWebAPI.Models
{
    public class Job
    {
        [Key]
        public int JobID { get; set; }

        /// <summary>Link to original Request — primary source of truth</summary>
        public int? RequestID { get; set; }

        /// <summary>ClientID optional — kept for backward compat only</summary>
        public int? ClientID { get; set; }

        public int? CompanyID { get; set; }

        public int? TechnicianID { get; set; }

        [MaxLength(20)]
        public string AssignmentSource { get; set; } = "Company";

        [MaxLength(50)]
        public string CRNumber { get; set; }

        [MaxLength(50)]
        public string CRID { get; set; }

        [MaxLength(200)]
        public string ClientRequest { get; set; }

        public string IssueDescription { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? BidOpenedAt { get; set; }

        public int BroadcastZoneLevel { get; set; } = 0;

        [ForeignKey("RequestID")]
        public Request? Request { get; set; }

        [ForeignKey("ClientID")]
        public Client? Client { get; set; }

        [ForeignKey("CompanyID")]
        public Company? Company { get; set; }

        [ForeignKey("TechnicianID")]
        public Technician? Technician { get; set; }
    }
}
