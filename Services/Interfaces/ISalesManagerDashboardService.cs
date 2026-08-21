using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISalesManagerDashboardService
    {
        Task<SalesManagerDashboardViewModel> GetDashboardAsync();

        Task<List<SalesManagerFollowUpViewModel>> GetFollowUpsAsync();

        Task<SalesManagerFollowUpDetailsViewModel?> GetFollowUpDetailsAsync(long leadId);
    }
}