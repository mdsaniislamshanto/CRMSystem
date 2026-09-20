using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AiAnalysisController : Controller
    {
        private readonly IGeminiFollowUpAnalysisService _geminiService;

        public AiAnalysisController(IGeminiFollowUpAnalysisService geminiService)
        {
            _geminiService = geminiService;
        }

        /// <summary>
        /// Analyzes or re-analyzes a feedback follow-up record with Gemini API
        /// </summary>
        [HttpPost("analyze/{feedbackId}")]
        public async Task<IActionResult> Analyze(long feedbackId, [FromQuery] bool force = false)
        {
            if (feedbackId <= 0)
            {
                return BadRequest(new { isSuccess = false, errorMessage = "Invalid feedback ID." });
            }

            var result = await _geminiService.AnalyzeFeedbackAsync(feedbackId, forceReanalysis: force);

            if (!result.IsSuccess)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the existing AI analysis or computes it on-demand if not yet analyzed
        /// </summary>
        [HttpGet("details/{feedbackId}")]
        public async Task<IActionResult> GetDetails(long feedbackId)
        {
            if (feedbackId <= 0)
            {
                return BadRequest(new { isSuccess = false, errorMessage = "Invalid feedback ID." });
            }

            var result = await _geminiService.GetFeedbackAnalysisAsync(feedbackId);

            if (result == null)
            {
                // Compute on demand
                result = await _geminiService.AnalyzeFeedbackAsync(feedbackId, forceReanalysis: false);
            }

            return Ok(result);
        }

        /// <summary>
        /// Analyzes or re-analyzes a Lead across all its follow-ups with Gemini API
        /// </summary>
        [HttpPost("analyze-lead/{leadId}")]
        public async Task<IActionResult> AnalyzeLead(long leadId, [FromQuery] bool force = false)
        {
            if (leadId <= 0)
            {
                return BadRequest(new { isSuccess = false, errorMessage = "Invalid lead ID." });
            }

            var result = await _geminiService.AnalyzeLeadAsync(leadId, forceReanalysis: force);

            if (!result.IsSuccess)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the existing AI Lead analysis or computes it on-demand
        /// </summary>
        [HttpGet("lead-details/{leadId}")]
        public async Task<IActionResult> GetLeadDetails(long leadId)
        {
            if (leadId <= 0)
            {
                return BadRequest(new { isSuccess = false, errorMessage = "Invalid lead ID." });
            }

            var result = await _geminiService.GetLeadAnalysisAsync(leadId);

            if (result == null)
            {
                result = await _geminiService.AnalyzeLeadAsync(leadId, forceReanalysis: false);
            }

            return Ok(result);
        }
    }
}
