using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class FeedbackDetailsViewModel
    {
        public long FeedbackId { get; set; }

        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string Summary { get; set; } = string.Empty;

        public FeedbackStatus Status { get; set; }

        public DateTime SubmittedAt { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        public string? ProofImage { get; set; }

        public string? VoiceRecording { get; set; }

        public string? Notes { get; set; }
    }
}