using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using CRMSystem.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Linq;

namespace CRMSystem.Controllers
{
    [Authorize(Roles = RoleKeys.Admin)]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IFollowUpService _followUpService;

        public AdminController(
            IAuthService authService,
            ApplicationDbContext context,
            IUserService userService,
            IFollowUpService followUpService)
        {
            _authService = authService;
            _context = context;
            _userService = userService;
            _followUpService = followUpService;
        }


        // =====================================================
        // GET: Admin/Index
        // Admin Dashboard
        // =====================================================

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
        // Sales Officers List
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


        // =====================================================
        // GET: Admin/FollowUps
        // Follow-up List
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FollowUps(
            SalesManagerFollowUpFilterViewModel filter)
        {
            ViewData["Title"] = "Follow-ups";
            ViewData["Breadcrumb"] = "Follow-ups";

            var model = await _followUpService.GetFollowUpsAsync(filter);

            return View(model);
        }


        // =====================================================
        // GET: Admin/FollowUpDetails/{id}
        // Follow-up Details & Feedback History
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FollowUpDetails(long id)
        {
            ViewData["Title"] = "Follow-up Details";
            ViewData["Breadcrumb"] = "Follow-up Details";

            var model = await _followUpService.GetFollowUpDetailsAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }
    }
}