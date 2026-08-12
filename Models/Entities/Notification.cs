using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class Notification : BaseEntity
    {
        [Key]
        public long NotificationId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public NotificationType NotificationType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public long? LeadId { get; set; }

        public long? AssignmentId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(LeadId))]
        public Lead? Lead { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public LeadAssignment? Assignment { get; set; }
    }
}