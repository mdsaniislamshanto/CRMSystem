namespace CRMSystem.Models.ViewModels
{
    public class TeamLeadDashboardViewModel
    {
        public string TeamLeadName { get; set; } =
            string.Empty;


        // =========================================================
        // Team Summary
        // =========================================================

        public int TeamMemberCount { get; set; }


        // =========================================================
        // Lead Summary
        // =========================================================

        public int ActiveLeadCount { get; set; }

        public int CompletedLeadCount { get; set; }

        public int PendingFollowUpCount { get; set; }


        // =========================================================
        // Lead Pipeline
        // =========================================================

        public int NewLeadCount { get; set; }

        public int AssignedLeadCount { get; set; }

        public int AcceptedLeadCount { get; set; }


        // =========================================================
        // Current Target
        // =========================================================

        public int TotalTarget { get; set; }

        public int TargetFulfilled { get; set; }
    }
}