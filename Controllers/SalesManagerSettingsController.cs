using CRMSystem.Attributes;
using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize(RoleKeys.SalesManager)]
    public class SalesManagerSettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SalesManagerSettingsController(
            ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }


        // =====================================================
        // GET: /SalesManagerSettings
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var profile =
                await _settingsService
                    .GetProfileAsync(userId.Value);

            if (profile == null)
            {
                return NotFound();
            }

            var settings =
                await _settingsService
                    .GetSettingsAsync();

            var autoAssignmentRequests =
                await _settingsService
                    .GetMyAutoAssignmentRequestsAsync(
                        userId.Value);

            ViewData["Title"] = "Settings";

            ViewBag.AutoAssignmentEnabled =
                settings.AutoAssignmentEnabled;

            ViewBag.AutoAssignmentRequests =
                autoAssignmentRequests;

            return View(profile);
        }


        // =====================================================
        // GET: /SalesManagerSettings/EditProfile
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
                await _settingsService
                    .GetProfileAsync(userId.Value);

            if (profile == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Profile";

            return View(profile);
        }


        // =====================================================
        // POST: /SalesManagerSettings/EditProfile
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            ProfileViewModel model)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Profile";

                return View(model);
            }

            var result =
                await _settingsService
                    .SubmitProfileChangeRequestAsync(
                        userId.Value,
                        model);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // GET: /SalesManagerSettings/ChangePassword
        // =====================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] = "Change Password";

            return View(
                new ChangePasswordViewModel());
        }


        // =====================================================
        // POST: /SalesManagerSettings/ChangePassword
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Change Password";

                return View(model);
            }

            var result =
                await _settingsService
                    .ChangePasswordAsync(
                        userId.Value,
                        model);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;

                return View(model);
            }

            TempData["Success"] =
                "Password changed successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: /SalesManagerSettings/UpdateProfileImage
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileImage(
            IFormFile profileImage)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            if (profileImage == null ||
                profileImage.Length == 0)
            {
                TempData["Error"] =
                    "Please select a profile image.";

                return RedirectToAction(nameof(Index));
            }

            var result =
                await _settingsService
                    .UpdateProfileImageAsync(
                        userId.Value,
                        profileImage);

            if (!result.IsSuccess)
            {
                TempData["Error"] =
                    result.Message;

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Profile picture updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: /SalesManagerSettings/RequestAutoAssignment
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            RequestAutoAssignment(bool enabled)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result =
                await _settingsService
                    .SubmitAutoAssignmentRequestAsync(
                        userId.Value,
                        enabled);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // Helper
        // =====================================================

        private long? GetCurrentUserId()
        {
            var userId =
                HttpContext.Session
                    .GetString(SessionKeys.UserId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            if (!long.TryParse(
                userId,
                out var parsedUserId))
            {
                return null;
            }

            return parsedUserId;
        }
    }
}