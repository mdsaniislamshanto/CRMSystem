using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesManagerController : Controller
    {
        // Dashboard
        public IActionResult Index()
        {
            return View();
        }

        // Lead Queue
        public IActionResult LeadQueue()
        {
            return View();
        }

        // Assign Leads
        public IActionResult AssignLead()
        {
            return View();
        }

        // Archived Leads
        public IActionResult ArchivedLeads()
        {
            return View();
        }

        // Sales Officers
        public IActionResult SalesOfficers()
        {
            return View();
        }

        // Performance
        public IActionResult Performance()
        {
            return View();
        }

        // Reports
        public IActionResult Reports()
        {
            return View();
        }

        // Settings
        public IActionResult Settings()
        {
            return View();
        }
    }
}