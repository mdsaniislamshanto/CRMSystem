using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ILeadService
    {
        Task<List<LeadViewModel>> GetAllLeadsAsync();

        Task CreateLeadAsync(CreateLeadViewModel model);

        //For Auto Lead Capture
        Task<long> CreateLeadFromCaptureAsync(AutoLeadCreateViewModel model);


        Task<LeadViewModel?> GetLeadByIdAsync(long id);

        Task<EditLeadViewModel?> GetLeadForEditAsync(long id);

        Task UpdateLeadAsync(EditLeadViewModel model);
        Task ArchiveLeadAsync(long id);

        Task<List<LeadViewModel>> GetArchivedLeadsAsync();
        Task RestoreLeadAsync(long id);
        Task<AssignLeadViewModel?> GetAssignLeadViewModelAsync(long leadId);
        Task AssignLeadAsync(AssignLeadViewModel model, long adminId);

        Task<List<MyAssignedLeadViewModel>> GetAssignedLeadsAsync(long salesOfficerId);

        Task AcceptLeadAsync(long assignmentId, long salesOfficerId);


     
    }
}