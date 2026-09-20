using System;
using System.Collections.Generic;

namespace CRMSystem.Models.ViewModels
{
    public class TeamLeadReportViewModel
    {
        public string TeamLeadName { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int TotalSalesOfficers { get; set; }

        public int TotalAssignedLeads { get; set; }

        public int TotalAcceptedLeads { get; set; }

        public int TotalCompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int TotalPendingFollowUps { get; set; }

        public int TotalOverdueFollowUps { get; set; }

        public int TeamTarget { get; set; }

        public int TargetCompletedLeads { get; set; }

        public double TargetAchievementPercentage { get; set; }

        public double AcceptanceRate { get; set; }

        public double AcceptanceSLAComplianceRate { get; set; }

        public double FirstFeedbackSLAComplianceRate { get; set; }

        public double NextFeedbackSLAComplianceRate { get; set; }

        public double FollowUpTimelinessRate { get; set; }

        public double CompletionRate { get; set; }

        public double PerformanceScore { get; set; }

        public List<TeamLeadOfficerReportViewModel> OfficerReports { get; set; }
            = new List<TeamLeadOfficerReportViewModel>();
    }


    public class TeamLeadOfficerReportViewModel
    {
        public long SalesOfficerId { get; set; }

        public string SalesOfficerName { get; set; } = string.Empty;

        public int TotalAssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public double AcceptanceRate { get; set; }

        public int AcceptanceSLAMet { get; set; }

        public int AcceptanceSLAMissed { get; set; }

        public double AcceptanceSLAComplianceRate { get; set; }

        public int FirstFeedbackSLAMet { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }

        public double FirstFeedbackSLAComplianceRate { get; set; }

        public int NextFeedbackSLAMet { get; set; }

        public int NextFeedbackSLAMissed { get; set; }

        public double NextFeedbackSLAComplianceRate { get; set; }

        public int FollowUpsCompletedOnTime { get; set; }

        public int FollowUpsCompletedLate { get; set; }

        public int OverdueFollowUps { get; set; }

        public double FollowUpTimelinessRate { get; set; }

        public double CompletionRate { get; set; }

        public double PerformanceScore { get; set; }
    }
}