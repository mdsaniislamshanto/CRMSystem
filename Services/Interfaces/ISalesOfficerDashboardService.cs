using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISalesOfficerDashboardService
    {
        Task<SalesOfficerDashboardViewModel> GetDashboardAsync(long salesOfficerId);
    }
}