namespace CRMSystem.Models.ViewModels
{
    public class TargetAchievementViewModel
    {
        public long TargetId { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; } =
            string.Empty;

        public string Role { get; set; } =
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