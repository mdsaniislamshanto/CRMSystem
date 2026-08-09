using CRMSystem.Services.Interfaces;
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
                return BadRequest("Google authorization code was not received.");
            }

            var success =
                await _googleFormsService.HandleCallbackAsync(code);

            if (!success)
            {
                return BadRequest("Google Forms authorization failed.");
            }

            return Content(
                "Google Forms connected successfully.");
        }
    }
}