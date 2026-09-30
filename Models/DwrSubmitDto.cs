namespace SmartProManWebAPI.Models
{
    public class DwrSubmitDto
    {
        public int TechnicianID { get; set; }
        public DateTime? DutyIn { get; set; }
        public DateTime? DutyOut { get; set; }
        public int? PendingJobs { get; set; }
        public int? RepeatJobs { get; set; }
        public int? CompletedJobs { get; set; }
        public decimal? WalletBalance { get; set; }
        public string? FinalNote { get; set; }
        public string? Record { get; set; }
    }
}
