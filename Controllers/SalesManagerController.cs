using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesManagerController : Controller
    {
        private readonly ISalesManagerDashboardService _dashboardService;
        private readonly ILeadService _leadService;

        public SalesManagerController(
            ISalesManagerDashboardService dashboardService,
            ILeadService leadService)
        {
            _dashboardService = dashboardService;
            _leadService = leadService;
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
        // Archived Leads
        // ==========================
        public IActionResult ArchivedLeads()
        {
            return View();
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
        public IActionResult Settings()
        {
            return View();
        }
    }
}