using CRMSystem.Models.ViewModels;
using Google.Apis.Forms.v1.Data;

namespace CRMSystem.Services.Interfaces
{
    public interface IGoogleFormsService
    {
        Task<string> GetAuthorizationUrlAsync();

        Task<bool> HandleCallbackAsync(string code);

        Task<IList<FormResponse>> GetResponsesAsync();

        Task<IList<Item>> GetFormItemsAsync();

        Task<List<AutoLeadCreateViewModel>> GetLeadCandidatesAsync();
    }
}