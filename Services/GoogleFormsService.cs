using CRMSystem.Configurations;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Forms.v1;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;  

namespace CRMSystem.Services.GoogleForms
{
    public class GoogleFormsService : IGoogleFormsService
    {
        private readonly GoogleFormsSettings _settings;
        private readonly ApplicationDbContext _context;

        private const string FormsResponsesReadonlyScope =
            "https://www.googleapis.com/auth/forms.responses.readonly";
        private const string FormsBodyReadonlyScope =
    "https://www.googleapis.com/auth/forms.body.readonly"; 

        private const string GoogleFormsProvider = "GoogleForms";

        public GoogleFormsService(
            IOptions<GoogleFormsSettings> settings,
            ApplicationDbContext context)
        {
            _settings = settings.Value;
            _context = context;
        }

        public Task<string> GetAuthorizationUrlAsync()
        {
            var flow = CreateAuthorizationFlow();

            var request = flow.CreateAuthorizationCodeRequest(
                _settings.RedirectUri);

            var googleRequest =
                (Google.Apis.Auth.OAuth2.Requests.GoogleAuthorizationCodeRequestUrl)request;

            googleRequest.AccessType = "offline";
            googleRequest.Prompt = "consent";

            return Task.FromResult(googleRequest.Build().ToString());
        }


        // This method handles the callback from Google after the user authorizes the application.
        public async Task<bool> HandleCallbackAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var flow = CreateAuthorizationFlow();

            TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                "crm-google-forms-user",
                code,
                _settings.RedirectUri,
                CancellationToken.None);

            if (token == null ||
                string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return false;
            }

            var existingCredential =
                await _context.GoogleOAuthCredentials
                    .FirstOrDefaultAsync(x =>
                        x.Provider == GoogleFormsProvider &&
                        x.IsActive);

            if (existingCredential == null)
            {
                var credential = new GoogleOAuthCredential
                {
                    Provider = GoogleFormsProvider,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken ?? string.Empty,
                    TokenExpiry = token.IssuedUtc.AddSeconds(
                        token.ExpiresInSeconds ?? 3600),
                    IsActive = true
                };

                await _context.GoogleOAuthCredentials.AddAsync(credential);
            }
            else
            {
                existingCredential.AccessToken = token.AccessToken;

                if (!string.IsNullOrWhiteSpace(token.RefreshToken))
                {
                    existingCredential.RefreshToken = token.RefreshToken;
                }

                existingCredential.TokenExpiry =
                    token.IssuedUtc.AddSeconds(
                        token.ExpiresInSeconds ?? 3600);

                existingCredential.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }


