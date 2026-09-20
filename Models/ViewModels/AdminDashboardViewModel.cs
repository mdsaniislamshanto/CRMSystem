namespace CRMSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        // =====================================================
        // Lead Status Statistics
        // =====================================================

        public int NewLeads { get; set; }

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int InProgressLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int RejectedLeads { get; set; }


        // =====================================================
        // Lead Overview
        // =====================================================

        public int TotalLeads { get; set; }

        public int UnassignedLeads { get; set; }

        public int ArchivedLeads { get; set; }


        // =====================================================
        // User / Workforce Statistics
        // =====================================================

        public int TotalUsers { get; set; }

        public int AdminUsers { get; set; }

        public int SalesManagers { get; set; }

        public int TeamLeads { get; set; }

        public int SalesOfficers { get; set; }

        public int AccountUsers { get; set; }

        public int HRUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int InactiveUsers { get; set; }


        // =====================================================
        // Overall Sales Manager Target Overview
        // =====================================================

        public int TotalTarget { get; set; }

        public int TargetFulfilled { get; set; }

        public decimal TargetProgress { get; set; }


        // =====================================================
        // Monthly Lead Trend
        // =====================================================

        public List<MonthlyLeadTrendItem> MonthlyLeadTrend { get; set; }
            = new List<MonthlyLeadTrendItem>();
    }
}