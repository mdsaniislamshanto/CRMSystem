using CRMSystem.Constants;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesOfficerController : Controller
    {
        private readonly ILeadService _leadService;

        public SalesOfficerController(ILeadService leadService)
        {
            _leadService = leadService;
        }


        // GET: SalesOfficer/Dashboard
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Sales Officer Dashboard";
            ViewData["Breadcrumb"] = "Dashboard";

            return View();
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
    }
}