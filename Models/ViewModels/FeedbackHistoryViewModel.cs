using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class FeedbackHistoryViewModel
    {
        public long FeedbackId { get; set; }

        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public FeedbackStatus Status { get; set; }

        public DateTime SubmittedAt { get; set; }

        public DateTime? NextFollowUpDate { get; set; }
    }
}