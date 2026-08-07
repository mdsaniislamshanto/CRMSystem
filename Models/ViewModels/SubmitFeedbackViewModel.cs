using CRMSystem.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class SubmitFeedbackViewModel
    {
        [Required]
        public long AssignmentId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public FeedbackStatus Status { get; set; }

        public IFormFile? ProofImage { get; set; }

        public IFormFile? VoiceRecording { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}