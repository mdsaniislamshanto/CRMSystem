using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<FollowUpFilterViewModel> GetFollowUpsAsync(
            FollowUpFilterViewModel filter);

        Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(long leadId);
    }
}