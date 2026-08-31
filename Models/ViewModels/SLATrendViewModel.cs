namespace CRMSystem.Models.ViewModels
{
    public class SLATrendViewModel
    {
        // =====================================================
        // Trend Date
        // =====================================================

        public DateTime Date { get; set; }


        // =====================================================
        // Acceptance SLA
        // =====================================================

        public double AcceptanceSLAComplianceRate { get; set; }


        // =====================================================
        // First Feedback SLA
        // =====================================================

        public double FirstFeedbackSLAComplianceRate { get; set; }


        // =====================================================
        // Next Feedback SLA
        // =====================================================

        public double NextFeedbackSLAComplianceRate { get; set; }
    }
}