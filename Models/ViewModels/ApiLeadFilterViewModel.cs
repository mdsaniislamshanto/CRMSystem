using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class ApiLeadFilterViewModel
    {
        public string? Search { get; set; }

        public LeadSource? Source { get; set; }

        public LeadStatus? Status { get; set; }

        public List<ApiLeadViewModel> Leads { get; set; }
            = new();
    }
}