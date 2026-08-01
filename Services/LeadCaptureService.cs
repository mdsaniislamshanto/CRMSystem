using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;

namespace CRMSystem.Services
{
    public class LeadCaptureService : ILeadCaptureService
    {
        private readonly ApplicationDbContext _context;

        private readonly ILeadService _leadService;

        public LeadCaptureService(ApplicationDbContext context, ILeadService leadService)
        {
            _context = context;
            _leadService = leadService;
        }

        public async Task<long> CaptureLeadAsync(
                 AutoLeadCreateViewModel model,
                 LeadCaptureSource captureSource,
                 string? externalLeadId = null,
                 string? payloadJson = null)
        {


            long leadId = await _leadService.CreateLeadFromCaptureAsync(model);


            var captureLog = new LeadCaptureLog
            {
                LeadId = leadId,
                CaptureSource = captureSource,
                CaptureStatus = CaptureStatus.Success,
                ExternalLeadId = externalLeadId,
                PayloadJson = payloadJson,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            _context.LeadCaptureLogs.Add(captureLog);

            await _context.SaveChangesAsync();

            return captureLog.CaptureLogId;
        }
    }
}