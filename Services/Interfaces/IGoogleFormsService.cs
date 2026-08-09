namespace CRMSystem.Services.Interfaces
{
    public interface IGoogleFormsService
    {
        Task<string> GetAuthorizationUrlAsync();

        Task<bool> HandleCallbackAsync(string code);
    }
}