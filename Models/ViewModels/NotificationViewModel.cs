using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class NotificationViewModel
    {
        public long NotificationId { get; set; }

        public NotificationType NotificationType { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public long? LeadId { get; set; }

        public long? AssignmentId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}