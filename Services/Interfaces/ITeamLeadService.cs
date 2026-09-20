using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ITeamLeadService
    {
        // =========================================================
        // Team Lead Dashboard
        // =========================================================

        Task<TeamLeadDashboardViewModel?>
            GetDashboardAsync(long teamLeadId);


        // =========================================================
        // Team Lead - My Team
        // =========================================================

        Task<TeamLeadMyTeamViewModel?>
            GetMyTeamAsync(long teamLeadId);


        // =========================================================
        // Team Lead - Officer Details
        // =========================================================

        Task<UserDetailsViewModel?>
            GetOfficerDetailsAsync(
                long officerId,
                long teamLeadId);


        // =========================================================
        // Team Lead - Lead Details
        // =========================================================

        Task<LeadViewModel?>
            GetLeadDetailsAsync(
                long leadId,
                long teamLeadId);


        // =========================================================
        // Team Lead - Feedback History
        // =========================================================

        Task<List<FeedbackHistoryViewModel>>
            GetFeedbackHistoryAsync(
                long leadId,
                long teamLeadId);


        // =========================================================
        // Team Lead - Feedback Details
        // =========================================================

        Task<FeedbackDetailsViewModel?>
            GetFeedbackDetailsAsync(
                long feedbackId,
                long teamLeadId);


        // =========================================================
        // Team Lead - Leads
        // =========================================================

        Task<List<LeadViewModel>>
            GetTeamLeadsAsync(
                long teamLeadId,
                string? search = null,
                LeadStatus? status = null,
                LeadPriority? priority = null,
                LeadSource? source = null,
                string? slaStatus = null,
                string? sort = null);


        // =========================================================
        // Team Lead - Follow-ups
        // =========================================================

        Task<FollowUpFilterViewModel>
            GetTeamFollowUpsAsync(
                long teamLeadId,
                FollowUpFilterViewModel filter);
    }
}