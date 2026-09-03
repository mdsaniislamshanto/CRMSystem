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
        private readonly ISalesOfficerPerformanceService _salesOfficerPerformanceService;
        private readonly IPerformanceExportService _performanceExportService;

        public AdminController(
            IAuthService authService,
            ApplicationDbContext context,
            IUserService userService,
            IFollowUpService followUpService,
            ISalesOfficerPerformanceService salesOfficerPerformanceService,
            IPerformanceExportService performanceExportService)
        {
            _authService = authService;
            _context = context;
            _userService = userService;
            _followUpService = followUpService;
            _salesOfficerPerformanceService = salesOfficerPerformanceService;
            _performanceExportService = performanceExportService;
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
            FollowUpFilterViewModel filter)
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


        // =====================================================
        // GET: Admin/Performance
        // Sales Officer Performance
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Performance(
            PerformanceFilterViewModel? filter)
        {
            filter ??= new PerformanceFilterViewModel();

            // -------------------------------------------------
            // Default Performance Range
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(filter.Range))
            {
                filter.Range = "ThisMonth";
            }


            // -------------------------------------------------
            // Custom Date Range Validation
            // -------------------------------------------------

            if (string.Equals(
                filter.Range,
                "Custom",
                StringComparison.OrdinalIgnoreCase))
            {
                if (!filter.FromDate.HasValue ||
                    !filter.ToDate.HasValue)
                {
                    TempData["Error"] =
                        "Please select both From Date and To Date.";

                    return RedirectToAction(nameof(Performance));
                }

                if (filter.FromDate.Value.Date >
                    filter.ToDate.Value.Date)
                {
                    TempData["Error"] =
                        "From Date cannot be later than To Date.";

                    return RedirectToAction(nameof(Performance));
                }
            }


            // -------------------------------------------------
            // Get Sales Officer Performance
            // -------------------------------------------------

            var performance =
                await _salesOfficerPerformanceService
                    .GetPerformanceAsync(filter);


            // -------------------------------------------------
            // Get Top Performer
            // -------------------------------------------------

            var topPerformer =
                await _salesOfficerPerformanceService
                    .GetTopPerformerAsync(filter);


            // -------------------------------------------------
            // Get Officer Needing Attention
            // -------------------------------------------------

            var needsAttention =
                await _salesOfficerPerformanceService
                    .GetNeedsAttentionAsync(filter);


            // -------------------------------------------------
            // Send Additional Data to View
            // -------------------------------------------------

            ViewData["PerformanceFilter"] = filter;
            ViewData["TopPerformer"] = topPerformer;
            ViewData["NeedsAttention"] = needsAttention;

            ViewData["Title"] = "Sales Officer Performance";
            ViewData["Breadcrumb"] = "Performance";


            // -------------------------------------------------
            // Return Performance View
            // -------------------------------------------------

            return View(performance);
        }


        // =====================================================
        // GET: Admin/PerformanceTrend
        // Performance Trend Chart Data
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> PerformanceTrend(
            PerformanceFilterViewModel filter)
        {
            var result =
                await _salesOfficerPerformanceService
                    .GetPerformanceTrendAsync(filter);

            return Json(result);
        }


        // =====================================================
        // GET: Admin/SLATrend
        // SLA Trend Chart Data
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> SLATrend(
            PerformanceFilterViewModel filter)
        {
            var result =
                await _salesOfficerPerformanceService
                    .GetSLATrendAsync(filter);

            return Json(result);
        }


        // =====================================================
        // GET: Admin/GetCompletionTrend
        // Completion Trend Chart Data
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetCompletionTrend(
            PerformanceFilterViewModel filter)
        {
            var result =
                await _salesOfficerPerformanceService
                    .GetCompletionTrendAsync(filter);

            return Json(result);
        }


        // =====================================================
        // GET: Admin/ExportPerformanceExcel
        // Export Performance Report to Excel
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportPerformanceExcel(
            PerformanceFilterViewModel filter)
        {
            var file =
                await _performanceExportService
                    .ExportPerformanceToExcelAsync(filter);

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesOfficerPerformance.xlsx");
        }


        // =====================================================
        // GET: Admin/ExportPerformancePdf
        // Export Performance Report to PDF
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportPerformancePdf(
            PerformanceFilterViewModel filter)
        {
            var file =
                await _performanceExportService
                    .ExportPerformanceToPdfAsync(filter);

            return File(
                file,
                "application/pdf",
                "SalesOfficerPerformance.pdf");
        }
    }
}