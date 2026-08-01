using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class LeadCaptureLog : BaseEntity
    {
        [Key]
        public long CaptureLogId { get; set; }

        [Required]
        public long LeadId { get; set; }

        [Required]
        public LeadCaptureSource CaptureSource { get; set; }

        [Required]
        public CaptureStatus CaptureStatus { get; set; }

        [StringLength(200)]
        public string? ExternalLeadId { get; set; }

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public string? PayloadJson { get; set; }

        public DateTime ReceivedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [ForeignKey(nameof(LeadId))]
        public Lead? Lead { get; set; }
    }
}