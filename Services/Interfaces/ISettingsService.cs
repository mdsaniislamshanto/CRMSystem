using CRMSystem.Models.DTOs;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ISettingsService
    {
        // =====================================================
        // System Settings
        // =====================================================

        Task<SystemSettings> GetSettingsAsync();

        // Admin only - Auto Assignment setting update
        // without approval request
        Task UpdateAutoAssignmentAsync(bool enabled);


        // =====================================================
        // Notification Settings
        // =====================================================

        // Admin only - update global notification settings
        Task UpdateGlobalNotificationSettingsAsync(
            bool globalNotificationsEnabled,
            bool leadAssignmentNotificationEnabled,
            bool followUpNotificationEnabled,
            bool feedbackNotificationEnabled,
            bool overdueNotificationEnabled);

        // Admin only - update own notification preference
        Task UpdateUserNotificationPreferenceAsync(
            long userId,
            bool enabled);


        // =====================================================
        // Profile
        // =====================================================

        // User profile retrieval
        Task<ProfileViewModel?> GetProfileAsync(long userId);

        // Admin only - direct profile update without approval
        Task UpdateProfileAsync(
            long userId,
            ProfileViewModel model);


        // =====================================================
        // Password
        // =====================================================

        Task<ServiceResult> ChangePasswordAsync(
            long userId,
            ChangePasswordViewModel model);


        // =====================================================
        // Profile Change Requests
        // =====================================================

        Task<ServiceResult> SubmitProfileChangeRequestAsync(
            long userId,
            ProfileViewModel model);

        Task<List<ProfileChangeRequestViewModel>>
            GetPendingProfileChangeRequestsAsync();

        Task<ServiceResult> ApproveProfileChangeRequestAsync(
            long requestId,
            long adminId,
            string? adminComment);

        Task<ServiceResult> RejectProfileChangeRequestAsync(
            long requestId,
            long adminId,
            string? adminComment);


        // =====================================================
        // Auto Assignment Requests
        // =====================================================

        Task<ServiceResult> SubmitAutoAssignmentRequestAsync(
            long salesManagerId,
            bool enabled);

        Task<List<AutoAssignmentRequestViewModel>>
            GetPendingAutoAssignmentRequestsAsync();

        Task<ServiceResult> ApproveAutoAssignmentRequestAsync(
            long requestId,
            long adminId,
            string? adminComment);

        Task<ServiceResult> RejectAutoAssignmentRequestAsync(
            long requestId,
            long adminId,
            string? adminComment);
    }
}