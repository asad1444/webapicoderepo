using System;
using System.Collections.Generic;

namespace SmartProManWebAPI.DTOs.Technician
{
    public class DashboardSummaryResponse
    {
        public string TechnicianName { get; set; }
        public string CompanyName { get; set; }
        public string DutyStatus { get; set; }
        public int TodaysJobs { get; set; }
        public int PendingJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int RepeatJobs { get; set; }
        public int StuckJobs { get; set; }
        public decimal TodaysCollection { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public int? RelatedRecordId { get; set; }
        public string RelatedModule { get; set; }
        public bool IsRead { get; set; }
    }
}
