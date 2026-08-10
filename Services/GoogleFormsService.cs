using CRMSystem.Configurations;
using CRMSystem.Data;
using CRMSystem.Models.Entities;
using CRMSystem.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Google.Apis.Forms.v1;
using Google.Apis.Services;

namespace CRMSystem.Services.GoogleForms
{
    public class GoogleFormsService : IGoogleFormsService
    {
        private readonly GoogleFormsSettings _settings;
        private readonly ApplicationDbContext _context;

        private const string FormsResponsesReadonlyScope =
            "https://www.googleapis.com/auth/forms.responses.readonly";

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
                        FormsResponsesReadonlyScope
                    }
                });
        }
    }
}