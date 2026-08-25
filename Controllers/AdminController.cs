using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using CRMSystem.ViewModels;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CRMSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public AdminController(
            IAuthService authService,
            ApplicationDbContext context,
            IUserService userService)
        {
            _authService = authService;
            _context = context;
            _userService = userService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Dashboard";

            var model = new AdminDashboardViewModel
            {
                NewLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.New),

                AssignedLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.Assigned),

                AcceptedLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.Accepted),

                InProgressLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.InProgress),

                CompletedLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.Completed),

                RejectedLeads = _context.Leads.Count(
                    l => l.Status == LeadStatus.Rejected)
            };

            return View(model);
        }


        // =====================================================
        // GET: Admin/SalesOfficers
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SalesOfficers(
            string? searchTerm,
            string? status)
        {
            ViewData["Title"] = "Sales Officers";
            ViewData["Breadcrumb"] = "Sales Officers";

            var users = await _userService.GetAllUsersAsync(
                searchTerm,
                "Sales Officer",
                status);

            return View(users);
        }

        // GET: Admin/SalesOfficers
        public async Task<IActionResult> SalesOfficers()
        {
            ViewData["Title"] = "Sales Officers";
            ViewData["Breadcrumb"] = "Sales Officers";

            var users = await _context.Users
                .Where(u => u.Role != null &&
                            u.Role.RoleName == "Sales Officer")
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    EmployeeCode = u.EmployeeCode,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RoleName = u.Role.RoleName,
                    IsEmailVerified = u.IsEmailVerified,
                    LastLoginAt = u.LastLoginAt,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return View(users);
        }
    }
}