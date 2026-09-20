using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [Authorize(Roles = RoleKeys.TeamLead)]
    public class TeamLeadController : Controller
    {
        private readonly ITeamLeadService _teamLeadService;
        private readonly IAuthService _authService;
        private readonly ITeamLeadAnalyticsService _teamLeadAnalyticsService;
        private readonly ISettingsService _settingsService;
        private readonly IReportExportService _reportExportService;


        public TeamLeadController(
            ITeamLeadService teamLeadService,
            IAuthService authService,
            ITeamLeadAnalyticsService teamLeadAnalyticsService,
            ISettingsService settingsService,
            IReportExportService reportExportService)
        {
            _teamLeadService =
                teamLeadService;

            _authService =
                authService;

            _teamLeadAnalyticsService =
                teamLeadAnalyticsService;

            _settingsService =
                settingsService;
            _reportExportService =
       reportExportService;
        }


        // =========================================================
        // Team Lead Dashboard
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetDashboardAsync(
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Team Lead Dashboard";

            ViewData["Breadcrumb"] =
                "Dashboard";


            return View(model);
        }


        // =========================================================
        // Team Lead - My Team
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyTeam()
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetMyTeamAsync(
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "My Team";

            ViewData["Breadcrumb"] =
                "My Team";


            return View(model);
        }




        // =========================================================
        // Team Lead - Sales Officers
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SalesOfficers()
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetMyTeamAsync(
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Sales Officers";

            ViewData["Breadcrumb"] =
                "Sales Officers";


            return View(model);
        }


        // =========================================================
        // Team Lead - Officer Details
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> OfficerDetails(
            long id)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetOfficerDetailsAsync(
                        id,
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Sales Officer Details";

            ViewData["Breadcrumb"] =
                "My Team / Officer Details";


            return View(model);
        }


        // =========================================================
        // Team Lead - Lead Details
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> LeadDetails(
            long id)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetLeadDetailsAsync(
                        id,
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Lead Details";

            ViewData["Breadcrumb"] =
                "Leads / Lead Details";


            return View(model);
        }


        // =========================================================
        // Team Lead - Feedback History
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> FeedbackHistory(
            long id)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var history =
                await _teamLeadService
                    .GetFeedbackHistoryAsync(
                        id,
                        teamLeadId.Value);

            var lead =
                await _teamLeadService
                    .GetLeadDetailsAsync(
                        id,
                        teamLeadId.Value);

            if (lead == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewBag.Lead =
                lead;


            ViewData["Title"] =
                "Feedback History";

            ViewData["Breadcrumb"] =
                "Leads / Feedback History";


            return View(history);
        }


        // =========================================================
        // Team Lead - Feedback Details
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> FeedbackDetails(
            long id)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetFeedbackDetailsAsync(
                        id,
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Feedback Details";

            ViewData["Breadcrumb"] =
                "Leads / Feedback History / Details";


            return View(model);
        }


        // =========================================================
        // Team Lead - Leads
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Leads(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            LeadSource? source,
            string? slaStatus,
            string? sort,
            int page = 1)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var allLeads =
                await _teamLeadService
                    .GetTeamLeadsAsync(
                        teamLeadId.Value,
                        search,
                        status,
                        priority,
                        source,
                        slaStatus,
                        sort);


            // -----------------------------------------------------
            // Summary Statistics
            // -----------------------------------------------------

            ViewBag.TotalActiveLeads =
                allLeads.Count;

            ViewBag.NewLeads =
                allLeads.Count(x =>
                    x.Status ==
                    LeadStatus.New);

            ViewBag.AssignedLeads =
                allLeads.Count(x =>
                    x.Status ==
                    LeadStatus.Assigned);

            ViewBag.AcceptedLeads =
                allLeads.Count(x =>
                    x.Status ==
                    LeadStatus.Accepted);

            ViewBag.SLABreachedLeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAMissed);

            ViewBag.WithinSLALeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAStatus ==
                    "Within SLA");

            ViewBag.PendingSLALeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAStatus != null &&
                    x.AcceptanceSLAStatus.StartsWith(
                        "Pending",
                        StringComparison.OrdinalIgnoreCase));


            // -----------------------------------------------------
            // Preserve Filters
            // -----------------------------------------------------

            ViewBag.Search =
                search;

            ViewBag.Status =
                status;

            ViewBag.Priority =
                priority;

            ViewBag.Source =
                source;

            ViewBag.SLAStatus =
                slaStatus;

            ViewBag.Sort =
                sort;


            // -----------------------------------------------------
            // Pagination
            // -----------------------------------------------------

            const int pageSize = 10;

            var totalFilteredLeads =
                allLeads.Count;

            var totalPages =
                totalFilteredLeads == 0
                    ? 1
                    : (int)Math.Ceiling(
                        totalFilteredLeads /
                        (double)pageSize);


            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }


            var paginatedLeads =
                allLeads
                    .Skip(
                        (page - 1) *
                        pageSize)
                    .Take(pageSize)
                    .ToList();


            ViewBag.CurrentPage =
                page;

            ViewBag.TotalPages =
                totalPages;

            ViewBag.TotalFilteredLeads =
                totalFilteredLeads;

            ViewBag.PageSize =
                pageSize;


            ViewData["Title"] =
                "Team Leads";

            ViewData["Breadcrumb"] =
                "Leads";


            return View(
                "Leads",
                paginatedLeads);
        }


        // =========================================================
        // Team Lead - Follow-ups
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> FollowUps(
            string? search,
            string? followUpStatus,
            string? feedbackStatus,
            string? sort,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var filter =
                new FollowUpFilterViewModel
                {
                    Search =
                        search,

                    FollowUpStatus =
                        followUpStatus,

                    FeedbackStatus =
                        feedbackStatus,

                    Sort =
                        sort,

                    FromDate =
                        fromDate,

                    ToDate =
                        toDate,

                    Page =
                        page,

                    PageSize =
                        10
                };


            var model =
                await _teamLeadService
                    .GetTeamFollowUpsAsync(
                        teamLeadId.Value,
                        filter);


            ViewData["Title"] =
                "Follow-ups";

            ViewData["Breadcrumb"] =
                "Follow-ups";


            return View(model);
        }


        // =========================================================
        // Team Lead - Follow-up Details
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> FollowUpDetails(
            long id)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var model =
                await _teamLeadService
                    .GetLeadDetailsAsync(
                        id,
                        teamLeadId.Value);

            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Follow-up Details";

            ViewData["Breadcrumb"] =
                "Follow-ups / Details";


            return View(model);
        }


        // =========================================================
        // Team Lead - Performance
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Performance(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            var today =
                DateTime.UtcNow.Date;


            var startDate =
                fromDate?.Date
                ?? new DateTime(
                    today.Year,
                    today.Month,
                    1);


            var endDate =
                toDate?.Date
                ?? today;


            if (startDate > endDate)
            {
                var temp =
                    startDate;

                startDate =
                    endDate;

                endDate =
                    temp;
            }


            var model =
                await _teamLeadAnalyticsService
                    .GetPerformanceTrendAsync(
                        teamLeadId.Value,
                        startDate,
                        endDate);


            if (model == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }


            ViewData["Title"] =
                "Team Performance";

            ViewData["Breadcrumb"] =
                "Performance";


            return View(model);
        }


        // =========================================================
        // Team Lead - Performance Trend
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> PerformanceTrend(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return Unauthorized();
            }


            var today =
                DateTime.UtcNow.Date;


            var startDate =
                fromDate?.Date
                ?? new DateTime(
                    today.Year,
                    today.Month,
                    1);


            var endDate =
                toDate?.Date
                ?? today;


            if (startDate > endDate)
            {
                var temp =
                    startDate;

                startDate =
                    endDate;

                endDate =
                    temp;
            }


            var model =
                await _teamLeadAnalyticsService
                    .GetPerformanceTrendAsync(
                        teamLeadId.Value,
                        startDate,
                        endDate);


            if (model == null)
            {
                return NotFound();
            }


            return Json(model);
        }



        // =========================================================
        // Team Lead Reports
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Reports(
            DateTime? fromDate,
            DateTime? toDate)
        {
            // -----------------------------------------------------
            // Get Logged-in Team Lead ID
            // -----------------------------------------------------

            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            // -----------------------------------------------------
            // Default Date Range
            // -----------------------------------------------------

            var selectedFromDate =
                fromDate?.Date ??
                DateTime.Today.AddDays(-29);

            var selectedToDate =
                toDate?.Date ??
                DateTime.Today;


            // -----------------------------------------------------
            // Get Report
            // -----------------------------------------------------

            var report =
                await _teamLeadAnalyticsService
                    .GetReportAsync(
                        teamLeadId.Value,
                        selectedFromDate,
                        selectedToDate);


            // -----------------------------------------------------
            // Team Lead Not Found
            // -----------------------------------------------------

            if (report == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Page Information
            // -----------------------------------------------------

            ViewData["Title"] =
                "Team Lead Reports";

            ViewData["Breadcrumb"] =
                "Reports";


            // -----------------------------------------------------
            // Return Report View
            // -----------------------------------------------------

            return View(report);
        }



        // =========================================================
        // Download Team Lead Report - PDF
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DownloadReportPdf(
            DateTime? fromDate,
            DateTime? toDate,
            string? range)
        {
            // -----------------------------------------------------
            // Get Logged-in Team Lead
            // -----------------------------------------------------

            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            // -----------------------------------------------------
            // Resolve Date Range
            // -----------------------------------------------------

            var today =
                DateTime.Today;

            var selectedFromDate =
                today.AddDays(-29);

            var selectedToDate =
                today;


            switch (range?.ToLowerInvariant())
            {
                case "today":

                    selectedFromDate =
                        today;

                    selectedToDate =
                        today;

                    break;


                case "week":

                    var dayOfWeek =
                        today.DayOfWeek;

                    var daysFromMonday =
                        dayOfWeek == DayOfWeek.Sunday
                            ? 6
                            : (int)dayOfWeek - 1;

                    selectedFromDate =
                        today.AddDays(-daysFromMonday);

                    selectedToDate =
                        today;

                    break;


                case "month":

                    selectedFromDate =
                        new DateTime(
                            today.Year,
                            today.Month,
                            1);

                    selectedToDate =
                        today;

                    break;


                default:

                    if (fromDate.HasValue)
                    {
                        selectedFromDate =
                            fromDate.Value.Date;
                    }

                    if (toDate.HasValue)
                    {
                        selectedToDate =
                            toDate.Value.Date;
                    }

                    break;
            }


            // -----------------------------------------------------
            // Get Team Lead Report
            // -----------------------------------------------------

            var report =
                await _teamLeadAnalyticsService
                    .GetReportAsync(
                        teamLeadId.Value,
                        selectedFromDate,
                        selectedToDate);


            if (report == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Generate PDF
            // -----------------------------------------------------

            var fileBytes =
                _reportExportService
                    .GenerateTeamLeadPdf(report);


            // -----------------------------------------------------
            // File Name
            // -----------------------------------------------------

            var fileName =
                $"TeamLead_Report_" +
                $"{selectedFromDate:yyyyMMdd}_" +
                $"{selectedToDate:yyyyMMdd}.pdf";


            // -----------------------------------------------------
            // Return File
            // -----------------------------------------------------

            return File(
                fileBytes,
                "application/pdf",
                fileName);
        }



        // =========================================================
        // Download Team Lead Report - Excel
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DownloadReportExcel(
            DateTime? fromDate,
            DateTime? toDate,
            string? range)
        {
            // -----------------------------------------------------
            // Get Logged-in Team Lead
            // -----------------------------------------------------

            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            // -----------------------------------------------------
            // Resolve Date Range
            // -----------------------------------------------------

            var today =
                DateTime.Today;

            var selectedFromDate =
                today.AddDays(-29);

            var selectedToDate =
                today;


            switch (range?.ToLowerInvariant())
            {
                case "today":

                    selectedFromDate =
                        today;

                    selectedToDate =
                        today;

                    break;


                case "week":

                    var dayOfWeek =
                        today.DayOfWeek;

                    var daysFromMonday =
                        dayOfWeek == DayOfWeek.Sunday
                            ? 6
                            : (int)dayOfWeek - 1;

                    selectedFromDate =
                        today.AddDays(-daysFromMonday);

                    selectedToDate =
                        today;

                    break;


                case "month":

                    selectedFromDate =
                        new DateTime(
                            today.Year,
                            today.Month,
                            1);

                    selectedToDate =
                        today;

                    break;


                default:

                    if (fromDate.HasValue)
                    {
                        selectedFromDate =
                            fromDate.Value.Date;
                    }

                    if (toDate.HasValue)
                    {
                        selectedToDate =
                            toDate.Value.Date;
                    }

                    break;
            }


            // -----------------------------------------------------
            // Get Team Lead Report
            // -----------------------------------------------------

            var report =
                await _teamLeadAnalyticsService
                    .GetReportAsync(
                        teamLeadId.Value,
                        selectedFromDate,
                        selectedToDate);


            if (report == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Generate Excel
            // -----------------------------------------------------

            var fileBytes =
                _reportExportService
                    .GenerateTeamLeadExcel(report);


            // -----------------------------------------------------
            // File Name
            // -----------------------------------------------------

            var fileName =
                $"TeamLead_Report_" +
                $"{selectedFromDate:yyyyMMdd}_" +
                $"{selectedToDate:yyyyMMdd}.xlsx";


            // -----------------------------------------------------
            // Return File
            // -----------------------------------------------------

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        // =========================================================
        // GET: Team Lead - Profile
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var profile =
                await _settingsService
                    .GetProfileAsync(
                        teamLeadId.Value);

            if (profile == null)
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Auth");
            }

            ViewData["Title"] =
                "My Profile";

            ViewData["Breadcrumb"] =
                "Profile";

            return View(profile);
        }


        // =========================================================
        // POST: Team Lead - Update Profile
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            ProfileViewModel model)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] =
                    "My Profile";

                ViewData["Breadcrumb"] =
                    "Profile";

                return View(
                    "Profile",
                    model);
            }

            var result =
                await _settingsService
                    .SubmitProfileChangeRequestAsync(
                        teamLeadId.Value,
                        model);

            TempData[result.IsSuccess
                ? "Success"
                : "Error"] =
                result.Message;

            return RedirectToAction(
                nameof(Profile));
        }


        // =========================================================
        // GET: Team Lead - Change Password
        // =========================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Title"] =
                "Change Password";

            ViewData["Breadcrumb"] =
                "Profile / Change Password";

            return View(
                new ChangePasswordViewModel());
        }


        // =========================================================
        // POST: Team Lead - Change Password
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] =
                    "Change Password";

                ViewData["Breadcrumb"] =
                    "Profile / Change Password";

                return View(model);
            }

            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var result =
                await _settingsService
                    .ChangePasswordAsync(
                        teamLeadId.Value,
                        model);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                ViewData["Title"] =
                    "Change Password";

                ViewData["Breadcrumb"] =
                    "Profile / Change Password";

                return View(model);
            }

            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(Profile));
        }

        // =========================================================
        // POST: Team Lead - Change Profile Picture
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfilePicture(
            IFormFile profileImage)
        {
            var teamLeadId =
                _authService.GetCurrentUserId();

            if (!teamLeadId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var result =
                await _settingsService
                    .UpdateProfileImageAsync(
                        teamLeadId.Value,
                        profileImage);

            if (!result.IsSuccess)
            {
                TempData["Error"] =
                    result.Message;

                return RedirectToAction(
                    nameof(Profile));
            }

            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(Profile));
        }
    }
}