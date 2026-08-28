using CRMSystem.Attributes;
using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize(RoleKeys.SalesOfficer)]
    public class SalesOfficerSettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SalesOfficerSettingsController(
            ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }


        // =====================================================
        // GET: /SalesOfficerSettings
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

            ViewData["Title"] = "Settings";

            return View(profile);
        }


        // =====================================================
        // GET: /SalesOfficerSettings/EditProfile
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
        // POST: /SalesOfficerSettings/EditProfile
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


            var result =
                await _settingsService
                    .SubmitProfileChangeRequestAsync(
                        userId.Value,
                        model);


            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                ViewData["Title"] = "Edit Profile";

                return View(model);
            }


            TempData["Success"] =
                result.Message;


            return RedirectToAction(nameof(Index));
        }



        // =====================================================
        // GET: /SalesOfficerSettings/ChangePassword
        // =====================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] = "Change Password";

            return View(
                new ChangePasswordViewModel());
        }


        // =====================================================
        // POST: /SalesOfficerSettings/ChangePassword
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
        // Helper
        // =====================================================

        private long? GetCurrentUserId()
        {
            var userId =
                HttpContext.Session.GetString("UserId");

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



        // =====================================================
        // POST: /SalesOfficerSettings/UpdateProfileImage
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


            var result =
                await _settingsService.UpdateProfileImageAsync(
                    userId.Value,
                    profileImage);


            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;

                return RedirectToAction(nameof(Index));
            }


            TempData["Success"] =
                result.Message;


            return RedirectToAction(nameof(Index));
        }
    }
}