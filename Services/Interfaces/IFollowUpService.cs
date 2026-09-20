using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<FollowUpFilterViewModel> GetFollowUpsAsync(FollowUpFilterViewModel filter);

        Task<FollowUpFilterViewModel>GetFollowUpsForSalesManagerAsync(
                FollowUpFilterViewModel filter,
                long salesManagerId);

        Task<SalesManagerFollowUpDetailsViewModel?>GetFollowUpDetailsAsync(long leadId);

        Task<SalesManagerFollowUpDetailsViewModel?>GetFollowUpDetailsForSalesManagerAsync(
                long leadId,
                long salesManagerId);
    }
}