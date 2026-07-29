using System.ComponentModel.DataAnnotations;
using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class CreateLeadViewModel
    {
       

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        public string? Profession { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        public LeadSource Source { get; set; }

        public LeadPriority Priority { get; set; }

        public string? Description { get; set; }

        [Display(Name = "Follow Up Date")]
        public DateTime? FollowUpDate { get; set; }
    }
}