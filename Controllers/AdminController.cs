using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Services.Interfaces;
using CRMSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CRMSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            IAuthService authService,
            ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Dashboard";
            var model = new AdminDashboardViewModel
            {
                NewLeads = _context.Leads.Count(l => l.Status == LeadStatus.New),

                AssignedLeads = _context.Leads.Count(l => l.Status == LeadStatus.Assigned),

                AcceptedLeads = _context.Leads.Count(l => l.Status == LeadStatus.Accepted),

                InProgressLeads = _context.Leads.Count(l => l.Status == LeadStatus.InProgress),

                CompletedLeads = _context.Leads.Count(l => l.Status == LeadStatus.Completed),

                RejectedLeads = _context.Leads.Count(l => l.Status == LeadStatus.Rejected)
            };

            return View(model);
        }
    }
}