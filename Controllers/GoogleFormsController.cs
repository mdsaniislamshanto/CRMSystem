using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> ImportLeads()
        {
            try
            {
                var candidates =
                    await _googleFormsService.GetLeadCandidatesAsync();

                var createdLeadIds = new List<long>();

                foreach (var candidate in candidates)
                {
                    var leadId =
                        await _leadService
                            .CreateLeadFromCaptureAsync(candidate);

                    createdLeadIds.Add(leadId);
                }

                return Json(new
                {
                    success = true,
                    message = "Google Form leads imported successfully.",
                    totalCandidates = candidates.Count,
                    createdLeads = createdLeadIds.Count,
                    leadIds = createdLeadIds
                });
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

    }
}