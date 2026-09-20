namespace CRMSystem.Models.ViewModels
{
    public class GeminiLeadAnalysisResultViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string BuyingDecision { get; set; } = string.Empty;

        public string InterestLevel { get; set; } = string.Empty;

        public string LeadAuthenticity { get; set; } = string.Empty;

        public string SynthesisSummary { get; set; } = string.Empty;

        public int TotalFollowUpsAnalyzed { get; set; }

        public DateTime? AnalyzedAt { get; set; }

        public bool IsSuccess { get; set; } = true;

        public string? ErrorMessage { get; set; }
    }
}
