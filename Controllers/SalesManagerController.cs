using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;



namespace CRMSystem.Controllers
{
    public class SalesManagerController : Controller
    {
        private readonly ISalesManagerDashboardService _dashboardService;
        private readonly ILeadService _leadService;
        private readonly ISettingsService _settingsService;
        private readonly ILeadCaptureService _leadCaptureService;

        public SalesManagerController(
            ISalesManagerDashboardService dashboardService,
            ILeadService leadService,
            ISettingsService settingsService,
            ILeadCaptureService leadCaptureService)
        {
            _dashboardService = dashboardService;
            _leadService = leadService;
            _settingsService = settingsService;
            _leadCaptureService = leadCaptureService;
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
        // Sales Officers
        // ==========================
        public IActionResult SalesOfficers()
        {
            return View();
        }

        // ==========================
        // Performance
        // ==========================
        public IActionResult Performance()
        {
            return View();
        }

        // ==========================
        // Reports
        // ==========================
        public IActionResult Reports()
        {
            return View();
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
    }
}