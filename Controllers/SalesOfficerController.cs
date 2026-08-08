using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesOfficerController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ISalesOfficerDashboardService _dashboardService;
        private readonly ILeadFeedbackService _leadFeedbackService; 

        public SalesOfficerController(ILeadService leadService, ISalesOfficerDashboardService dashboardService, ILeadFeedbackService leadFeedbackService)
        {
            _leadService = leadService;
            _dashboardService = dashboardService;
            _leadFeedbackService = leadFeedbackService;
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


        // ==========================
        // GET: Submit Feedback
        // ==========================

        [HttpGet]
        public IActionResult SubmitFeedback(long assignmentId)
        {
            ViewData["Title"] = "Submit Feedback";
            ViewData["Breadcrumb"] = "Submit Feedback";

            var model = new SubmitFeedbackViewModel
            {
                AssignmentId = assignmentId
            };

            return View(model);
        }

        // ==========================
        // POST: Submit Feedback
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(SubmitFeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                await _leadFeedbackService.SubmitFeedbackAsync(
                    model,
                    long.Parse(userId));

                TempData["Success"] = "Feedback submitted successfully.";

                return RedirectToAction(nameof(MyAssignedLeads));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                return View(model);
            }
        }

        // GET: SalesOfficer/FeedbackHistory
        public async Task<IActionResult> FeedbackHistory()
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewData["Title"] = "Feedback History";
            ViewData["Breadcrumb"] = "Feedback History";

            var feedbacks =
                await _leadFeedbackService.GetFeedbackHistoryAsync(
                    long.Parse(userId));

            return View(feedbacks);
        }


            // GET: SalesOfficer/FeedbackDetails
        public async Task<IActionResult> FeedbackDetails(long feedbackId)
        {
            var userId = HttpContext.Session.GetString(SessionKeys.UserId);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewData["Title"] = "Feedback Details";
            ViewData["Breadcrumb"] = "Feedback Details";

            var feedback = await _leadFeedbackService.GetFeedbackDetailsAsync(
                feedbackId,
                long.Parse(userId));

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }
    }
}