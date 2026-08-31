using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



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

        public SalesManagerController(
            ISalesManagerDashboardService dashboardService,
            ILeadService leadService,
            ISettingsService settingsService,
            ILeadCaptureService leadCaptureService,
            IReportService reportService,
          ISalesOfficerServiceForSalesManager salesOfficerServiceForSalesManager, ISalesOfficerPerformanceService salesOfficerPerformanceService, IPerformanceExportService performanceExportService)

        {
            _dashboardService = dashboardService;
            _leadService = leadService;
            _settingsService = settingsService;
            _leadCaptureService = leadCaptureService;
            _reportService = reportService;
            _salesOfficerServiceForSalesManager = salesOfficerServiceForSalesManager;
            _salesOfficerPerformanceService = salesOfficerPerformanceService;
            _performanceExportService = performanceExportService;
        }

        // ==========================
        // Dashboard
        // ==========================
        public async Task<IActionResult> Index()
        {
            var model = await _dashboardService.GetDashboardAsync();

            return View(model);
        }

        // ==========================
        // Lead Queue
        // ==========================
        public async Task<IActionResult> LeadQueue()
        {
            ViewData["Title"] = "Lead Queue";

            var leads = await _leadService.GetAllLeadsAsync();

            return View(leads);
        }


        // ==========================
        // GET: Assign Lead
        // ==========================
        [HttpGet]
        public async Task<IActionResult> AssignLead(long id)
        {
            var model = await _leadService.GetAssignLeadViewModelAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }


        // ==========================
        // POST: Assign Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLead(AssignLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _leadService.GetAssignLeadViewModelAsync(model.LeadId);

                if (viewModel == null)
                {
                    return NotFound();
                }

                model.SalesOfficers = viewModel.SalesOfficers;

                return View(model);
            }

            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await _leadService.AssignLeadAsync(model, long.Parse(userId));

            TempData["Success"] = "Lead assigned successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }


        // ==========================
        // GET: Lead Details
        // ==========================
        [HttpGet]
        public async Task<IActionResult> LeadDetails(long id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Lead Details";

            return View(lead);
        }


        // ==========================
        // GET: Edit Lead
        // ==========================
        [HttpGet]
        public async Task<IActionResult> EditLead(long id)
        {
            var model = await _leadService.GetLeadForEditAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Lead";

            return View(model);
        }

        // ==========================
        // POST: Edit Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(EditLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService.UpdateLeadAsync(model);

            TempData["Success"] = "Lead updated successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }


        // ==========================
        // GET: Reassign Lead
        // ==========================
        [HttpGet]
        public async Task<IActionResult> ReassignLead(long id)
        {
            var model = await _leadService.GetReassignLeadViewModelAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Reassign Lead";

            return View(model);
        }

        // ==========================
        // POST: Reassign Lead
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignLead(ReassignLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = await _leadService.GetReassignLeadViewModelAsync(model.LeadId);

                if (viewModel == null)
                {
                    return NotFound();
                }

                model.SalesOfficers = viewModel.SalesOfficers;

                return View(model);
            }

            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await _leadService.ReassignLeadAsync(
                model,
                long.Parse(userId));

            TempData["Success"] = "Lead reassigned successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }



        // ==========================
        // GET: Create Manual  Lead
        // ==========================

        [HttpGet]
        public IActionResult CreateLead()
        {
            ViewData["Title"] = "Create Lead";
            ViewData["Breadcrumb"] = "Create Lead";

            return View();
        }
        // ==========================
        // POST:  Manual Lead Create 
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLead(CreateLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService.CreateLeadAsync(model);

            TempData["Success"] = "Lead created successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }


        // ==========================
        // Generate Demo Lead
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDemoLead()
        {
            var model = new AutoLeadCreateViewModel
            {
                LeadName = "Demo Customer",
                CompanyName = "Facebook Demo Ltd.",
                Email = "demo@example.com",
                Phone = "01712345678",
                Profession = "Business Owner",
                Address = "Dhaka",
                Source = LeadSource.Facebook,
                Priority = LeadPriority.Medium,
                Description = "This is a simulated Facebook Lead."
            };

            await _leadCaptureService.CaptureLeadAsync(
                model,
                LeadCaptureSource.FacebookLeadAds,
                "FB-DEMO-001",
                null);

            TempData["Success"] = "Demo Lead generated successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }




        // ==========================
        // Archive Lead
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveLead(long id)
        {
            await _leadService.ArchiveLeadAsync(id);

            TempData["Success"] = "Lead archived successfully.";

            return RedirectToAction(nameof(LeadQueue));
        }

        // ==========================
        // Archived Leads
        // ==========================

        [HttpGet]
        public async Task<IActionResult> ArchivedLeads()
        {
            ViewData["Title"] = "Archived Leads";
            ViewData["Breadcrumb"] = "Archived Leads";

            var leads = await _leadService.GetArchivedLeadsAsync();

            return View(leads);
        }

        // ==========================
        // Restore Lead
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreLead(long id)
        {
            await _leadService.RestoreLeadAsync(id);

            TempData["Success"] = "Lead restored successfully.";

            return RedirectToAction(nameof(ArchivedLeads));
        }

        // ==========================
        // GET: SalesOfficers
        // ==========================
        [HttpGet]
        public async Task<IActionResult> SalesOfficers()
        {
            ViewData["Title"] = "Sales Officers";
            ViewData["Breadcrumb"] = "Sales Officers";

            var salesOfficers =
                await _salesOfficerServiceForSalesManager.GetSalesOfficersAsync();

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
            // -------------------------------------------------
            // Create default filter if no filter was supplied
            // -------------------------------------------------

            filter ??=
                new PerformanceFilterViewModel();


            // -------------------------------------------------
            // Default Range
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(filter.Range))
            {
                filter.Range =
                    "ThisMonth";
            }


            // -------------------------------------------------
            // Validate Custom Range
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


            // -------------------------------------------------
            // Get Filtered Performance
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
            // Get Needs Attention Officer
            // -------------------------------------------------

            var needsAttention =
                await _salesOfficerPerformanceService
                    .GetNeedsAttentionAsync(filter);


            // -------------------------------------------------
            // Send Filter To View
            // -------------------------------------------------

            ViewData["PerformanceFilter"] =
                filter;


            // -------------------------------------------------
            // Send Top Performer To View
            // -------------------------------------------------

            ViewData["TopPerformer"] =
                topPerformer;


            // -------------------------------------------------
            // Send Needs Attention To View
            // -------------------------------------------------

            ViewData["NeedsAttention"] =
                needsAttention;


            // -------------------------------------------------
            // Return Existing Performance List
            // -------------------------------------------------

            return View(performance);
        }






        // ==========================
        // Settings
        // ==========================
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _settingsService.GetSettingsAsync();

            var model = new AutoAssignmentSettingsViewModel
            {
                SettingId = settings.SettingId,
                AutoAssignmentEnabled = settings.AutoAssignmentEnabled
            };

            return View(model);
        }

        // ==========================
        // Save Settings
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(
            AutoAssignmentSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _settingsService.UpdateAutoAssignmentAsync(
                model.AutoAssignmentEnabled);

            TempData["Success"] =
                "Settings updated successfully.";

            return RedirectToAction(nameof(Settings));
        }


        // ==========================
        // Follow-ups
        // ==========================
        [HttpGet]
        public async Task<IActionResult> FollowUps()
        {
            ViewData["Title"] = "Follow-ups";
            ViewData["Breadcrumb"] = "Follow-ups";

            var followUps =
                await _dashboardService.GetFollowUpsAsync();

            return View(followUps);
        }



        // ==========================
        // Follow-up Details
        // ==========================

        [HttpGet]
        public async Task<IActionResult> FollowUpDetails(long id)
        {
            var model =
                await _dashboardService.GetFollowUpDetailsAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Follow-up Details";
            ViewData["Breadcrumb"] = "Follow-up Details";

            return View(model);
        }



        // GET: SalesManager/UnassignedLeads
        public async Task<IActionResult> UnassignedLeads()
        {
            ViewData["Title"] = "Assign Leads";
            ViewData["Breadcrumb"] = "Assign Leads";

            var leads = await _leadService.GetUnassignedLeadsAsync();

            return View(leads);
        }


        // ==========================
        // Reports
        // ==========================

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            ViewData["Title"] = "Reports";
            ViewData["Breadcrumb"] = "Reports";

            var report =
                await _reportService
                    .GetSalesManagerReportAsync();

            return View(report);
        }

        // ==========================
        // Sales Officer Details
        // ==========================

        [HttpGet]
        public async Task<IActionResult> SalesOfficerDetails(long id)
        {
            ViewData["Title"] = "Sales Officer Details";
            ViewData["Breadcrumb"] = "Sales Officer Details";

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
        // Performance Trend
        // =====================================================

        [HttpGet]
        [Authorize(Roles = "ADMIN,SALES_MANAGER")]
        public async Task<IActionResult> PerformanceTrend(
            PerformanceFilterViewModel? filter)
        {
            filter ??=
                new PerformanceFilterViewModel();


            // -------------------------------------------------
            // Default Range
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(filter.Range))
            {
                filter.Range =
                    "ThisMonth";
            }


            // -------------------------------------------------
            // Get Trend
            // -------------------------------------------------

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
        // Export Performance Report To Excel
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
        // Export Performance Report To PDF
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


    }
}