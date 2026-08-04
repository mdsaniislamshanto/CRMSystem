using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CRMSystem.Constants;


namespace CRMSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ==========================
        // Login Page
        // ==========================
        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in
            if (_authService.GetCurrentUserId() != null)
            {
                return RedirectToAction(nameof(RedirectToDashboard));
            }

            return View();
        }

        // ==========================
        // Login Process
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(model);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            return RedirectToAction(nameof(RedirectToDashboard));
        }

        // ==========================
        // Dashboard Redirect
        // ==========================
        public IActionResult RedirectToDashboard()
        {
            var role = _authService.GetCurrentUserRole();

            switch (role)
            {
                case RoleKeys.Admin:
                    return RedirectToAction("Index", "Admin");

                case RoleKeys.SalesOfficer:
                    return RedirectToAction("Index", "Sales");

                case RoleKeys.Account:
                    return RedirectToAction("Index", "Account");

                case RoleKeys.SalesManager:
                    return RedirectToAction("Index", "SalesManager");

                case RoleKeys.HR:
                    return RedirectToAction("Index", "HR");

                default:
                    _authService.Logout();
                    return RedirectToAction(nameof(Login));
            }
        }

        // ==========================
        // Logout
        // ==========================
        public IActionResult Logout()
        {
            _authService.Logout();
            return RedirectToAction(nameof(Login));
        }


        //for assecc denied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
