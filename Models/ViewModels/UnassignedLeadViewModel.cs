using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class UnassignedLeadViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string Phone { get; set; } = string.Empty;

        public LeadPriority Priority { get; set; }

        public LeadSource Source { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}