using CRMSystem.Constants;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesOfficerController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ISalesOfficerDashboardService _dashboardService;

        public SalesOfficerController(ILeadService leadService, ISalesOfficerDashboardService dashboardService)
        {
            _leadService = leadService;
            _dashboardService = dashboardService;
        }


        // GET: SalesOfficer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewData["Title"] = "Sales Officer Dashboard";
            ViewData["Breadcrumb"] = "Dashboard";

            var dashboard = await _dashboardService.GetDashboardAsync(long.Parse(userId));

            return View(dashboard);
        }



        // GET: SalesOfficer/MyAssignedLeads
        public async Task<IActionResult> MyAssignedLeads()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewData["Title"] = "My Assigned Leads";
            ViewData["Breadcrumb"] = "My Assigned Leads";

            var leads = await _leadService.GetAssignedLeadsAsync(long.Parse(userId));

            return View(leads);
        }

        // ==========================
        // Accept Lead
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptLead(long assignmentId)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await _leadService.AcceptLeadAsync(
                assignmentId,
                long.Parse(userId));

            TempData["Success"] = "Lead accepted successfully.";

            return RedirectToAction(nameof(MyAssignedLeads));
        }
    }
}