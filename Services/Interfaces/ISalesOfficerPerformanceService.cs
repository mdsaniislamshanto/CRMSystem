using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISalesOfficerPerformanceService
    {
        // =====================================================
        // Sales Officer Performance
        // =====================================================

        Task<SalesOfficerPerformanceViewModel>
            GetPerformanceAsync(long salesOfficerId);
    }
}