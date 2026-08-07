using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ILeadFeedbackService
    {
        Task SubmitFeedbackAsync(SubmitFeedbackViewModel model, long salesOfficerId);
    }
}