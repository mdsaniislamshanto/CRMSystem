using CRMSystem.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using CRMSystem.Validators;

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

        [FutureDate]
        public DateTime? NextFollowUpDate { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}