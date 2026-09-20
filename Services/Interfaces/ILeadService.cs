using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ILeadService
    {
        Task<List<LeadViewModel>> GetAllLeadsAsync();

        Task CreateLeadAsync(CreateLeadViewModel model);

        //For Auto Lead Capture
        Task<long> CreateLeadFromCaptureAsync(AutoLeadCreateViewModel model);


        // For API Leads
        Task<List<ApiLeadViewModel>> GetApiLeadsAsync(
            string? search = null,
            LeadSource? source = null,
            LeadStatus? status = null);



        Task<LeadViewModel?> GetLeadByIdAsync(long id);

        // For Sales Manager to view a specific lead
        // assigned within their own hierarchy
        Task<LeadViewModel?> GetLeadForSalesManagerAsync(
            long leadId,
            long salesManagerId);




        Task<LeadViewModel?> GetArchivedLeadByIdAsync(long id);

        Task<LeadViewModel?> GetArchivedLeadForSalesManagerAsync(long leadId,long salesManagerId);

        Task<EditLeadViewModel?> GetLeadForEditAsync(long id);

        Task UpdateLeadAsync(EditLeadViewModel model);
        Task ArchiveLeadAsync(long id, long archivedBy);

        Task<List<LeadViewModel>> GetArchivedLeadsAsync();
        Task RestoreLeadAsync(long id);
        Task<AssignLeadViewModel?> GetAssignLeadViewModelAsync(long leadId);
        Task AssignLeadAsync(AssignLeadViewModel model, long adminId);

        Task<List<MyAssignedLeadViewModel>> GetAssignedLeadsAsync(long salesOfficerId);

        Task AcceptLeadAsync(long assignmentId, long salesOfficerId);

        Task<ReassignLeadViewModel?> GetReassignLeadViewModelAsync(long leadId);

        Task ReassignLeadAsync(
            ReassignLeadViewModel model,
            long salesManagerId);

        Task<List<UnassignedLeadViewModel>> GetUnassignedLeadsAsync(
            string? search = null,
            LeadSource? source = null,
            LeadPriority? priority = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );


        //for sales manager to view all leads assigned to their team members only
        Task<List<LeadViewModel>> GetLeadsForSalesManagerAsync(
            long salesManagerId);



        //for sales manager to edit a lead assigned to their team members only
        Task<EditLeadViewModel?> GetLeadForEditForSalesManagerAsync(long leadId,long salesManagerId);
        Task<bool> UpdateLeadForSalesManagerAsync(EditLeadViewModel model,long salesManagerId);

        //for sales manager to reassign a lead assigned to their team members only
        Task<ReassignLeadViewModel?> GetReassignLeadViewModelForSalesManagerAsync(long leadId,long salesManagerId);


        // Sales Manager Assign Lead to their team members only
        Task<AssignLeadViewModel?>GetAssignLeadViewModelForSalesManagerAsync(long leadId,long salesManagerId);
        Task<bool>AssignLeadForSalesManagerAsync(AssignLeadViewModel model, long salesManagerId);

        // Sales Manager Archive Lead and restore Lead assigned to their team members only
        Task<bool>ArchiveLeadForSalesManagerAsync(long leadId,long salesManagerId);
        Task<List<LeadViewModel>>GetArchivedLeadsForSalesManagerAsync(long salesManagerId);
        Task<bool> RestoreLeadForSalesManagerAsync(long leadId,long salesManagerId);

        //for sales manager to view api lead 
        Task<List<ApiLeadViewModel>> GetApiLeadsForSalesManagerAsync(
                long salesManagerId,
                string? search = null,
                LeadSource? source = null,
                LeadStatus? status = null);


        Task<List<UnassignedLeadViewModel>>
    GetUnassignedLeadsForSalesManagerAsync(
        long salesManagerId,
        string? search = null,
        LeadSource? source = null,
        LeadPriority? priority = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    }
}