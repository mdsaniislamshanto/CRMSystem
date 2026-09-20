namespace CRMSystem.Models.ViewModels
{
    public class GeminiAiAnalysisResultViewModel
    {
        public long FeedbackId { get; set; }

        public bool IsCustomerInterested { get; set; }

        public string BuyingIntent { get; set; } = string.Empty;

        public string Sentiment { get; set; } = string.Empty;

        public bool SiteVisitInterested { get; set; }

        public string AiSummary { get; set; } = string.Empty;

        public DateTime? AnalyzedAt { get; set; }

        public bool HasImage { get; set; }

        public string? ProofImageUrl { get; set; }

        public bool IsSuccess { get; set; } = true;

        public string? ErrorMessage { get; set; }
    }
}
