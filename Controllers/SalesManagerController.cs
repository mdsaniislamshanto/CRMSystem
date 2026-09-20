using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRMSystem.Controllers
{
    public class SalesManagerController : Controller
    {
        private readonly ISalesManagerDashboardService _dashboardService;
        private readonly ILeadService _leadService;
        private readonly ISettingsService _settingsService;
        private readonly ILeadCaptureService _leadCaptureService;
        private readonly IReportService _reportService;
        private readonly ISalesOfficerServiceForSalesManager _salesOfficerServiceForSalesManager;
        private readonly ISalesOfficerPerformanceService _salesOfficerPerformanceService;
        private readonly IPerformanceExportService _performanceExportService;
        private readonly IFollowUpService _followUpService;
        private readonly ITargetService _targetService;

        public SalesManagerController(
            ISalesManagerDashboardService dashboardService,
            ILeadService leadService,
            ISettingsService settingsService,
            ILeadCaptureService leadCaptureService,
            IReportService reportService,
            ISalesOfficerServiceForSalesManager salesOfficerServiceForSalesManager,
            ISalesOfficerPerformanceService salesOfficerPerformanceService,
            IPerformanceExportService performanceExportService,
            IFollowUpService followUpService,
            ITargetService targetService)
        {
            _dashboardService = dashboardService;
            _leadService = leadService;
            _settingsService = settingsService;
            _leadCaptureService = leadCaptureService;
            _reportService = reportService;
            _salesOfficerServiceForSalesManager =
                salesOfficerServiceForSalesManager;
            _salesOfficerPerformanceService =
                salesOfficerPerformanceService;
            _performanceExportService =
                performanceExportService;
            _followUpService =
                followUpService;
            _targetService =
                targetService;
        }

        // ==========================
        // Dashboard
        // ==========================
        public async Task<IActionResult> Index()
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model =
                await _dashboardService
                    .GetDashboardAsync(
                        salesManagerId.Value);

            return View(model);
        }

        // ==========================
        // Lead Queue
        // ==========================
        [HttpGet]
        public async Task<IActionResult> LeadQueue(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            LeadSource? source,
            string? slaStatus,
            string? sort,
            int page = 1)
        {
            ViewData["Title"] = "Lead Queue";
            ViewData["Breadcrumb"] = "Lead Queue";

            var salesManagerId = _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var allLeads =
                await _leadService.GetLeadsForSalesManagerAsync(
                    salesManagerId.Value);

            ViewBag.TotalActiveLeads =
                allLeads.Count;

            ViewBag.NewLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.New);

            ViewBag.AssignedLeads =
                allLeads.Count(x =>
                    x.Status != LeadStatus.New &&
                    x.Status != LeadStatus.Completed);

            ViewBag.AcceptedLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.Accepted);

            ViewBag.HighPriorityLeads =
                allLeads.Count(x =>
                    x.Priority == LeadPriority.High);

            ViewBag.UnassignedLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.New);

            ViewBag.SLABreachedLeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAMissed);

            ViewBag.WithinSLALeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAStatus == "Within SLA");

            ViewBag.PendingSLALeads =
                allLeads.Count(x =>
                    x.AcceptanceSLAStatus != null &&
                    x.AcceptanceSLAStatus.StartsWith(
                        "Pending",
                        StringComparison.OrdinalIgnoreCase));

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Source = source;
            ViewBag.SLAStatus = slaStatus;
            ViewBag.Sort = sort;

            IEnumerable<LeadViewModel> filteredLeads =
                allLeads;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchValue =
                    search.Trim();

                filteredLeads =
                    filteredLeads.Where(lead =>
                        (!string.IsNullOrWhiteSpace(
                            lead.LeadCode) &&
                         lead.LeadCode.Contains(
                             searchValue,
                             StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(
                            lead.LeadName) &&
                         lead.LeadName.Contains(
                             searchValue,
                             StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(
                            lead.CompanyName) &&
                         lead.CompanyName.Contains(
                             searchValue,
                             StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(
                            lead.Phone) &&
                         lead.Phone.Contains(
                             searchValue,
                             StringComparison.OrdinalIgnoreCase))
                    );
            }

            if (status.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Status == status.Value);
            }

            if (priority.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Priority == priority.Value);
            }

            if (source.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Source == source.Value);
            }

            if (!string.IsNullOrWhiteSpace(slaStatus))
            {
                switch (slaStatus.Trim()
                    .ToLowerInvariant())
                {
                    case "notassigned":

                        filteredLeads =
                            filteredLeads.Where(x =>
                                x.AcceptanceSLAStatus ==
                                "Not Assigned");

                        break;

                    case "pending":

                        filteredLeads =
                            filteredLeads.Where(x =>
                                x.AcceptanceSLAStatus != null &&
                                x.AcceptanceSLAStatus.StartsWith(
                                    "Pending",
                                    StringComparison.OrdinalIgnoreCase));

                        break;

                    case "withinsla":

                        filteredLeads =
                            filteredLeads.Where(x =>
                                x.AcceptanceSLAStatus ==
                                "Within SLA");

                        break;

                    case "breached":

                        filteredLeads =
                            filteredLeads.Where(x =>
                                x.AcceptanceSLAMissed ||
                                x.AcceptanceSLAStatus ==
                                "SLA Breached");

                        break;
                }
            }

            filteredLeads =
                sort switch
                {
                    "oldest" =>
                        filteredLeads
                            .OrderBy(x => x.LeadId),

                    "name" =>
                        filteredLeads
                            .OrderBy(x => x.LeadName),

                    "name_desc" =>
                        filteredLeads
                            .OrderByDescending(
                                x => x.LeadName),

                    "priority" =>
                        filteredLeads
                            .OrderByDescending(
                                x => x.Priority)
                            .ThenByDescending(
                                x => x.LeadId),

                    "priority_low" =>
                        filteredLeads
                            .OrderBy(x => x.Priority)
                            .ThenByDescending(
                                x => x.LeadId),

                    "status" =>
                        filteredLeads
                            .OrderBy(x => x.Status)
                            .ThenByDescending(
                                x => x.LeadId),

                    "sla" =>
                        filteredLeads
                            .OrderByDescending(
                                x => x.AcceptanceSLAMissed)
                            .ThenBy(x =>
                                x.AcceptanceSLAStatus)
                            .ThenByDescending(
                                x => x.LeadId),

                    _ =>
                        filteredLeads
                            .OrderByDescending(
                                x => x.LeadId)
                };

            const int pageSize = 10;

            var filteredList =
                filteredLeads.ToList();

            var totalFilteredLeads =
                filteredList.Count;

            var totalPages =
                (int)Math.Ceiling(
                    totalFilteredLeads /
                    (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var paginatedLeads =
                filteredList
                    .Skip((page - 1) * pageSize)
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

            return View(paginatedLeads);
        }





        // =========================================================
        // API Leads - For Sales Manager
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ApiLeads(
            string? search,
            LeadSource? source,
            LeadStatus? status)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var leads =
                await _leadService
                    .GetApiLeadsForSalesManagerAsync(
                        salesManagerId.Value,
                        search,
                        source,
                        status);

            var model =
                new ApiLeadFilterViewModel
                {
                    Search = search,
                    Source = source,
                    Status = status,
                    Leads = leads
                };

            ViewData["Title"] =
                "API Leads";

            ViewData["Breadcrumb"] =
                "API Leads";

            return View(
                "~/Views/Lead/ApiLeads.cshtml",
                model);
        }
        //// =========================================================
        //// API Leads - For Sales Manager
        //// =========================================================
        //[HttpGet]
        //public async Task<IActionResult> ApiLeads(
        //    string? search,
        //    LeadSource? source,
        //    LeadStatus? status)
        //{
        //    var salesManagerId = _GetCurrentUserId();

        //    if (!salesManagerId.HasValue)
        //    {
        //        return RedirectToAction("Login", "Auth");
        //    }

        //    var leads =
        //        await _leadService
        //            .GetApiLeadsForSalesManagerAsync(
        //                salesManagerId.Value,
        //                search,
        //                source,
        //                status);

        //    ViewBag.Search = search;
        //    ViewBag.Source = source;
        //    ViewBag.Status = status;

        //    return View(leads);
        //}









        // =========================================================
        // GET: Assign Lead - Sales Manager
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AssignLead(long id)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model =
                await _leadService
                    .GetAssignLeadViewModelForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] =
                "Assign Lead";

            ViewData["Breadcrumb"] =
                "Assign Lead";

            return View(model);
        }

        // =========================================================
        // POST: Assign Lead - Sales Manager
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLead(
            AssignLeadViewModel model)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            if (!ModelState.IsValid)
            {
                var viewModel =
                    await _leadService
                        .GetAssignLeadViewModelForSalesManagerAsync(
                            model.LeadId,
                            salesManagerId.Value);

                if (viewModel == null)
                {
                    return NotFound();
                }

                model.LeadCode =
                    viewModel.LeadCode;

                model.LeadName =
                    viewModel.LeadName;

                model.SalesOfficers =
                    viewModel.SalesOfficers;

                return View(model);
            }

            var success =
                await _leadService
                    .AssignLeadForSalesManagerAsync(
                        model,
                        salesManagerId.Value);

            if (!success)
            {
                return Forbid();
            }

            TempData["Success"] =
                "Lead assigned successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }

        // ==========================
        // GET: Lead Details
        // ==========================
        [HttpGet]
        public async Task<IActionResult> LeadDetails(
            long id)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var lead =
                await _leadService
                    .GetLeadForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] =
                "Lead Details";

            ViewData["Breadcrumb"] =
                "Lead Details";

            return View(lead);
        }

        // ==========================
        // GET: Edit Lead
        // ==========================
        [HttpGet]
        public async Task<IActionResult> EditLead(long id)
        {
            var salesManagerId = _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model = await _leadService
                .GetLeadForEditForSalesManagerAsync(
                    id,
                    salesManagerId.Value);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // ==========================
        // POST: Edit Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(
    EditLeadViewModel model)
        {
            var salesManagerId = _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var updated = await _leadService
                .UpdateLeadForSalesManagerAsync(
                    model,
                    salesManagerId.Value);

            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Lead updated successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }



        // ==========================
        // GET: Reassign Lead
        // ==========================
        // ==========================
        // GET: Reassign Lead
        // ==========================
        [HttpGet]
        public async Task<IActionResult> ReassignLead(long id)
        {
            var userId =
                HttpContext.Session.GetString(
                    SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var salesManagerId =
                long.Parse(userId);

            var model =
                await _leadService
                    .GetReassignLeadViewModelForSalesManagerAsync(
                        id,
                        salesManagerId);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] =
                "Reassign Lead";

            ViewData["Breadcrumb"] =
                "Reassign Lead";

            return View(model);
        }

        // ==========================
        // POST: Reassign Lead
        // ==========================
        // ==========================
        // POST: Reassign Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignLead(
            ReassignLeadViewModel model)
        {
            var userId =
                HttpContext.Session.GetString(
                    SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var salesManagerId =
                long.Parse(userId);

            if (!ModelState.IsValid)
            {
                var viewModel =
                    await _leadService
                        .GetReassignLeadViewModelForSalesManagerAsync(
                            model.LeadId,
                            salesManagerId);

                if (viewModel == null)
                {
                    return NotFound();
                }

                model.SalesOfficers =
                    viewModel.SalesOfficers;

                return View(model);
            }

            await _leadService
                .ReassignLeadAsync(
                    model,
                    salesManagerId);

            TempData["Success"] =
                "Lead reassigned successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }

        // ==========================
        // GET: Create Manual Lead
        // ==========================
        [HttpGet]
        public IActionResult CreateLead()
        {
            ViewData["Title"] =
                "Create Lead";

            ViewData["Breadcrumb"] =
                "Create Lead";

            return View();
        }

        // ==========================
        // POST: Manual Lead Create
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLead(
            CreateLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService
                .CreateLeadAsync(model);

            TempData["Success"] =
                "Lead created successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }

        // ==========================
        // Generate Demo Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDemoLead()
        {
            var model =
                new AutoLeadCreateViewModel
                {
                    LeadName =
                        "Demo Customer",

                    CompanyName =
                        "Facebook Demo Ltd.",

                    Email =
                        "demo@example.com",

                    Phone =
                        "01712345678",

                    Profession =
                        "Business Owner",

                    Address =
                        "Dhaka",

                    Source =
                        LeadSource.Facebook,

                    Priority =
                        LeadPriority.Medium,

                    Description =
                        "This is a simulated Facebook Lead."
                };

            await _leadCaptureService
                .CaptureLeadAsync(
                    model,
                    LeadCaptureSource.FacebookLeadAds,
                    "FB-DEMO-001",
                    null);

            TempData["Success"] =
                "Demo Lead generated successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }

        /// =========================================================
        // POST: Archive Lead - Sales Manager
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(long id)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var success =
                await _leadService
                    .ArchiveLeadForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (!success)
            {
                return Forbid();
            }

            TempData["Success"] =
                "Lead archived successfully.";

            return RedirectToAction(
                nameof(LeadQueue));
        }

        // ==========================
        // Archived Leads
        // =========================
        [HttpGet]
        public async Task<IActionResult> ArchivedLeads()
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            ViewData["Title"] =
                "Archived Leads";

            ViewData["Breadcrumb"] =
                "Archived Leads";

            var leads =
                await _leadService
                    .GetArchivedLeadsForSalesManagerAsync(
                        salesManagerId.Value);

            return View(leads);
        }

        // =========================================================
        // ARCHIVED LEAD DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ArchivedLeadDetails(long id)
        {
            var salesManagerId = _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var lead =
                await _leadService
                    .GetArchivedLeadForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] =
                "Archived Lead Details";

            ViewData["Breadcrumb"] =
                "Archived Lead Details";

            return View(lead);
        }




        // ==========================
        // Restore Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(long id)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var success =
                await _leadService
                    .RestoreLeadForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (!success)
            {
                return Forbid();
            }

            TempData["Success"] =
                "Lead restored successfully.";

            return RedirectToAction(
                nameof(ArchivedLeads));
        }



        // =========================================================
        // Sales Officers
        // Sales Manager → Own Hierarchy Only
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SalesOfficers()
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            ViewData["Title"] =
                "Sales Officers";

            ViewData["Breadcrumb"] =
                "Sales Officers";

            var salesOfficers =
                await _salesOfficerServiceForSalesManager
                    .GetSalesOfficersAsync(
                        salesManagerId.Value);

            return View(salesOfficers);
        }


        // =====================================================
        // Sales Officer Performance
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "ADMIN,SALES_MANAGER")]
        public async Task<IActionResult> Performance(
            PerformanceFilterViewModel? filter)
        {
            filter ??=
                new PerformanceFilterViewModel();

            if (string.IsNullOrWhiteSpace(
                    filter.Range))
            {
                filter.Range =
                    "ThisMonth";
            }

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

                    filter.Range =
                        "ThisMonth";

                    filter.FromDate =
                        null;

                    filter.ToDate =
                        null;
                }
                else if (
                    filter.FromDate.Value.Date >
                    filter.ToDate.Value.Date)
                {
                    TempData["Error"] =
                        "From Date cannot be later than To Date.";

                    filter.Range =
                        "ThisMonth";

                    filter.FromDate =
                        null;

                    filter.ToDate =
                        null;
                }
            }

            var currentUserId =
                _GetCurrentUserId();

            if (!currentUserId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }


            // =====================================================
            // ADMIN
            // Global Performance
            // =====================================================

            if (User.IsInRole("ADMIN"))
            {
                var performance =
                    await _salesOfficerPerformanceService
                        .GetPerformanceAsync(filter);

                var topPerformer =
                    await _salesOfficerPerformanceService
                        .GetTopPerformerAsync(filter);

                var needsAttention =
                    await _salesOfficerPerformanceService
                        .GetNeedsAttentionAsync(filter);

                ViewData["PerformanceFilter"] =
                    filter;

                ViewData["TopPerformer"] =
                    topPerformer;

                ViewData["NeedsAttention"] =
                    needsAttention;

                return View(performance);
            }


            // =====================================================
            // SALES MANAGER
            // Own Hierarchy Only
            // =====================================================

            if (User.IsInRole("SALES_MANAGER"))
            {
                var performance =
                    await _salesOfficerPerformanceService
                        .GetPerformanceForSalesManagerAsync(
                            currentUserId.Value,
                            filter);

                var topPerformer =
                    await _salesOfficerPerformanceService
                        .GetTopPerformerForSalesManagerAsync(
                            currentUserId.Value,
                            filter);

                var needsAttention =
                    await _salesOfficerPerformanceService
                        .GetNeedsAttentionForSalesManagerAsync(
                            currentUserId.Value,
                            filter);

                ViewData["PerformanceFilter"] =
                    filter;

                ViewData["TopPerformer"] =
                    topPerformer;

                ViewData["NeedsAttention"] =
                    needsAttention;

                return View(performance);
            }


            return Forbid();
        }
        //// =====================================================
        //// Sales Officer Performance
        //// =====================================================
        //[HttpGet]
        //[Authorize(Roles = "ADMIN,SALES_MANAGER")]
        //public async Task<IActionResult> Performance(
        //    PerformanceFilterViewModel? filter)
        //{
        //    filter ??=
        //        new PerformanceFilterViewModel();

        //    if (string.IsNullOrWhiteSpace(
        //            filter.Range))
        //    {
        //        filter.Range =
        //            "ThisMonth";
        //    }

        //    if (string.Equals(
        //            filter.Range,
        //            "Custom",
        //            StringComparison.OrdinalIgnoreCase))
        //    {
        //        if (!filter.FromDate.HasValue ||
        //            !filter.ToDate.HasValue)
        //        {
        //            TempData["Error"] =
        //                "Please select both From Date and To Date.";

        //            filter.Range =
        //                "ThisMonth";

        //            filter.FromDate =
        //                null;

        //            filter.ToDate =
        //                null;
        //        }
        //        else if (
        //            filter.FromDate.Value.Date >
        //            filter.ToDate.Value.Date)
        //        {
        //            TempData["Error"] =
        //                "From Date cannot be later than To Date.";

        //            filter.Range =
        //                "ThisMonth";

        //            filter.FromDate =
        //                null;

        //            filter.ToDate =
        //                null;
        //        }
        //    }

        //    var performance =
        //        await _salesOfficerPerformanceService
        //            .GetPerformanceAsync(filter);

        //    var topPerformer =
        //        await _salesOfficerPerformanceService
        //            .GetTopPerformerAsync(filter);

        //    var needsAttention =
        //        await _salesOfficerPerformanceService
        //            .GetNeedsAttentionAsync(filter);

        //    ViewData["PerformanceFilter"] =
        //        filter;

        //    ViewData["TopPerformer"] =
        //        topPerformer;

        //    ViewData["NeedsAttention"] =
        //        needsAttention;

        //    return View(performance);
        //}





        //// =====================================================
        //// Settings
        //// =====================================================
        //[HttpGet]
        //public async Task<IActionResult> Settings()
        //{
        //    var settings =
        //        await _settingsService
        //            .GetSettingsAsync();

        //    var model =
        //        new AutoAssignmentSettingsViewModel
        //        {
        //            SettingId =
        //                settings.SettingId,

        //            AutoAssignmentEnabled =
        //                settings.AutoAssignmentEnabled
        //        };

        //    return View(model);
        //}

        //// =====================================================
        //// Save Settings
        //// =====================================================
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Settings(
        //    AutoAssignmentSettingsViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    await _settingsService
        //        .UpdateAutoAssignmentAsync(
        //            model.AutoAssignmentEnabled);

        //    TempData["Success"] =
        //        "Settings updated successfully.";

        //    return RedirectToAction(
        //        nameof(Settings));
        //}






        // =====================================================
        // Follow-ups
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FollowUps(
            FollowUpFilterViewModel filter)
        {
            ViewData["Title"] = "Follow-ups";
            ViewData["Breadcrumb"] = "Follow-ups";

            if (filter.FromDate.HasValue &&
                filter.ToDate.HasValue &&
                filter.FromDate.Value.Date >
                filter.ToDate.Value.Date)
            {
                TempData["Error"] =
                    "From Date cannot be later than To Date.";

                filter.FromDate = null;
                filter.ToDate = null;
            }

            var salesManagerId =
    _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model =
                await _followUpService
                    .GetFollowUpsForSalesManagerAsync(
                        filter,
                        salesManagerId.Value);

            return View(model);
        }

        // =====================================================
        // Follow-up Details
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> FollowUpDetails(
            long id)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model =
                await _followUpService
                    .GetFollowUpDetailsForSalesManagerAsync(
                        id,
                        salesManagerId.Value);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] =
                "Follow-up Details";

            ViewData["Breadcrumb"] =
                "Follow-up Details";

            return View(model);
        }

        // =====================================================
        // Unassigned Leads
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> UnassignedLeads(
            UnassignedLeadFilterViewModel filter)
        {
            ViewData["Title"] =
                "Assign Leads";

            ViewData["Breadcrumb"] =
                "Assign Leads";

            if (filter.FromDate.HasValue &&
                filter.ToDate.HasValue &&
                filter.FromDate.Value.Date >
                filter.ToDate.Value.Date)
            {
                TempData["Error"] =
                    "From Date cannot be later than To Date.";

                filter.FromDate = null;
                filter.ToDate = null;
            }

            var salesManagerId = _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            filter.Leads =
                await _leadService.GetUnassignedLeadsForSalesManagerAsync(
                    salesManagerId.Value,
                    filter.Search,
                    filter.Source,
                    filter.Priority,
                    filter.FromDate,
                    filter.ToDate);

            return View(filter);
        }

        // =====================================================
        // Reports
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            ViewData["Title"] =
                "Reports";

            ViewData["Breadcrumb"] =
                "Reports";

            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var report =
                await _reportService
                    .GetSalesManagerReportAsync(
                        salesManagerId.Value);

            return View(report);
        }



        // =====================================================
        // Sales Officer Details
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> SalesOfficerDetails(
            long id)
        {
            ViewData["Title"] =
                "Sales Officer Details";

            ViewData["Breadcrumb"] =
                "Sales Officer Details";

            var officer =
                await _salesOfficerServiceForSalesManager
                    .GetSalesOfficerDetailsAsync(id);

            if (officer == null)
            {
                return NotFound();
            }

            return View(officer);
        }

        // =====================================================
        // Team Lead Targets
        // =====================================================
        [HttpGet]
        [Authorize(Roles = RoleKeys.SalesManager)]
        public async Task<IActionResult> TeamLeadTargets()
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            ViewData["Title"] =
                "Team Lead Targets";

            ViewData["Breadcrumb"] =
                "Team Lead Targets";

            var targets =
                await _targetService
                    .GetTeamLeadTargetAchievementsAsync(
                        salesManagerId.Value);

            return View(targets);
        }

        // =====================================================
        // GET: Create Team Lead Target
        // =====================================================
        [HttpGet]
        [Authorize(Roles = RoleKeys.SalesManager)]
        public async Task<IActionResult> CreateTeamLeadTarget()
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            var model =
                await _targetService
                    .GetCreateTeamLeadTargetViewModelAsync(
                        salesManagerId.Value);

            ViewData["Title"] =
                "Create Team Lead Target";

            ViewData["Breadcrumb"] =
                "Create Team Lead Target";

            return View(model);
        }

        // =====================================================
        // POST: Create Team Lead Target
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleKeys.SalesManager)]
        public async Task<IActionResult> CreateTeamLeadTarget(
            CreateTargetViewModel model)
        {
            var salesManagerId =
                _GetCurrentUserId();

            if (!salesManagerId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            if (!ModelState.IsValid)
            {
                var viewModel =
                    await _targetService
                        .GetCreateTeamLeadTargetViewModelAsync(
                            salesManagerId.Value);

                model.Users =
                    viewModel.Users;

                ViewData["Title"] =
                    "Create Team Lead Target";

                ViewData["Breadcrumb"] =
                    "Create Team Lead Target";

                return View(model);
            }

            var result =
                await _targetService
                    .CreateTeamLeadTargetAsync(
                        model,
                        salesManagerId.Value);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                var viewModel =
                    await _targetService
                        .GetCreateTeamLeadTargetViewModelAsync(
                            salesManagerId.Value);

                model.Users =
                    viewModel.Users;

                ViewData["Title"] =
                    "Create Team Lead Target";

                ViewData["Breadcrumb"] =
                    "Create Team Lead Target";

                return View(model);
            }

            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(TeamLeadTargets));
        }

        // =====================================================
        // Performance Trend
        // =====================================================
        [HttpGet]
        [Authorize(Roles = "ADMIN,SALES_MANAGER")]
        public async Task<IActionResult> PerformanceTrend(
            PerformanceFilterViewModel? filter)
        {
            filter ??=
                new PerformanceFilterViewModel();

            if (string.IsNullOrWhiteSpace(
                    filter.Range))
            {
                filter.Range =
                    "ThisMonth";
            }

            var trend =
                await _salesOfficerPerformanceService
                    .GetPerformanceTrendAsync(filter);

            return Json(trend);
        }

        // =====================================================
        // SLA Trend
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> SLATrend(
            PerformanceFilterViewModel? filter = null)
        {
            var trend =
                await _salesOfficerPerformanceService
                    .GetSLATrendAsync(filter);

            return Json(trend);
        }

        // =====================================================
        // Completion Trend
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetCompletionTrend(
            PerformanceFilterViewModel? filter = null)
        {
            var trend =
                await _salesOfficerPerformanceService
                    .GetCompletionTrendAsync(filter);

            return Json(trend);
        }

        // =====================================================
        // Export Performance Excel
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> ExportPerformanceExcel(
            PerformanceFilterViewModel? filter = null)
        {
            var fileBytes =
                await _performanceExportService
                    .ExportPerformanceToExcelAsync(filter);

            var fileName =
                $"SalesOfficerPerformance_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            const string contentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(
                fileBytes,
                contentType,
                fileName);
        }

        // =====================================================
        // Export Performance PDF
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> ExportPerformancePdf(
            PerformanceFilterViewModel? filter = null)
        {
            var pdfBytes =
                await _performanceExportService
                    .ExportPerformanceToPdfAsync(filter);

            var fileName =
                $"SalesOfficerPerformance_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }

        // =====================================================
        // Current Logged-in User ID
        // =====================================================
        private long? _GetCurrentUserId()
        {
            var userId =
                HttpContext.Session.GetString(
                    SessionKeys.UserId);

            if (long.TryParse(
                    userId,
                    out var parsedUserId))
            {
                return parsedUserId;
            }

            var claimUserId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (long.TryParse(
                    claimUserId,
                    out parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }
    }
}

