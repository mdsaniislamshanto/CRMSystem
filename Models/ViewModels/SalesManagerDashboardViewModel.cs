namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerDashboardViewModel
    {
        // ==========================
        // KPI Cards
        // ==========================
        public int TotalNewLeads { get; set; }

        public int TotalUnassignedLeads { get; set; }

        public int AssignedToday { get; set; }

        public int CompletedToday { get; set; }

        // ==========================
        // Action Center
        // ==========================
        public int PendingAssignments { get; set; }

        public int MissedAcceptanceSLA { get; set; }

        public int MissedFirstFeedbackSLA { get; set; }

        public int DueFollowUps { get; set; }

        public int ArchivedToday { get; set; }

        // ==========================
        // System Settings
        // ==========================
        public bool AutoAssignmentEnabled { get; set; }
    }
}