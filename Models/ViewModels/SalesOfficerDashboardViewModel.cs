namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerDashboardViewModel
    {
        public int TotalAssigned { get; set; }

        public int PendingLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }

        // ==============================
        // Target Overview
        // ==============================

        public int TotalTarget { get; set; }

        public int TargetFulfilled { get; set; }
    }
}