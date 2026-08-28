using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.Entities
{
    public class SystemSettings : BaseEntity
    {
        [Key]
        public long SettingId { get; set; }

        // =====================================================
        // Lead Assignment
        // =====================================================

        [Required]
        public bool AutoAssignmentEnabled { get; set; }


        // =====================================================
        // Global Notification Control
        // =====================================================

        [Required]
        public bool GlobalNotificationsEnabled { get; set; } = true;


        // =====================================================
        // Notification Types
        // =====================================================

        [Required]
        public bool LeadAssignmentNotificationEnabled { get; set; } = true;

        [Required]
        public bool FollowUpNotificationEnabled { get; set; } = true;

        [Required]
        public bool FeedbackNotificationEnabled { get; set; } = true;

        [Required]
        public bool OverdueNotificationEnabled { get; set; } = true;
    }
}