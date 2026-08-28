namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerSettingsViewModel
    {
        public ProfileViewModel Profile { get; set; } = new();

        public bool AutoAssignmentEnabled { get; set; }

        public bool AutoAssignmentRequestPending { get; set; }

        public bool LeadAssignmentNotificationEnabled { get; set; }

        public bool FollowUpNotificationEnabled { get; set; }

        public bool FeedbackNotificationEnabled { get; set; }

        public bool OverdueNotificationEnabled { get; set; }

        public bool ShowOverdueLeads { get; set; } = true;

        public bool ShowFollowUps { get; set; } = true;

        public string DashboardPeriod { get; set; } = "This Month";
    }
}