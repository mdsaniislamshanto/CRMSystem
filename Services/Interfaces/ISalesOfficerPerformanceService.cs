using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISalesOfficerPerformanceService
    {
        // =====================================================
        // Get Performance Of All Active Sales Officers
        // =====================================================

        Task<List<SalesOfficerPerformanceViewModel>>
            GetPerformanceAsync(
                PerformanceFilterViewModel? filter = null);


        // =====================================================
        // Get Performance Of A Single Sales Officer
        // =====================================================
        Task<SalesOfficerPerformanceViewModel?>
            GetPerformanceAsync(
                long salesOfficerId,
                PerformanceFilterViewModel? filter = null);


        // =====================================================
        // Get Performance Trend
        // =====================================================

        Task<List<PerformanceTrendViewModel>>
            GetPerformanceTrendAsync(
                PerformanceFilterViewModel? filter = null);

        // =====================================================
        // Get Top Performer
        // =====================================================

        Task<PerformanceHighlightViewModel?>
            GetTopPerformerAsync(
                PerformanceFilterViewModel? filter = null);


        // =====================================================
        // Get Needs Attention Officer
        // =====================================================

        Task<PerformanceHighlightViewModel?>
            GetNeedsAttentionAsync(
                PerformanceFilterViewModel? filter = null);

        // =====================================================
        // Get SLA Trend
        // =====================================================

        Task<List<SLATrendViewModel>>
            GetSLATrendAsync(
                PerformanceFilterViewModel? filter = null);


        // =====================================================
        // Get Completion Trend
        // =====================================================

        Task<List<CompletionTrendViewModel>>
            GetCompletionTrendAsync(
                PerformanceFilterViewModel? filter = null);
    }
}
