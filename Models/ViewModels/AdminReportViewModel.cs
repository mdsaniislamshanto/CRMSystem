namespace CRMSystem.Models.ViewModels
{
    public class AdminReportViewModel
    {
        // Lead Summary
        public int TotalLeads { get; set; }

        public int NewLeads { get; set; }

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int InProgressLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int RejectedLeads { get; set; }

        // Assignment Summary
        public int UnassignedLeads { get; set; }

        // Sales Officer Performance
        public List<SalesOfficerReportViewModel> SalesOfficerReports { get; set; }
            = new();

        // Lead Source Summary
        public List<LeadSourceReportViewModel> LeadSourceReports { get; set; }
            = new();
    }


    public class SalesOfficerReportViewModel
    {
        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int InProgressLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int RejectedLeads { get; set; }
    }


    public class LeadSourceReportViewModel
    {
        public string Source { get; set; } = string.Empty;

        public int LeadCount { get; set; }
    }
}