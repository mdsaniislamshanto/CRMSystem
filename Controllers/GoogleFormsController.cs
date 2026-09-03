using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class GoogleFormsController : Controller
    {
        private readonly IGoogleFormsService _googleFormsService;

        public GoogleFormsController(
            IGoogleFormsService googleFormsService)
        {
            _googleFormsService = googleFormsService;
        }

        // =====================================================
        // Google Forms OAuth Connection
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Connect()
        {
            var authorizationUrl =
                await _googleFormsService.GetAuthorizationUrlAsync();

            return Redirect(authorizationUrl);
        }


        // =====================================================
        // Google Forms OAuth Callback
        // =====================================================

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


        // =====================================================
        // Test Google Form Responses
        // Development / Testing Only
        // =====================================================

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


        // =====================================================
        // Test Google Form Items
        // Development / Testing Only
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> TestFormItems()
        {
            var items =
                await _googleFormsService.GetFormItemsAsync();

            var result = items
                .Where(x =>
                    x.QuestionItem?.Question != null)
                .Select(x => new
                {
                    ItemId = x.ItemId,
                    Title = x.Title,
                    QuestionId =
                        x.QuestionItem!
                            .Question!
                            .QuestionId
                })
                .ToList();

            return Json(result);
        }


        // =====================================================
        // Test Automatic Lead Candidates
        // Development / Testing Only
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> TestLeadCandidates()
        {
            var leads =
                await _googleFormsService
                    .GetLeadCandidatesAsync();

            return Json(leads);
        }
    }
}