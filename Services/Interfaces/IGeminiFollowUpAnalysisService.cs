using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IGeminiFollowUpAnalysisService
    {
        Task<GeminiAiAnalysisResultViewModel> AnalyzeFeedbackAsync(long feedbackId, bool forceReanalysis = false);

        Task<GeminiAiAnalysisResultViewModel?> GetFeedbackAnalysisAsync(long feedbackId);

        Task<GeminiLeadAnalysisResultViewModel> AnalyzeLeadAsync(long leadId, bool forceReanalysis = false);

        Task<GeminiLeadAnalysisResultViewModel?> GetLeadAnalysisAsync(long leadId);
    }
}
