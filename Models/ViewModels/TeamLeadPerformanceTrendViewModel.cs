namespace CRMSystem.Models.ViewModels
{
    public class TeamLeadPerformanceTrendViewModel
    {
        public string TeamLeadName { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        // Existing weekly performance trend data.
        public List<PerformanceTrendPointViewModel> TrendPoints { get; set; } = new();

        // Target Achievement Summary
        public int TeamTarget { get; set; }

        public int TeamCompletedLeads { get; set; }

        public double TargetAchievementPercentage { get; set; }
    }

    public class PerformanceTrendPointViewModel
    {
        public string Label { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }

        public double AcceptanceSLAPercentage { get; set; }

        public double FirstFeedbackSLAPercentage { get; set; }

        public double NextFeedbackSLAPercentage { get; set; }

        public double FollowUpPercentage { get; set; }

        public double PerformanceScore { get; set; }
    }
}