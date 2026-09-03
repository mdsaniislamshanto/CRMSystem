namespace CRMSystem.Models.ViewModels
{
    public class FollowUpFilterViewModel
    {
        public string? Search { get; set; }

        public string? FollowUpStatus { get; set; }

        public string? FeedbackStatus { get; set; }

        public string? Sort { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalItems { get; set; }

        public int TotalPages =>
            TotalItems == 0
                ? 1
                : (int)Math.Ceiling(TotalItems / (double)PageSize);

        public int TotalFollowUps { get; set; }

        public int DueTodayCount { get; set; }

        public int UpcomingCount { get; set; }

        public int OverdueCount { get; set; }

        public List<FollowUpViewModel> FollowUps { get; set; } = new();

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}