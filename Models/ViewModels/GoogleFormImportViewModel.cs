using System.Collections.Generic;

namespace CRMSystem.Models.ViewModels
{
    public class GoogleFormImportViewModel
    {
        public List<AutoLeadCreateViewModel> Candidates { get; set; }
            = new();

        public int TotalCandidates { get; set; }

        public int NewLeads { get; set; }

        public int DuplicateLeads { get; set; }

        public int FailedLeads { get; set; }

        public bool ImportCompleted { get; set; }

        public string? Message { get; set; }
    }
}