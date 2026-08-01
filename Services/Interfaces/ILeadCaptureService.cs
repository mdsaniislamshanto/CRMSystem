using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ILeadCaptureService
    {
        Task<long> CaptureLeadAsync(
    AutoLeadCreateViewModel model,
    LeadCaptureSource captureSource,
    string? externalLeadId = null,
    string? payloadJson = null);
    }
}