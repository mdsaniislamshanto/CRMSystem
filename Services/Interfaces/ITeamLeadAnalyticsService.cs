using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ITeamLeadAnalyticsService
    {
        // =========================================================
        // Performance Trend
        // =========================================================
        Task<TeamLeadPerformanceTrendViewModel?>
            GetPerformanceTrendAsync(
                long teamLeadId,
                DateTime fromDate,
                DateTime toDate);

        // =========================================================
        // Team Lead Report
        // =========================================================
        Task<TeamLeadReportViewModel?>
            GetReportAsync(
                long teamLeadId,
                DateTime fromDate,
                DateTime toDate);
    }
}