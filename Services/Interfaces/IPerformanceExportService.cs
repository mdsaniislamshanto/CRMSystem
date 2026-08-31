using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IPerformanceExportService
    {
        // =====================================================
        // Export Sales Officer Performance Report to Excel
        // =====================================================

        Task<byte[]> ExportPerformanceToExcelAsync(
            PerformanceFilterViewModel? filter = null);


        // =====================================================
        // Export Sales Officer Performance Report to PDF
        // =====================================================

        Task<byte[]> ExportPerformanceToPdfAsync(
            PerformanceFilterViewModel? filter = null);
    }
}