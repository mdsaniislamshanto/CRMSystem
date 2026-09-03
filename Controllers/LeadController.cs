using CRMSystem.Constants;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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


        // =====================================================
        // GET: Lead/Index
        // Admin Lead Management
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            LeadStatus? status,
            LeadPriority? priority,
            LeadSource? source,
            string? slaStatus,
            string? sort,
            int page = 1)
        {
            ViewData["Title"] = "Lead Management";
            ViewData["Breadcrumb"] = "Leads";


            // =================================================
            // Get all active leads
            // =================================================

            var allLeads =
                await _leadService.GetAllLeadsAsync();


            // =================================================
            // Summary Statistics
            // =================================================

            ViewBag.TotalActiveLeads =
                allLeads.Count;

            ViewBag.NewLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.New);

            ViewBag.AssignedLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.Assigned);

            ViewBag.AcceptedLeads =
                allLeads.Count(x =>
                    x.Status == LeadStatus.Accepted);


            // =================================================
            // SLA Summary
            // =================================================

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


            // =================================================
            // Preserve Selected Filters
            // =================================================

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.Source = source;
            ViewBag.SLAStatus = slaStatus;
            ViewBag.Sort = sort;


            // =================================================
            // Start Filtering
            // =================================================

            IEnumerable<LeadViewModel> filteredLeads =
                allLeads;


            // =================================================
            // Search
            // Lead Code / Name / Company / Phone
            // =================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchValue =
                    search.Trim();

                filteredLeads =
                    filteredLeads.Where(lead =>
                        (
                            !string.IsNullOrWhiteSpace(
                                lead.LeadCode)
                            &&
                            lead.LeadCode.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase)
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(
                                lead.LeadName)
                            &&
                            lead.LeadName.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase)
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(
                                lead.CompanyName)
                            &&
                            lead.CompanyName.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase)
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(
                                lead.Phone)
                            &&
                            lead.Phone.Contains(
                                searchValue,
                                StringComparison.OrdinalIgnoreCase)
                        )
                    );
            }


            // =================================================
            // Status Filter
            // =================================================

            if (status.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Status == status.Value);
            }


            // =================================================
            // Priority Filter
            // =================================================

            if (priority.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Priority == priority.Value);
            }


            // =================================================
            // Source Filter
            // =================================================

            if (source.HasValue)
            {
                filteredLeads =
                    filteredLeads.Where(x =>
                        x.Source == source.Value);
            }


            // =================================================
            // Acceptance SLA Filter
            // =================================================

            if (!string.IsNullOrWhiteSpace(slaStatus))
            {
                switch (
                    slaStatus.Trim().ToLowerInvariant())
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


            // =================================================
            // Sorting
            // =================================================

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
                            .OrderByDescending(x =>
                                x.LeadName),

                    "priority" =>
                        filteredLeads
                            .OrderByDescending(x =>
                                x.Priority)
                            .ThenByDescending(x =>
                                x.LeadId),

                    "priority_low" =>
                        filteredLeads
                            .OrderBy(x =>
                                x.Priority)
                            .ThenByDescending(x =>
                                x.LeadId),

                    "status" =>
                        filteredLeads
                            .OrderBy(x =>
                                x.Status)
                            .ThenByDescending(x =>
                                x.LeadId),

                    "sla" =>
                        filteredLeads
                            .OrderByDescending(x =>
                                x.AcceptanceSLAMissed)
                            .ThenBy(x =>
                                x.AcceptanceSLAStatus)
                            .ThenByDescending(x =>
                                x.LeadId),

                    _ =>
                        filteredLeads
                            .OrderByDescending(x =>
                                x.LeadId)
                };


            // =================================================
            // Pagination
            // =================================================

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


            // =================================================
            // Pagination Data
            // =================================================

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


        // =====================================================
        // GET: Lead/Create
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =====================================================
        // GET: Lead/Details/{id}
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var lead =
                await _leadService.GetLeadByIdAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Lead Details";
            ViewData["Breadcrumb"] = "Lead Details";

            return View(lead);
        }


        // =====================================================
        // GET: Lead/ArchivedDetails/{id}
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ArchivedDetails(long id)
        {
            var lead =
                await _leadService.GetArchivedLeadByIdAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Archived Lead Details";
            ViewData["Breadcrumb"] = "Archived Lead Details";

            return View("ArchivedDetails", lead);
        }


        // =====================================================
        // POST: Lead/Create
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService.CreateLeadAsync(model);

            TempData["Success"] =
                "Lead created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // GET: Lead/Edit/{id}
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var lead =
                await _leadService.GetLeadForEditAsync(id);

            if (lead == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Lead";
            ViewData["Breadcrumb"] = "Edit Lead";

            return View(lead);
        }


        // =====================================================
        // POST: Lead/Edit
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leadService.UpdateLeadAsync(model);

            TempData["Success"] =
                "Lead updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: Lead/Archive
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(long id)
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!long.TryParse(
                    userIdClaim,
                    out long archivedBy))
            {
                TempData["Error"] =
                    "Unable to identify the current user.";

                return RedirectToAction(nameof(Index));
            }

            await _leadService.ArchiveLeadAsync(
                id,
                archivedBy);

            TempData["Success"] =
                "Lead archived successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // GET: Lead/Archived
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Archived()
        {
            ViewData["Title"] = "Archived Leads";
            ViewData["Breadcrumb"] = "Archived Leads";

            var leads =
                await _leadService.GetArchivedLeadsAsync();

            return View(leads);
        }


        // =====================================================
        // POST: Lead/Restore
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(long id)
        {
            await _leadService.RestoreLeadAsync(id);

            TempData["Success"] =
                "Lead restored successfully.";

            return RedirectToAction(nameof(Archived));
        }


        // =====================================================
        // GET: Lead/Assign/{id}
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Assign(long id)
        {
            var model =
                await _leadService
                    .GetAssignLeadViewModelAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }


        // =====================================================
        // POST: Lead/Assign
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            AssignLeadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var viewModel =
                    await _leadService
                        .GetAssignLeadViewModelAsync(
                            model.LeadId);

                if (viewModel == null)
                {
                    return NotFound();
                }

                model.SalesOfficers =
                    viewModel.SalesOfficers;

                return View(model);
            }

            var userId =
                HttpContext.Session.GetString(
                    SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Auth");
            }

            await _leadService.AssignLeadAsync(
                model,
                long.Parse(userId));

            TempData["Success"] =
                "Lead assigned successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // POST: Generate Demo Lead
        // =====================================================

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

            await _leadCaptureService.CaptureLeadAsync(
                model,
                LeadCaptureSource.FacebookLeadAds,
                "FB-DEMO-001",
                null);

            TempData["Success"] =
                "Demo Lead generated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // GET: API Leads
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ApiLeads(
            string? search,
            LeadSource? source,
            LeadStatus? status)
        {
            var leads =
                await _leadService.GetApiLeadsAsync(
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

            return View(model);
        }


        // =====================================================
        // GET: Unassigned Leads
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Unassigned()
        {
            ViewData["Title"] = "Assign Lead";
            ViewData["Breadcrumb"] = "Assign Lead";

            var leads =
                await _leadService
                    .GetUnassignedLeadsAsync();

            return View(leads);
        }
    }
}