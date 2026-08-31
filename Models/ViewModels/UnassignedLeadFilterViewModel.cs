using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class UnassignedLeadFilterViewModel
    {
        public string? Search { get; set; }

        public LeadSource? Source { get; set; }

        public LeadPriority? Priority { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<UnassignedLeadViewModel> Leads { get; set; } = new();
    }
}