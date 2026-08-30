using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISalesOfficerServiceForSalesManager
    {
        Task<List<SalesOfficerListViewModel>>GetSalesOfficersAsync();

        Task<SalesOfficerDetailsViewModel?>GetSalesOfficerDetailsAsync(long userId);
    }
}