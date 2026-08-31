namespace CRMSystem.Models.ViewModels
{
    public class PerformanceHighlightViewModel
    {
        // =====================================================
        // Sales Officer Information
        // =====================================================

        public long SalesOfficerId { get; set; }

        public string SalesOfficerName { get; set; } = string.Empty;


        // =====================================================
        // Overall Performance
        // =====================================================

        public double PerformanceScore { get; set; }


        // =====================================================
        // Lead Performance
        // =====================================================

        public int TotalAssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }


        // =====================================================
        // Performance Rates
        // =====================================================

        public double AcceptanceRate { get; set; }

        public double CompletionRate { get; set; }


        // =====================================================
        // Acceptance SLA
        // =====================================================

        public double AcceptanceSLAComplianceRate { get; set; }

        public int AcceptanceSLAMissed { get; set; }


        // =====================================================
        // First Feedback SLA
        // =====================================================

        public double FirstFeedbackSLAComplianceRate { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }


        // =====================================================
        // Next Feedback SLA
        // =====================================================

        public double NextFeedbackSLAComplianceRate { get; set; }

        public int NextFeedbackSLAMissed { get; set; }


        // =====================================================
        // Follow-up Performance
        // =====================================================

        public double FollowUpTimelinessRate { get; set; }

        public int OverdueFollowUps { get; set; }


        // =====================================================
        // Feedback Performance
        // =====================================================

        public int TotalFeedbacks { get; set; }
    }
}