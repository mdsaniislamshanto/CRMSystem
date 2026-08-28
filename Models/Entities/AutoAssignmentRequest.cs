using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class AutoAssignmentRequest : BaseEntity
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        public long RequestedBy { get; set; }

        [Required]
        public bool RequestedStatus { get; set; }

        [Required]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public long? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? AdminComment { get; set; }

        [ForeignKey(nameof(RequestedBy))]
        public User? Requester { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public User? Reviewer { get; set; }
    }
}