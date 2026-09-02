using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<SalesManagerFollowUpFilterViewModel> GetFollowUpsAsync(
            SalesManagerFollowUpFilterViewModel filter);

        Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(long leadId);
    }
}