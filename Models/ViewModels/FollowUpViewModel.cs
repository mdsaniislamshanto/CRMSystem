namespace CRMSystem.Models.ViewModels
{
    public class FollowUpViewModel
    {
        public long FeedbackId { get; set; }

        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string LeadName { get; set; } = string.Empty;

        public string SalesOfficerName { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string FeedbackStatus { get; set; } = string.Empty;

        // When Sales Officer submitted the feedback
        public DateTime SubmittedAt { get; set; }

        // When the next follow-up is scheduled
        public DateTime FollowUpDate { get; set; }

        public bool IsOverdue { get; set; }

        public bool IsDueToday { get; set; }

        public string FollowUpStatus { get; set; } = string.Empty;

 
        // Timeliness Tracking
        public string TimelinessStatus { get; set; } = string.Empty;
    }
}