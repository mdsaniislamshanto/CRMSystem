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
        [StringLength(1000)]
        public string Summary { get; set; } = string.Empty;

        public FeedbackStatus Status { get; set; }

        public string? ProofImage { get; set; }

        public string? VoiceRecording { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public DateTime SubmittedAt { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        // ==============================
        // SLA Tracking
        // ==============================

        public bool NextFeedbackSLAMissed { get; set; } = false;

        // ==============================
        // Gemini AI Follow-up Analysis
        // ==============================

        public bool? AiCustomerInterested { get; set; }

        [StringLength(50)]
        public string? AiBuyingIntent { get; set; }

        [StringLength(50)]
        public string? AiSentiment { get; set; }

        public bool? AiSiteVisitInterested { get; set; }

        [StringLength(2000)]
        public string? AiSummary { get; set; }

        public DateTime? AiAnalyzedAt { get; set; }


        [ForeignKey(nameof(AssignmentId))]
        public LeadAssignment? LeadAssignment { get; set; }
    }
}