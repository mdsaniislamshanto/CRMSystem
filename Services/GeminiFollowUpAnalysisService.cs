using CRMSystem.Configurations;
using CRMSystem.Data;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CRMSystem.Services
{
    public class GeminiFollowUpAnalysisService : IGeminiFollowUpAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiSettings _geminiSettings;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<GeminiFollowUpAnalysisService> _logger;

        public GeminiFollowUpAnalysisService(
            ApplicationDbContext context,
            IOptions<GeminiSettings> geminiSettings,
            HttpClient httpClient,
            IWebHostEnvironment webHostEnvironment,
            ILogger<GeminiFollowUpAnalysisService> logger)
        {
            _context = context;
            _geminiSettings = geminiSettings.Value;
            _httpClient = httpClient;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<GeminiAiAnalysisResultViewModel?> GetFeedbackAnalysisAsync(long feedbackId)
        {
            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

            if (feedback == null || !feedback.AiAnalyzedAt.HasValue)
            {
                return null;
            }

            return new GeminiAiAnalysisResultViewModel
            {
                FeedbackId = feedback.FeedbackId,
                IsCustomerInterested = feedback.AiCustomerInterested ?? false,
                BuyingIntent = feedback.AiBuyingIntent ?? "Moderate",
                Sentiment = feedback.AiSentiment ?? "Neutral",
                SiteVisitInterested = feedback.AiSiteVisitInterested ?? false,
                AiSummary = feedback.AiSummary ?? string.Empty,
                AnalyzedAt = feedback.AiAnalyzedAt,
                HasImage = !string.IsNullOrWhiteSpace(feedback.ProofImage),
                ProofImageUrl = feedback.ProofImage,
                IsSuccess = true
            };
        }

        public async Task<GeminiAiAnalysisResultViewModel> AnalyzeFeedbackAsync(long feedbackId, bool forceReanalysis = false)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la!.Lead)
                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la!.SalesOfficer)
                    .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

                if (feedback == null)
                {
                    return new GeminiAiAnalysisResultViewModel
                    {
                        FeedbackId = feedbackId,
                        IsSuccess = false,
                        ErrorMessage = "Feedback record not found."
                    };
                }

                // If already analyzed and force reanalysis is not requested, return saved result
                if (!forceReanalysis && feedback.AiAnalyzedAt.HasValue)
                {
                    return new GeminiAiAnalysisResultViewModel
                    {
                        FeedbackId = feedback.FeedbackId,
                        IsCustomerInterested = feedback.AiCustomerInterested ?? false,
                        BuyingIntent = feedback.AiBuyingIntent ?? "Moderate",
                        Sentiment = feedback.AiSentiment ?? "Neutral",
                        SiteVisitInterested = feedback.AiSiteVisitInterested ?? false,
                        AiSummary = feedback.AiSummary ?? string.Empty,
                        AnalyzedAt = feedback.AiAnalyzedAt,
                        HasImage = !string.IsNullOrWhiteSpace(feedback.ProofImage),
                        ProofImageUrl = feedback.ProofImage,
                        IsSuccess = true
                    };
                }

                var apiKey = _geminiSettings.ApiKey?.Trim();
                var model = string.IsNullOrWhiteSpace(_geminiSettings.Model) ? "gemini-3.5-flash-lite" : _geminiSettings.Model.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new GeminiAiAnalysisResultViewModel
                    {
                        FeedbackId = feedbackId,
                        IsSuccess = false,
                        ErrorMessage = "Gemini API key is not configured in appsettings."
                    };
                }

                var leadName = feedback.LeadAssignment?.Lead?.LeadName ?? "Customer";
                var companyName = feedback.LeadAssignment?.Lead?.CompanyName ?? "Unknown Company";
                var officerName = feedback.LeadAssignment?.SalesOfficer?.FullName ?? "Sales Officer";
                var feedbackStatus = feedback.Status.ToString();
                var summary = feedback.Summary ?? string.Empty;
                var notes = string.IsNullOrWhiteSpace(feedback.Notes) ? "None" : feedback.Notes;

                var promptText = $@"You are an expert CRM sales intelligence analyst.
Analyze the following sales officer followup details and any attached proof image:

Customer / Lead Name: {leadName}
Company: {companyName}
Sales Officer: {officerName}
Follow-up Status: {feedbackStatus}
Follow-up Summary: {summary}
Notes: {notes}

Based on the conversation summary, notes, and visual proof image (if attached), evaluate:
1. isCustomerInterested: (boolean) Is the customer genuinely interested in proceeding?
2. buyingIntent: (string) Likelihood to purchase - strictly one of: 'High', 'Moderate', 'Low'
3. sentiment: (string) Overall customer sentiment - strictly one of: 'Positive', 'Neutral', 'Negative'
4. siteVisitInterested: (boolean) Is the customer interested in or planning a site visit / project visit?
5. aiSummary: (string) A concise 1-2 sentence executive summary explaining the assessment (mention observations from the proof image if provided).

