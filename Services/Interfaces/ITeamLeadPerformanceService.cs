using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ITeamLeadPerformanceService
    {
        // =========================================================
        // Get Performance Of Current Team
        // =========================================================

        Task<List<SalesOfficerPerformanceViewModel>>
            GetTeamPerformanceAsync(
                long teamLeadId,
                PerformanceFilterViewModel? filter = null);
    }
}