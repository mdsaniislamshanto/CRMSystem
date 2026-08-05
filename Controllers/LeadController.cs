using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class LeadController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ILeadCaptureService _leadCaptureService;
        private readonly IAssignmentService _assignmentService;

        public LeadController(
               ILeadService leadService,
               ILeadCaptureService leadCaptureService,
               IAssignmentService assignmentService)
        {
            _leadService = leadService;
            _leadCaptureService = leadCaptureService;
            _assignmentService = assignmentService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Lead Management";
            ViewData["Breadcrumb"] = "Leads";

            var leads = await _leadService.GetAllLeadsAsync();
            return View(leads);
        }


        // GET: Lead/Create
        public IActionResult Create()
        {
            return View();
        }



        // GET: Lead/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var lead = await _leadService.GetLeadByIdAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Lead Details";
            ViewData["Breadcrumb"] = "Lead Details";

            return View(lead);
        }


        // POST: Lead/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _leadService.CreateLeadAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // GET: Lead/Edit
        public async Task<IActionResult> Edit(long id)
        {
            var lead = await _leadService.GetLeadForEditAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Lead";
            ViewData["Breadcrumb"] = "Edit Lead";

            return View(lead);
        }

        // POST: Lead/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService.UpdateLeadAsync(model);

            TempData["Success"] = "Lead updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Lead/Archive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(long id)
        {
            await _leadService.ArchiveLeadAsync(id);

            TempData["Success"] = "Lead archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Lead/Archived
        public async Task<IActionResult> Archived()
        {
            ViewData["Title"] = "Archived Leads";
            ViewData["Breadcrumb"] = "Archived Leads";

            var leads = await _leadService.GetArchivedLeadsAsync();

            return View(leads);
        }

        // POST: Lead/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(long id)
        {
            await _leadService.RestoreLeadAsync(id);

            TempData["Success"] = "Lead restored successfully.";

            return RedirectToAction(nameof(Archived));
        }


        // GET: Lead/Assign
        [HttpGet]
        public async Task<IActionResult> Assign(long id)
        {
            var model = await _leadService.GetAssignLeadViewModelAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }


        // POST: Lead/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignLeadViewModel model)
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

            return RedirectToAction(nameof(Index));


        }


        // Demo Auto Lead Generation
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

            return RedirectToAction(nameof(Index));
        }
    }
}