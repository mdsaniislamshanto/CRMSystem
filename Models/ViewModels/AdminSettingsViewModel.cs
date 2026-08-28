namespace CRMSystem.Models.ViewModels
{
    public class AdminSettingsViewModel
    {
        // =====================================================
        // Profile
        // =====================================================

        public ProfileViewModel Profile { get; set; } = new();


        // =====================================================
        // Lead Assignment Settings
        // =====================================================

        public bool AutoAssignmentEnabled { get; set; }

        public bool ManualAssignmentEnabled { get; set; } = true;

        public bool ReassignmentEnabled { get; set; } = true;


        // =====================================================
        // Global Notification Master Switch
        // =====================================================

        public bool GlobalNotificationsEnabled { get; set; } = true;


        // =====================================================
        // Notification Type Settings
        // =====================================================

        public bool LeadAssignmentNotificationEnabled { get; set; } = true;

        public bool FollowUpNotificationEnabled { get; set; } = true;

        public bool FeedbackNotificationEnabled { get; set; } = true;

        public bool OverdueNotificationEnabled { get; set; } = true;


        // =====================================================
        // Global Notification Type Settings
        // =====================================================

        public bool GlobalLeadAssignmentNotificationEnabled { get; set; } = true;

        public bool GlobalFollowUpNotificationEnabled { get; set; } = true;

        public bool GlobalFeedbackNotificationEnabled { get; set; } = true;

        public bool GlobalOverdueNotificationEnabled { get; set; } = true;

        // =====================================================
        // Admin Personal Notification
        // =====================================================

        public bool MyNotificationsEnabled { get; set; } = true;


        // =====================================================
        // SLA / Feedback Settings
        // =====================================================

        public int FirstFeedbackDeadlineHours { get; set; } = 3;


        // =====================================================
        // CRM Information
        // =====================================================

        public string? CrmName { get; set; }

        public string? CompanyName { get; set; }

        public string? DefaultLeadPriority { get; set; }
    }
}