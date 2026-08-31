namespace CRMSystem.Models.ViewModels
{
    public class PerformanceTrendViewModel
    {
        // =====================================================
        // Date
        // =====================================================

        public DateTime Date { get; set; }


        // =====================================================
        // Lead Performance
        // =====================================================

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }


        // =====================================================
        // Feedback Performance
        // =====================================================

        public int Feedbacks { get; set; }


        // =====================================================
        // Follow-up Performance
        // =====================================================

        public int OverdueFollowUps { get; set; }
    }
}