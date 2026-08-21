namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerFollowUpDetailsViewModel
    {
        public long LeadId { get; set; }

        public string LeadName { get; set; } = string.Empty;

        public string SalesOfficerName { get; set; } = string.Empty;

        public List<SalesManagerFeedbackHistoryViewModel> FeedbackHistory { get; set; }
            = new List<SalesManagerFeedbackHistoryViewModel>();
    }
}