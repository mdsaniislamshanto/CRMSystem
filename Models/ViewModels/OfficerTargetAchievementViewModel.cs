namespace CRMSystem.Models.ViewModels
{
    public class OfficerTargetAchievementViewModel
    {
        public long TargetId { get; set; }

        public long SalesOfficerId { get; set; }

        public string SalesOfficerName { get; set; } =
            string.Empty;

        public string PeriodType { get; set; } =
            string.Empty;

        public int TargetCount { get; set; }

        public int CompletedCount { get; set; }

        public int RemainingCount { get; set; }

        public decimal AchievementPercentage { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } =
            string.Empty;
    }
}