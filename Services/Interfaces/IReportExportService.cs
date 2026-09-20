using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IReportExportService
    {
        byte[] GenerateTeamLeadPdf(
            TeamLeadReportViewModel report);

        byte[] GenerateTeamLeadExcel(
            TeamLeadReportViewModel report);
    }
}