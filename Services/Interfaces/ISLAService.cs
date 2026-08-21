namespace CRMSystem.Services.Interfaces
{
    public interface ISLAService
    {
        Task CheckAcceptanceSLAsAsync();

        Task CheckFirstFeedbackSLAsAsync();

        Task CheckNextFeedbackSLAsAsync();
    }
}