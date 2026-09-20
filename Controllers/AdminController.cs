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
        private readonly ITargetService _targetService;
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminController(
            IAuthService authService,
            ApplicationDbContext context,
            IUserService userService,
            IFollowUpService followUpService,
            ISalesOfficerPerformanceService salesOfficerPerformanceService,
            IPerformanceExportService performanceExportService,
            ITargetService targetService,
            IAdminDashboardService adminDashboardService)
        {
            _authService = authService;
            _context = context;
            _userService = userService;
            _followUpService = followUpService;
            _salesOfficerPerformanceService = salesOfficerPerformanceService;
            _performanceExportService = performanceExportService;
            _targetService = targetService;
            _adminDashboardService = adminDashboardService;
        }


        // =====================================================
        // GET: Admin/Index
        // Admin Dashboard
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin Dashboard";

            var model =
                await _adminDashboardService
                    .GetDashboardAsync();

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

            var model =
                await _followUpService
                    .GetFollowUpsAsync(filter);

            return View(model);
        }


        // =====================================================
        // GET: Admin/FollowUpDetails/{id}
        // Follow-up Details & Feedback History
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FollowUpDetails(
            long id)
        {
            ViewData["Title"] =
                "Follow-up Details";

            ViewData["Breadcrumb"] =
                "Follow-up Details";

            var model =
                await _followUpService
                    .GetFollowUpDetailsAsync(id);

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
            filter ??=
                new PerformanceFilterViewModel();

            // -------------------------------------------------
            // Default Performance Range
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                filter.Range))
            {
                filter.Range =
                    "ThisMonth";
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

                    return RedirectToAction(
                        nameof(Performance));
                }

                if (filter.FromDate.Value.Date >
                    filter.ToDate.Value.Date)
                {
                    TempData["Error"] =
                        "From Date cannot be later than To Date.";

                    return RedirectToAction(
                        nameof(Performance));
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

            ViewData["PerformanceFilter"] =
                filter;

            ViewData["TopPerformer"] =
                topPerformer;

            ViewData["NeedsAttention"] =
                needsAttention;

            ViewData["Title"] =
                "Sales Officer Performance";

            ViewData["Breadcrumb"] =
                "Performance";


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
        public async Task<IActionResult>
            ExportPerformanceExcel(
                PerformanceFilterViewModel filter)
        {
            var file =
                await _performanceExportService
                    .ExportPerformanceToExcelAsync(
                        filter);

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
        public async Task<IActionResult>
            ExportPerformancePdf(
                PerformanceFilterViewModel filter)
        {
            var file =
                await _performanceExportService
                    .ExportPerformanceToPdfAsync(
                        filter);

            return File(
                file,
                "application/pdf",
                "SalesOfficerPerformance.pdf");
        }


        // =====================================================
        // GET: Admin/Targets
        // Sales Manager Target Management
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Targets()
        {
            ViewData["Title"] =
                "Sales Target Management";

            ViewData["Breadcrumb"] =
                "Sales Targets";

            var targets =
                await _targetService
                    .GetTargetAchievementsForAdminAsync();

            return View(targets);
        }


        // =====================================================
        // GET: Admin/CreateTarget
        // Create Sales Manager Target
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> CreateTarget()
        {
            ViewData["Title"] =
                "Create Sales Manager Target";

            ViewData["Breadcrumb"] =
                "Sales Targets / Create";

            var model =
                await _targetService
                    .GetCreateTargetViewModelAsync();

            return View(model);
        }


        // =====================================================
        // POST: Admin/CreateTarget
        // Create Sales Manager Target
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTarget(
            CreateTargetViewModel model)
        {
            ViewData["Title"] =
                "Create Sales Manager Target";

            ViewData["Breadcrumb"] =
                "Sales Targets / Create";


            // -------------------------------------------------
            // Model Validation
            // -------------------------------------------------

            if (!ModelState.IsValid)
            {
                var invalidModel =
                    await _targetService
                        .GetCreateTargetViewModelAsync();

                invalidModel.UserId =
                    model.UserId;

                invalidModel.PeriodType =
                    model.PeriodType;

                invalidModel.TargetCount =
                    model.TargetCount;

                invalidModel.StartDate =
                    model.StartDate;

                invalidModel.EndDate =
                    model.EndDate;

                return View(invalidModel);
            }


            // -------------------------------------------------
            // Current Admin User
            // -------------------------------------------------

            var currentAdminId =
                _authService.GetCurrentUserId();

            if (!currentAdminId.HasValue)
            {
                return Unauthorized();
            }


            // -------------------------------------------------
            // Create Target
            // -------------------------------------------------

            var result =
                await _targetService
                    .CreateTargetAsync(
                        model,
                        currentAdminId.Value);


            // -------------------------------------------------
            // Handle Service Result
            // -------------------------------------------------

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                var failedModel =
                    await _targetService
                        .GetCreateTargetViewModelAsync();

                failedModel.UserId =
                    model.UserId;

                failedModel.PeriodType =
                    model.PeriodType;

                failedModel.TargetCount =
                    model.TargetCount;

                failedModel.StartDate =
                    model.StartDate;

                failedModel.EndDate =
                    model.EndDate;

                return View(failedModel);
            }


            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(Targets));
        }
    }
}