Return ONLY valid JSON matching this schema:
{{
  ""isCustomerInterested"": true,
  ""buyingIntent"": ""High"",
  ""sentiment"": ""Positive"",
  ""siteVisitInterested"": true,
  ""aiSummary"": ""...""
}}";

                var partsList = new List<object>
                {
                    new { text = promptText }
                };

                // Check for proof image attachment
                if (!string.IsNullOrWhiteSpace(feedback.ProofImage))
                {
                    try
                    {
                        var sanitizedPath = feedback.ProofImage.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                        var fullImagePath = Path.Combine(_webHostEnvironment.WebRootPath, sanitizedPath);

                        if (File.Exists(fullImagePath))
                        {
                            var extension = Path.GetExtension(fullImagePath).ToLowerInvariant();
                            var mimeType = extension switch
                            {
                                ".jpg" or ".jpeg" => "image/jpeg",
                                ".png" => "image/png",
                                ".webp" => "image/webp",
                                ".gif" => "image/gif",
                                _ => "image/jpeg"
                            };

                            var imageBytes = await File.ReadAllBytesAsync(fullImagePath);
                            var base64Image = Convert.ToBase64String(imageBytes);

                            partsList.Add(new
                            {
                                inlineData = new
                                {
                                    mimeType = mimeType,
                                    data = base64Image
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load proof image for feedback ID {FeedbackId}", feedbackId);
                    }
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = partsList }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var httpResponse = await _httpClient.PostAsync(requestUrl, jsonContent);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API call failed with status {StatusCode}: {Response}", httpResponse.StatusCode, responseContent);
                    return new GeminiAiAnalysisResultViewModel
                    {
                        FeedbackId = feedbackId,
                        IsSuccess = false,
                        ErrorMessage = $"Gemini API call failed with status {httpResponse.StatusCode}."
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return new GeminiAiAnalysisResultViewModel
                    {
                        FeedbackId = feedbackId,
                        IsSuccess = false,
                        ErrorMessage = "No response generated by Gemini model."
                    };
                }

                var firstCandidate = candidates[0];
                var parts = firstCandidate.GetProperty("content").GetProperty("parts");
                var generatedJsonText = parts[0].GetProperty("text").GetString() ?? "{}";

                using var analysisDoc = JsonDocument.Parse(generatedJsonText);
                var analysisRoot = analysisDoc.RootElement;

                bool isCustomerInterested = false;
                if (analysisRoot.TryGetProperty("isCustomerInterested", out var propInterested))
                {
                    isCustomerInterested = propInterested.ValueKind == JsonValueKind.True;
                }

                string buyingIntent = "Moderate";
                if (analysisRoot.TryGetProperty("buyingIntent", out var propIntent))
                {
                    var intentVal = propIntent.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(intentVal))
                    {
                        buyingIntent = intentVal switch
                        {
                            var v when v.Contains("High", StringComparison.OrdinalIgnoreCase) => "High",
                            var v when v.Contains("Low", StringComparison.OrdinalIgnoreCase) => "Low",
                            _ => "Moderate"
                        };
                    }
                }

                string sentiment = "Neutral";
                if (analysisRoot.TryGetProperty("sentiment", out var propSentiment))
                {
                    var sentVal = propSentiment.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(sentVal))
                    {
                        sentiment = sentVal switch
                        {
                            var v when v.Contains("Pos", StringComparison.OrdinalIgnoreCase) => "Positive",
                            var v when v.Contains("Neg", StringComparison.OrdinalIgnoreCase) => "Negative",
                            _ => "Neutral"
                        };
                    }
                }

                bool siteVisitInterested = false;
                if (analysisRoot.TryGetProperty("siteVisitInterested", out var propSiteVisit))
                {
                    siteVisitInterested = propSiteVisit.ValueKind == JsonValueKind.True;
                }

                string aiSummary = string.Empty;
                if (analysisRoot.TryGetProperty("aiSummary", out var propSummary))
                {
                    aiSummary = propSummary.GetString() ?? string.Empty;
                }

                // Update entity in database
                feedback.AiCustomerInterested = isCustomerInterested;
                feedback.AiBuyingIntent = buyingIntent;
                feedback.AiSentiment = sentiment;
                feedback.AiSiteVisitInterested = siteVisitInterested;
                feedback.AiSummary = aiSummary.Length > 2000 ? aiSummary.Substring(0, 2000) : aiSummary;
                feedback.AiAnalyzedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new GeminiAiAnalysisResultViewModel
                {
                    FeedbackId = feedback.FeedbackId,
                    IsCustomerInterested = isCustomerInterested,
                    BuyingIntent = buyingIntent,
                    Sentiment = sentiment,
                    SiteVisitInterested = siteVisitInterested,
                    AiSummary = feedback.AiSummary,
                    AnalyzedAt = feedback.AiAnalyzedAt,
                    HasImage = !string.IsNullOrWhiteSpace(feedback.ProofImage),
                    ProofImageUrl = feedback.ProofImage,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception analyzing feedback {FeedbackId} with Gemini", feedbackId);
                return new GeminiAiAnalysisResultViewModel
                {
                    FeedbackId = feedbackId,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<GeminiLeadAnalysisResultViewModel?> GetLeadAnalysisAsync(long leadId)
        {
            var lead = await _context.Leads
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LeadId == leadId);

            if (lead == null || !lead.AiAnalyzedAt.HasValue)
            {
                return null;
            }

            var totalFollowUps = await _context.Feedbacks
                .CountAsync(f => f.LeadAssignment != null && f.LeadAssignment.LeadId == leadId && !f.IsDeleted);

            return new GeminiLeadAnalysisResultViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                BuyingDecision = lead.AiBuyingDecision ?? "Likely to Buy",
                InterestLevel = lead.AiInterestLevel ?? "Medium",
                LeadAuthenticity = lead.AiLeadAuthenticity ?? "Genuine Lead",
                SynthesisSummary = lead.AiSynthesisSummary ?? string.Empty,
                TotalFollowUpsAnalyzed = totalFollowUps,
                AnalyzedAt = lead.AiAnalyzedAt,
                IsSuccess = true
            };
        }

        public async Task<GeminiLeadAnalysisResultViewModel> AnalyzeLeadAsync(long leadId, bool forceReanalysis = false)
        {
            try
            {
                var lead = await _context.Leads
                    .FirstOrDefaultAsync(l => l.LeadId == leadId);

                if (lead == null)
                {
                    return new GeminiLeadAnalysisResultViewModel
                    {
                        LeadId = leadId,
                        IsSuccess = false,
                        ErrorMessage = "Lead not found."
                    };
                }

                var assignments = await _context.LeadAssignments
                    .Include(la => la.SalesOfficer)
                    .Include(la => la.Feedbacks)
                    .Where(la => la.LeadId == leadId && !la.IsDeleted)
                    .ToListAsync();

                var allFeedbacks = assignments
                    .SelectMany(la => (IEnumerable<Feedback>?)la.Feedbacks ?? Enumerable.Empty<Feedback>())
                    .Where(f => !f.IsDeleted)
                    .OrderBy(f => f.SubmittedAt)
                    .ToList();

                // If already analyzed and force reanalysis is not requested, return saved result
                if (!forceReanalysis && lead.AiAnalyzedAt.HasValue)
                {
                    return new GeminiLeadAnalysisResultViewModel
                    {
                        LeadId = lead.LeadId,
                        LeadCode = lead.LeadCode,
                        LeadName = lead.LeadName,
                        BuyingDecision = lead.AiBuyingDecision ?? "Likely to Buy",
                        InterestLevel = lead.AiInterestLevel ?? "Medium",
                        LeadAuthenticity = lead.AiLeadAuthenticity ?? "Genuine Lead",
                        SynthesisSummary = lead.AiSynthesisSummary ?? string.Empty,
                        TotalFollowUpsAnalyzed = allFeedbacks.Count,
                        AnalyzedAt = lead.AiAnalyzedAt,
                        IsSuccess = true
                    };
                }

                var apiKey = _geminiSettings.ApiKey?.Trim();
                var model = string.IsNullOrWhiteSpace(_geminiSettings.Model) ? "gemini-3.5-flash-lite" : _geminiSettings.Model.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new GeminiLeadAnalysisResultViewModel
                    {
                        LeadId = leadId,
                        IsSuccess = false,
                        ErrorMessage = "Gemini API key is not configured."
                    };
                }

                var followUpHistoryText = new StringBuilder();
                int counter = 1;
                foreach (var fb in allFeedbacks)
                {
                    followUpHistoryText.AppendLine($"Follow-up #{counter++} ({fb.SubmittedAt:yyyy-MM-dd HH:mm UTC}):");
                    followUpHistoryText.AppendLine($"- Status: {fb.Status}");
                    followUpHistoryText.AppendLine($"- Summary: {fb.Summary}");
                    if (!string.IsNullOrWhiteSpace(fb.Notes))
                    {
                        followUpHistoryText.AppendLine($"- Additional Notes: {fb.Notes}");
                    }
                    if (!string.IsNullOrWhiteSpace(fb.ProofImage))
                    {
                        followUpHistoryText.AppendLine($"- Attached Proof: Image provided");
                    }
                    followUpHistoryText.AppendLine();
                }

                var promptText = $@"You are a Chief Sales Intelligence Analyst.
Analyze the entire customer journey and ALL follow-up logs for this lead:

Lead Code: {lead.LeadCode}
Customer Name: {lead.LeadName}
Company: {lead.CompanyName ?? "N/A"}
Phone: {lead.Phone}
Email: {lead.Email ?? "N/A"}
Profession: {lead.Profession ?? "N/A"}
Current Lead Status: {lead.Status}
Priority: {lead.Priority}
Lead Source: {lead.Source}
Description / Requirements: {lead.Description ?? "N/A"}

Total Follow-ups: {allFeedbacks.Count}
Detailed Follow-up History:
{(followUpHistoryText.Length > 0 ? followUpHistoryText.ToString() : "No follow-up feedback submitted yet.")}

Based on the cumulative follow-ups, officer remarks, customer reactions, and consistency of interactions, evaluate:
1. buyingDecision: (string) Will the customer buy or not? Strictly one of: 'Will Buy' / 'Likely to Buy' / 'Unlikely to Buy' / 'Will Not Buy'
2. interestLevel: (string) Customer interest level - strictly one of: 'High' / 'Medium' / 'Low' / 'Zero Interest'
3. leadAuthenticity: (string) Is this lead genuine or fake/unresponsive? Strictly one of: 'Genuine Lead' / 'Suspicious / Potential Fake' / 'Invalid / Unresponsive'
4. synthesisSummary: (string) A comprehensive 2-3 sentence executive assessment summarizing their buying intent, key objections or progress across follow-ups, and recommendation for the sales team.

Return ONLY valid JSON matching this schema:
{{
  ""buyingDecision"": ""Likely to Buy"",
  ""interestLevel"": ""High"",
  ""leadAuthenticity"": ""Genuine Lead"",
  ""synthesisSummary"": ""...""
}}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new object[] { new { text = promptText } } }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var httpResponse = await _httpClient.PostAsync(requestUrl, jsonContent);
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini Lead Analysis API call failed with status {StatusCode}: {Response}", httpResponse.StatusCode, responseContent);
                    return new GeminiLeadAnalysisResultViewModel
                    {
                        LeadId = leadId,
                        IsSuccess = false,
                        ErrorMessage = $"Gemini API call failed with status {httpResponse.StatusCode}."
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return new GeminiLeadAnalysisResultViewModel
                    {
                        LeadId = leadId,
                        IsSuccess = false,
                        ErrorMessage = "No response generated by Gemini model."
                    };
                }

                var firstCandidate = candidates[0];
                var parts = firstCandidate.GetProperty("content").GetProperty("parts");
                var generatedJsonText = parts[0].GetProperty("text").GetString() ?? "{}";

                using var analysisDoc = JsonDocument.Parse(generatedJsonText);
                var analysisRoot = analysisDoc.RootElement;

                string buyingDecision = "Likely to Buy";
                if (analysisRoot.TryGetProperty("buyingDecision", out var propDecision))
                {
                    var val = propDecision.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        buyingDecision = val;
                    }
                }

                string interestLevel = "Medium";
                if (analysisRoot.TryGetProperty("interestLevel", out var propInterest))
                {
                    var val = propInterest.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        interestLevel = val;
                    }
                }

                string leadAuthenticity = "Genuine Lead";
                if (analysisRoot.TryGetProperty("leadAuthenticity", out var propAuth))
                {
                    var val = propAuth.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        leadAuthenticity = val;
                    }
                }

                string synthesisSummary = string.Empty;
                if (analysisRoot.TryGetProperty("synthesisSummary", out var propSummary))
                {
                    synthesisSummary = propSummary.GetString() ?? string.Empty;
                }

                // Update lead in database
                lead.AiBuyingDecision = buyingDecision;
                lead.AiInterestLevel = interestLevel;
                lead.AiLeadAuthenticity = leadAuthenticity;
                lead.AiSynthesisSummary = synthesisSummary.Length > 3000 ? synthesisSummary.Substring(0, 3000) : synthesisSummary;
                lead.AiAnalyzedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new GeminiLeadAnalysisResultViewModel
                {
                    LeadId = lead.LeadId,
                    LeadCode = lead.LeadCode,
                    LeadName = lead.LeadName,
                    BuyingDecision = buyingDecision,
                    InterestLevel = interestLevel,
                    LeadAuthenticity = leadAuthenticity,
                    SynthesisSummary = lead.AiSynthesisSummary,
                    TotalFollowUpsAnalyzed = allFeedbacks.Count,
                    AnalyzedAt = lead.AiAnalyzedAt,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception analyzing lead {LeadId} with Gemini", leadId);
                return new GeminiLeadAnalysisResultViewModel
                {
                    LeadId = leadId,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
