using CRMSystem.Models.DTOs;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ITargetService
    {
        // =====================================================
        // Admin → Sales Manager Target
        // =====================================================

        Task<CreateTargetViewModel>
            GetCreateTargetViewModelAsync();

        Task<ServiceResult>
            CreateTargetAsync(
                CreateTargetViewModel model,
                long createdByUserId);

        Task<List<TargetAchievementViewModel>>
            GetTargetAchievementsForAdminAsync();


        // =====================================================
        // Admin Dashboard → Overall Sales Manager Target
        // =====================================================

        Task<(int TotalTarget, int TargetFulfilled)>
            GetAdminTargetSummaryAsync();


        // =====================================================
        // Sales Manager → Sales Officer Target
        // Existing Functionality
        // =====================================================

        Task<List<OfficerTargetAchievementViewModel>>
            GetOfficerTargetAchievementsAsync(
                long salesManagerId);


        // =====================================================
        // Sales Manager → Team Lead Target
        // =====================================================

        Task<CreateTargetViewModel>
            GetCreateTeamLeadTargetViewModelAsync(
                long salesManagerId);

        Task<ServiceResult>
            CreateTeamLeadTargetAsync(
                CreateTargetViewModel model,
                long salesManagerId);

        Task<List<TargetAchievementViewModel>>
            GetTeamLeadTargetAchievementsAsync(
                long salesManagerId);


        // =====================================================
        // Completed Lead Count
        // =====================================================

        Task<int>
            GetCompletedLeadCountAsync(
                long userId,
                DateTime startDate,
                DateTime endDate);


        // =====================================================
        // Edit & End Target (Admin → Sales Manager)
        // =====================================================

        Task<EditTargetViewModel?>
            GetEditTargetViewModelAsync(
                long targetId);

        Task<ServiceResult>
            UpdateTargetAsync(
                EditTargetViewModel model,
                long currentAdminId);

        Task<ServiceResult>
            EndTargetAsync(
                long targetId,
                long currentAdminId);


        // =====================================================
        // Edit & End Target (Sales Manager → Team Lead)
        // =====================================================

        Task<EditTargetViewModel?>
            GetEditTeamLeadTargetViewModelAsync(
                long targetId,
                long salesManagerId);

        Task<ServiceResult>
            UpdateTeamLeadTargetAsync(
                EditTargetViewModel model,
                long salesManagerId);

        Task<ServiceResult>
            EndTeamLeadTargetAsync(
                long targetId,
                long salesManagerId);
    }
}