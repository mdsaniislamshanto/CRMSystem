using CRMSystem.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMSystem.Models.Entities
{
    public class Feedback : BaseEntity
    {
        [Key]
        public long FeedbackId { get; set; }

        [Required]
        public long AssignmentId { get; set; }

        [Required]
        public string Summary { get; set; } = string.Empty;

        public FeedbackStatus Status { get; set; }

        public string? ProofImage { get; set; }

        public string? VoiceRecording { get; set; }

        public string? Notes { get; set; }

        public DateTime SubmittedAt { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public LeadAssignment? LeadAssignment { get; set; }
    }
}