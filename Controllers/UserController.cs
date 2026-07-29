using CRMSystem.Attributes;
using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace CRMSystem.Controllers
{



    [SessionAuthorize]
    [RoleAuthorize(RoleKeys.Admin)]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        // GET: User/Index
        public async Task<IActionResult> Index(
                string? searchTerm,
                 string? role,
                string? status)
        {
            var users = await _userService.GetAllUsersAsync(
                searchTerm,
                role,
                status);

            return View(users);
        }


        // GET: for partial view table by search users by employee code, full name, or email
        [HttpGet]
        public async Task<IActionResult> SearchUsers(
                string? searchTerm,
                string? role,
                string? status)
        {
            var users = await _userService.GetAllUsersAsync(
                searchTerm,
                role,
                status);

            return PartialView("_UserTable", users);
        }

        // GET: User/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = await _userService.GetCreateUserViewModelAsync();

            return View(model);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = (await _userService.GetCreateUserViewModelAsync()).Roles;

                return View(model);
            }

            var result = await _userService.CreateUserAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                model.Roles = (await _userService.GetCreateUserViewModelAsync()).Roles;

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }


        // GET: User/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var model = await _userService.GetEditUserAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }


        // POST: User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = (await _userService.GetEditUserAsync(model.UserId))?.Roles
               ?? new List<SelectListItem>();

                return View(model);
            }

            var result = await _userService.UpdateUserAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                model.Roles = (await _userService.GetEditUserAsync((int)model.UserId))?.Roles
                              ?? new List<SelectListItem>();

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Details
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            ViewData["Title"] = "User Details";
            ViewData["Breadcrumb"] = "Users / Details";

            var model = await _userService.GetUserDetailsAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: User/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var result = await _userService.ToggleUserStatusAsync(id);

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}