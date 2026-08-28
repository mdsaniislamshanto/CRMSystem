using CRMSystem.Attributes;
using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize(RoleKeys.Admin)]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }


        // =====================================================
        // GET: /Settings
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();

            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile = await _settingsService.GetProfileAsync(userId.Value);

            if (profile == null)
            {
                return NotFound();
            }

            var model = new AdminSettingsViewModel
            {
                Profile = profile,

                AutoAssignmentEnabled =
                    settings.AutoAssignmentEnabled,

                GlobalNotificationsEnabled =
                    settings.GlobalNotificationsEnabled,

                LeadAssignmentNotificationEnabled =
                    settings.LeadAssignmentNotificationEnabled,

                FollowUpNotificationEnabled =
                    settings.FollowUpNotificationEnabled,

                FeedbackNotificationEnabled =
                    settings.FeedbackNotificationEnabled,

                OverdueNotificationEnabled =
                    settings.OverdueNotificationEnabled
            };

            // Admin's personal notification preference
            model.MyNotificationsEnabled =
                profile.NotificationsEnabled;

            ViewData["Title"] = "Settings";

            return View(model);
        }


        // =====================================================
        // POST: /Settings/UpdateAutoAssignment
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAutoAssignment(bool enabled)
        {
            await _settingsService.UpdateAutoAssignmentAsync(enabled);

            TempData["Success"] = enabled
                ? "Auto lead assignment has been enabled."
                : "Auto lead assignment has been disabled.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: /Settings/UpdateGlobalNotifications
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGlobalNotifications(
            bool globalNotificationsEnabled,
            bool leadAssignmentNotificationEnabled,
            bool followUpNotificationEnabled,
            bool feedbackNotificationEnabled,
            bool overdueNotificationEnabled)
        {
            await _settingsService.UpdateGlobalNotificationSettingsAsync(
                globalNotificationsEnabled,
                leadAssignmentNotificationEnabled,
                followUpNotificationEnabled,
                feedbackNotificationEnabled,
                overdueNotificationEnabled);

            TempData["Success"] =
                "Notification settings have been updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: /Settings/UpdateMyNotifications
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMyNotifications(
            bool enabled)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            await _settingsService
                .UpdateUserNotificationPreferenceAsync(
                    userId.Value,
                    enabled);

            TempData["Success"] = enabled
                ? "Your notifications have been enabled."
                : "Your notifications have been disabled.";

            return RedirectToAction(nameof(Index));
        }


      
        // =====================================================
        // GET: /Settings/EditProfile
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile =
                await _settingsService.GetProfileAsync(userId.Value);

            if (profile == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Profile";

            return View(profile);
        }


        // =====================================================
        // POST: /Settings/EditProfile
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Profile";

                return View(model);
            }

            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                await _settingsService.UpdateProfileAsync(
                    userId.Value,
                    model);

                TempData["Success"] =
                    "Your profile has been updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                ViewData["Title"] = "Edit Profile";

                return View(model);
            }
        }


        // =====================================================
        // GET: /Settings/ChangePassword
        // =====================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] = "Change Password";

            return View(new ChangePasswordViewModel());
        }


        // =====================================================
        // POST: /Settings/ChangePassword
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Change Password";

                return View(model);
            }

            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService.ChangePasswordAsync(
                    userId.Value,
                    model);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                ViewData["Title"] = "Change Password";

                return View(model);
            }

            TempData["Success"] =
                result.Message;

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // GET: /Settings/ApprovalRequests
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ApprovalRequests()
        {
            var profileRequests =
                await _settingsService
                    .GetPendingProfileChangeRequestsAsync();

            var autoAssignmentRequests =
                await _settingsService
                    .GetPendingAutoAssignmentRequestsAsync();

            var model = new AdminApprovalViewModel
            {
                ProfileChangeRequests =
                    profileRequests,

                AutoAssignmentRequests =
                    autoAssignmentRequests
            };

            ViewData["Title"] = "Approval Requests";

            return View(model);
        }


        // =====================================================
        // POST: /Settings/ApproveProfileChange
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfileChange(
            long requestId,
            string? adminComment)
        {
            var adminId = GetCurrentUserId();

            if (adminId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService
                    .ApproveProfileChangeRequestAsync(
                        requestId,
                        adminId.Value,
                        adminComment);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] = result.Message;

            return RedirectToAction(
                nameof(ApprovalRequests));
        }


        // =====================================================
        // POST: /Settings/RejectProfileChange
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProfileChange(
            long requestId,
            string? adminComment)
        {
            var adminId = GetCurrentUserId();

            if (adminId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService
                    .RejectProfileChangeRequestAsync(
                        requestId,
                        adminId.Value,
                        adminComment);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] = result.Message;

            return RedirectToAction(
                nameof(ApprovalRequests));
        }


        // =====================================================
        // POST: /Settings/ApproveAutoAssignment
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAutoAssignment(
            long requestId,
            string? adminComment)
        {
            var adminId = GetCurrentUserId();

            if (adminId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService
                    .ApproveAutoAssignmentRequestAsync(
                        requestId,
                        adminId.Value,
                        adminComment);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] = result.Message;

            return RedirectToAction(
                nameof(ApprovalRequests));
        }


        // =====================================================
        // POST: /Settings/RejectAutoAssignment
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAutoAssignment(
            long requestId,
            string? adminComment)
        {
            var adminId = GetCurrentUserId();

            if (adminId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService
                    .RejectAutoAssignmentRequestAsync(
                        requestId,
                        adminId.Value,
                        adminComment);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] = result.Message;

            return RedirectToAction(
                nameof(ApprovalRequests));
        }


        // =====================================================
        // Helper
        // =====================================================

        private long? GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (!long.TryParse(userId, out var parsedUserId))
            {
                return null;
            }

            return parsedUserId;
        }
    }
}