        // This method retrieves the responses from the Google Form using the stored OAuth credentials.
        public async Task<IList<Google.Apis.Forms.v1.Data.FormResponse>> GetResponsesAsync()
        {
            var credential = await _context.GoogleOAuthCredentials
                .Where(x =>
                    x.Provider == GoogleFormsProvider &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (credential == null)
            {
                throw new InvalidOperationException(
                    "Google Forms is not connected.");
            }

            if (string.IsNullOrWhiteSpace(credential.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Google Forms refresh token is missing. Please reconnect Google Forms.");
            }

            var flow = CreateAuthorizationFlow();

            var token = new TokenResponse
            {
                AccessToken = credential.AccessToken,
                RefreshToken = credential.RefreshToken,
                IssuedUtc = DateTime.UtcNow,
                ExpiresInSeconds = credential.TokenExpiry.HasValue
                    ? (long)Math.Max(
                        0,
                        (credential.TokenExpiry.Value - DateTime.UtcNow).TotalSeconds)
                    : null
            };

            var userCredential = new UserCredential(
                flow,
                "crm-google-forms-user",
                token);

            var formsService = new Google.Apis.Forms.v1.FormsService(
                new Google.Apis.Services.BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = "CRM System"
                });

            var request = formsService.Forms.Responses.List(
                _settings.FormId);

            request.PageSize = 100;

            var response = await request.ExecuteAsync();

            return response.Responses
                ?? new List<Google.Apis.Forms.v1.Data.FormResponse>();
        }


        // This method retrieves the questions/items from the Google Form.
        public async Task<IList<Google.Apis.Forms.v1.Data.Item>> GetFormItemsAsync()
        {
            var credential = await _context.GoogleOAuthCredentials
                .Where(x =>
                    x.Provider == GoogleFormsProvider &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (credential == null)
            {
                throw new InvalidOperationException(
                    "Google Forms is not connected.");
            }

            if (string.IsNullOrWhiteSpace(credential.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Google Forms refresh token is missing. Please reconnect Google Forms.");
            }

            var flow = CreateAuthorizationFlow();

            var token = new TokenResponse
            {
                AccessToken = credential.AccessToken,
                RefreshToken = credential.RefreshToken,
                IssuedUtc = DateTime.UtcNow,
                ExpiresInSeconds = credential.TokenExpiry.HasValue
                    ? (long)Math.Max(
                        0,
                        (credential.TokenExpiry.Value - DateTime.UtcNow).TotalSeconds)
                    : null
            };

            var userCredential = new UserCredential(
                flow,
                "crm-google-forms-user",
                token);

            var formsService = new Google.Apis.Forms.v1.FormsService(
                new Google.Apis.Services.BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = "CRM System"
                });

            var request = formsService.Forms.Get(
                _settings.FormId);

            var form = await request.ExecuteAsync();

            return form.Items
                ?? new List<Google.Apis.Forms.v1.Data.Item>();
        }


        // This method creates a GoogleAuthorizationCodeFlow instance using the client ID, client secret, and required scopes.
        private GoogleAuthorizationCodeFlow CreateAuthorizationFlow()
        {
            return new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _settings.ClientId,
                        ClientSecret = _settings.ClientSecret
                    },

                    Scopes = new[]
                    {
                FormsResponsesReadonlyScope,
                FormsBodyReadonlyScope
                    }
                });
        }

        // This method converts Google Form responses into CRM lead view models.
        public async Task<List<AutoLeadCreateViewModel>> GetLeadCandidatesAsync()
        {
            var responses = await GetResponsesAsync();

            var items = await GetFormItemsAsync();

            var result = new List<AutoLeadCreateViewModel>();

            foreach (var response in responses)
            {
                string? leadName = null;
                string? phone = null;
                string? email = null;
                string? address = null;
                string? companyName = null;

                foreach (var answer in response.Answers)
                {
                    var questionId = answer.Key;

                    var item = items.FirstOrDefault(x =>
                        x.QuestionItem?.Question?.QuestionId == questionId);

                    var title = item?.Title?.Trim();

                    var textAnswer =
                        answer.Value.TextAnswers?.Answers?
                            .FirstOrDefault()?.Value;

                    if (string.IsNullOrWhiteSpace(textAnswer))
                    {
                        continue;
                    }

                    if (string.Equals(
                        title,
                        "Name",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        leadName = textAnswer;
                    }
                    else if (string.Equals(
                        title,
                        "Phone number",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        phone = textAnswer;
                    }
                    else if (string.Equals(
                        title,
                        "Email",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        email = textAnswer;
                    }
                    else if (string.Equals(
                        title,
                        "Address",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        address = textAnswer;
                    }
                    else if (string.Equals(
                        title,
                        "Company Name",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        companyName = textAnswer;
                    }
                }

                // Ignore invalid responses
                if (string.IsNullOrWhiteSpace(leadName) ||
                    string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                // Google Form ResponseId is the unique external reference
                var sourceReferenceId = response.ResponseId;

                // Check whether this Google Form response was already imported
                var alreadyImported = await _context.Leads
                    .AnyAsync(l =>
                        l.Source == LeadSource.GoogleForm &&
                        l.SourceReferenceId == sourceReferenceId &&
                        !l.IsDeleted);

                result.Add(new AutoLeadCreateViewModel
                {
                    LeadName = leadName,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    CompanyName = companyName,

                    // Google Form leads must have GoogleForm as their source
                    Source = LeadSource.GoogleForm,

                    Priority = LeadPriority.Medium,

                    SourceReferenceId = sourceReferenceId,

                    PayloadJson = JsonSerializer.Serialize(response),

                    IsAlreadyImported = alreadyImported,
                    SubmittedAt = response.CreateTimeDateTimeOffset?.DateTime
                });
            }

            return result;
        }

    }
    
}