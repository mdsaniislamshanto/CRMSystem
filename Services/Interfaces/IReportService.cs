using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IReportService
    {
        Task<AdminReportViewModel> GetAdminReportAsync();
    }
}