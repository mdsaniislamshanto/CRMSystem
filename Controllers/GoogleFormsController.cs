using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CRMSystem.Controllers
{
    public class GoogleFormsController : Controller
    {
        private readonly IGoogleFormsService _googleFormsService;
        private readonly ILeadService _leadService;

        public GoogleFormsController(
            IGoogleFormsService googleFormsService,
            ILeadService leadService)
        {
            _googleFormsService = googleFormsService;
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<IActionResult> Connect()
        {
            var authorizationUrl =
                await _googleFormsService.GetAuthorizationUrlAsync();

            return Redirect(authorizationUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(
                    "Google authorization code was not received.");
            }

            var success =
                await _googleFormsService.HandleCallbackAsync(code);

            if (!success)
            {
                return BadRequest(
                    "Google Forms authorization failed.");
            }

            return Content(
                "Google Forms connected successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> TestResponses()
        {
            try
            {
                var responses =
                    await _googleFormsService.GetResponsesAsync();

                return Json(responses);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestFormItems()
        {
            var items = await _googleFormsService.GetFormItemsAsync();

            var result = items
                .Where(x => x.QuestionItem?.Question != null)
                .Select(x => new
                {
                    ItemId = x.ItemId,
                    Title = x.Title,
                    QuestionId = x.QuestionItem!.Question!.QuestionId
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> TestLeadCandidates()
        {
            var leads =
                await _googleFormsService.GetLeadCandidatesAsync();

            return Json(leads);
        }



        [HttpGet]
        [Authorize(Roles = "ADMIN,SALES_MANAGER")]    
        public async Task<IActionResult> Import()
        {
            try
            {
                var candidates =
                    await _googleFormsService.GetLeadCandidatesAsync();

                var newLeads = candidates
                    .Count(x => !x.IsAlreadyImported);

                var duplicateLeads = candidates
                    .Count(x => x.IsAlreadyImported);

                var model = new GoogleFormImportViewModel
                {
                    Candidates = candidates,

                    TotalCandidates = candidates.Count,

                    NewLeads = newLeads,

                    DuplicateLeads = duplicateLeads,

                    FailedLeads = 0,

                    ImportCompleted = false
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return View(
                    new GoogleFormImportViewModel());
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN,SALES_MANAGER")]
        public async Task<IActionResult> ImportLeads()
        {
            try
            {
                var candidates =
                    await _googleFormsService.GetLeadCandidatesAsync();

                var newCandidates = candidates
                    .Where(x => !x.IsAlreadyImported)
                    .ToList();

                var duplicateCount = candidates
                    .Count(x => x.IsAlreadyImported);

                var newLeadCount = 0;
                var failedCount = 0;

                foreach (var candidate in newCandidates)
                {
                    try
                    {
                        await _leadService
                            .CreateLeadFromCaptureAsync(candidate);

                        newLeadCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                }

                TempData["Success"] =
                    $"Google Form import completed successfully. " +
                    $"New Leads: {newLeadCount}, " +
                    $"Already Imported: {duplicateCount}, " +
                    $"Failed: {failedCount}.";

                return RedirectToAction(nameof(Import));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Google Form import failed: {ex.Message}";

                return RedirectToAction(nameof(Import));
            }
        }

    }
}