namespace CRMSystem.Models.ViewModels
{
    public class CompletionTrendViewModel
    {
        // =====================================================
        // Trend Date
        // =====================================================

        public DateTime Date { get; set; }


        // =====================================================
        // Lead Activity
        // =====================================================

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }


        // =====================================================
        // Completion Rate
        // =====================================================

        public double CompletionRate { get; set; }
    }
}