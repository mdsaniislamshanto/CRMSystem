using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerCurrentFeedbackViewModel
    {
        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string? Summary { get; set; }

        public FeedbackStatus? Status { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        public long? LatestFeedbackId { get; set; }
    }
}