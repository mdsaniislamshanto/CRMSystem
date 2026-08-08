using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface ILeadFeedbackService
    {
        Task SubmitFeedbackAsync(SubmitFeedbackViewModel model, long salesOfficerId);
        Task<List<FeedbackHistoryViewModel>> GetFeedbackHistoryAsync(long salesOfficerId);
        Task<FeedbackDetailsViewModel?> GetFeedbackDetailsAsync(long feedbackId, long salesOfficerId);
        Task<List<SalesOfficerFollowUpViewModel>> GetSalesOfficerFollowUpsAsync(long salesOfficerId);


    }
}