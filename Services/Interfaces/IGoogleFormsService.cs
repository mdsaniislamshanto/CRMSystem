namespace CRMSystem.Services.Interfaces
{
    public interface IGoogleFormsService
    {
        Task<string> GetAuthorizationUrlAsync();

        Task<bool> HandleCallbackAsync(string code);
        Task<IList<Google.Apis.Forms.v1.Data.FormResponse>> GetResponsesAsync();
    }
}