namespace CRMSystem.Models.ViewModels
{
    public class SalesManagerReportViewModel
    {
        // =====================================================
        // Team Summary
        // =====================================================

        public int TotalSalesOfficers { get; set; }

        public int TotalAssignedLeads { get; set; }

        public int TotalAcceptedLeads { get; set; }

        public int TotalPendingAcceptance { get; set; }

        public double OverallAcceptanceRate { get; set; }

        public int TotalCompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int TotalAcceptanceSLAMissed { get; set; }

        public int TotalFirstFeedbackSLAMissed { get; set; }

        public int TotalOverdueFollowUps { get; set; }


        // =====================================================
        // Sales Officer Reports
        // =====================================================

        public List<SalesManagerOfficerReportViewModel>
            SalesOfficerReports
        { get; set; } = new();
    }


    // =========================================================
    // Individual Sales Officer Report
    // =========================================================

    public class SalesManagerOfficerReportViewModel
    {
        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int PendingAcceptance { get; set; }

        public double AcceptanceRate { get; set; }

        public int CompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int AcceptanceSLAMissed { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }

        public int OverdueFollowUps { get; set; }
    }
}