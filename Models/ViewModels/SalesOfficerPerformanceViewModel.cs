using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerPerformanceViewModel
    {
        // =====================================================
        // Sales Officer Information
        // =====================================================

        public long SalesOfficerId { get; set; }

        public string SalesOfficerName { get; set; } = string.Empty;


        // =====================================================
        // Lead Performance
        // =====================================================

        public int TotalAssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int PendingAcceptance { get; set; }

        public double AcceptanceRate { get; set; }


        // =====================================================
        // Acceptance SLA
        // =====================================================

        public int AcceptanceSLAMet { get; set; }

        public int AcceptanceSLAMissed { get; set; }

        public double AcceptanceSLAComplianceRate { get; set; }


        // =====================================================
        // First Feedback SLA
        // =====================================================

        public int FirstFeedbackSLAMet { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }

        public double FirstFeedbackSLAComplianceRate { get; set; }


        // =====================================================
        // Next Feedback SLA
        // =====================================================

        public int NextFeedbackSLAMet { get; set; }

        public int NextFeedbackSLAMissed { get; set; }

        public double NextFeedbackSLAComplianceRate { get; set; }


        // =====================================================
        // Lead Completion
        // =====================================================

        public int CompletedLeads { get; set; }


        // =====================================================
        // Overall Performance
        // =====================================================

        public double PerformanceScore { get; set; }


        // =====================================================
        // Feedback Performance
        // =====================================================

        public int TotalFeedbacks { get; set; }


        // =====================================================
        // Follow-up Performance
        // =====================================================

        public int FollowUpsCompletedOnTime { get; set; }

        public int FollowUpsCompletedLate { get; set; }

        public int OverdueFollowUps { get; set; }

        public double FollowUpTimelinessRate { get; set; }


        // =====================================================
        // Lead Completion
        // =====================================================

      

        public double CompletionRate { get; set; }


        // =====================================================
        // Feedback Status Overview
        // =====================================================

        public int InterestedCount { get; set; }

        public int FollowUpRequiredCount { get; set; }

        public int MeetingScheduledCount { get; set; }

        public int VisitedCount { get; set; }

        public int QuotationSentCount { get; set; }

        public int NegotiationCount { get; set; }

        public int CompletedCount { get; set; }

        public int ClosedCount { get; set; }
    }
}