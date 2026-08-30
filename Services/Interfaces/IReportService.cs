using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IReportService
    {
        // =====================================================
        // Admin Report
        // =====================================================

        Task<AdminReportViewModel>
            GetAdminReportAsync();


        // =====================================================
        // Sales Manager Report
        // =====================================================

        Task<SalesManagerReportViewModel>
            GetSalesManagerReportAsync();
    }
}