using CRMSystem.Configurations;
using CRMSystem.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Options;

namespace CRMSystem.Services.GoogleForms
{
    public class GoogleFormsService : IGoogleFormsService
    {
        private readonly GoogleFormsSettings _settings;

        private const string FormsResponsesReadonlyScope =
            "https://www.googleapis.com/auth/forms.responses.readonly";

        public GoogleFormsService(
            IOptions<GoogleFormsSettings> settings)
        {
            _settings = settings.Value;
        }

        public Task<string> GetAuthorizationUrlAsync()
        {
            var flow = CreateAuthorizationFlow();

            var request = flow.CreateAuthorizationCodeRequest( _settings.RedirectUri);

            return Task.FromResult(request.Build().ToString());
        }

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

            return token != null &&
                   !string.IsNullOrWhiteSpace(token.AccessToken);
        }

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