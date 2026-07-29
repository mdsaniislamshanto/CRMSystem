using CRMSystem.Enums;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class EditLeadViewModel
    {
        public long LeadId { get; set; }

        [Required]
        public string LeadCode { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        [Required]
        public string LeadName { get; set; } = string.Empty;

        public string? Profession { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        public LeadSource Source { get; set; }

        public LeadPriority Priority { get; set; }

        public LeadStatus Status { get; set; }

        public string? Description { get; set; }

        public DateTime? FollowUpDate { get; set; }
    }
}