namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerFeedbackHistoryViewModel
    {
        public long FeedbackId { get; set; }

        public long AssignmentId { get; set; }

        public DateTime SubmittedAt { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string FeedbackStatus { get; set; } = string.Empty;

        public DateTime? NextFollowUpDate { get; set; }

        public string? ProofImage { get; set; }

        public string? VoiceRecording { get; set; }

        public string? Notes { get; set; }

        public bool IsNextFollowUpOverdue { get; set; }

        public string LastFeedbackTiming { get; set; } = string.Empty;
    }
}