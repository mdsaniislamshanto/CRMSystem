using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class ProfileChangeRequest : BaseEntity
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FieldName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? OldValue { get; set; }

        [Required]
        [StringLength(255)]
        public string NewValue { get; set; } = string.Empty;

        [Required]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public long? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? AdminComment { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public User? Reviewer { get; set; }
    }
}