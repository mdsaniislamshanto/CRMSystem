using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class ApiLeadViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string? Email { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Profession { get; set; }

        public LeadSource Source { get; set; }

        public LeadPriority Priority { get; set; }

        public LeadStatus Status { get; set; }

        public string? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastContactDate { get; set; }
    }
}