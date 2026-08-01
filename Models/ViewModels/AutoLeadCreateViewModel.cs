using System.ComponentModel.DataAnnotations;
using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class AutoLeadCreateViewModel
    {
        [Required]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Profession { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public LeadSource Source { get; set; }

        public LeadPriority Priority { get; set; }

        public string? Description { get; set; }
    }
}