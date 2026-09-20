using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerFollowUpViewModel
    {
        public long FeedbackId { get; set; }

        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public FeedbackStatus Status { get; set; }

        public DateTime NextFollowUpDate { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string Summary { get; set; } = string.Empty;

        // Gemini AI Analysis
        public bool AiAnalyzed { get; set; }
        public bool? AiCustomerInterested { get; set; }
        public string? AiBuyingIntent { get; set; }
        public string? AiSentiment { get; set; }
        public bool? AiSiteVisitInterested { get; set; }
        public string? AiSummary { get; set; }
        public string? ProofImage { get; set; }
    }
}