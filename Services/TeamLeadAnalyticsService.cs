using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class TeamLeadAnalyticsService
        : ITeamLeadAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public TeamLeadAnalyticsService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // Performance Trend
        // =========================================================

        public async Task<TeamLeadPerformanceTrendViewModel?>
            GetPerformanceTrendAsync(
                long teamLeadId,
                DateTime fromDate,
                DateTime toDate)
        {
            // -----------------------------------------------------
            // Validate Team Lead
            // -----------------------------------------------------

            var teamLead =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == teamLeadId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.TeamLead &&
                        u.IsActive &&
                        !u.IsDeleted);

            if (teamLead == null)
            {
                return null;
            }


            // -----------------------------------------------------
            // Normalize Date Range
            // -----------------------------------------------------

            fromDate = fromDate.Date;
            toDate = toDate.Date;

            if (fromDate > toDate)
            {
                var temp = fromDate;

                fromDate = toDate;
                toDate = temp;
            }


            var performanceEndDate =
                toDate.AddDays(1);


            // =====================================================
            // Team Lead Target
            // =====================================================

            var teamLeadTargets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsDeleted &&
                        t.UserId == teamLeadId)
                    .OrderByDescending(t =>
                        t.StartDate)
                    .ToListAsync();


            var teamTarget =
                teamLeadTargets
                    .FirstOrDefault(t =>
                        t.StartDate.Date <= toDate &&
                        t.EndDate.Date >= fromDate);


            var teamTargetCount =
                teamTarget?.TargetCount ?? 0;


            // =====================================================
            // Get Team Sales Officers
            // =====================================================

            var officerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.TeamLeadId == teamLeadId &&
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.SalesOfficer)
                    .Select(u => u.UserId)
                    .ToListAsync();


            // =====================================================
            // Load Lead Assignments
            // =====================================================

            var assignments =
                officerIds.Count == 0
                    ? new List<ReportAssignmentData>()
                    : await _context.LeadAssignments
                        .AsNoTracking()
                        .Where(a =>
                            officerIds.Contains(
                                a.SalesOfficerId) &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.AssignedAt >= fromDate &&

                            a.AssignedAt <
                                performanceEndDate &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted)
                        .Select(a =>
                            new ReportAssignmentData
                            {
                                AssignmentId =
                                    a.AssignmentId,

                                SalesOfficerId =
                                    a.SalesOfficerId,

                                LeadId =
                                    a.LeadId,

                                AssignedAt =
                                    a.AssignedAt,

                                AcceptedAt =
                                    a.AcceptedAt,

                                AcceptanceSLAMissed =
                                    a.AcceptanceSLAMissed,

                                AssignmentStatus =
                                    a.AssignmentStatus,

                                LeadStatus =
                                    a.Lead!.Status
                            })
                        .ToListAsync();


            // =====================================================
            // Load Feedbacks
            // =====================================================

            var assignmentIds =
                assignments
                    .Select(a => a.AssignmentId)
                    .ToList();


            var feedbacks =
                assignmentIds.Count == 0
                    ? new List<FeedbackTrendData>()
                    : await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            assignmentIds.Contains(
                                f.AssignmentId) &&

                            f.IsActive &&

                            !f.IsDeleted)
                        .Select(f =>
                            new FeedbackTrendData
                            {
                                FeedbackId =
                                    f.FeedbackId,

                                AssignmentId =
                                    f.AssignmentId,

                                SubmittedAt =
                                    f.SubmittedAt,

                                NextFeedbackSLAMissed =
                                    f.NextFeedbackSLAMissed,

                                NextFollowUpDate =
                                    f.NextFollowUpDate
                            })
                        .ToListAsync();


            // =====================================================
            // Team Completed Leads
            // =====================================================

            var teamCompletedLeads =
                officerIds.Count == 0
                    ? 0
                    : await _context.LeadAssignments
                        .AsNoTracking()
                        .Where(a =>
                            officerIds.Contains(
                                a.SalesOfficerId) &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted &&

                            a.Lead.Status ==
                                LeadStatus.Completed &&

                            a.Lead.UpdatedAt >=
                                fromDate &&

                            a.Lead.UpdatedAt <
                                performanceEndDate)
                        .Select(a => a.LeadId)
                        .Distinct()
                        .CountAsync();


            // =====================================================
            // Target Achievement
            // =====================================================

            var targetAchievementPercentage =
                teamTargetCount <= 0
                    ? 0
                    : teamCompletedLeads *
                      100.0 /
                      teamTargetCount;


            // =====================================================
            // Build Weekly Trend
            // =====================================================

            var trendPoints =
                new List<PerformanceTrendPointViewModel>();


            var currentStart =
                fromDate;


            while (currentStart < performanceEndDate)
            {
                var currentEnd =
                    currentStart.AddDays(7);

                if (currentEnd > performanceEndDate)
                {
                    currentEnd =
                        performanceEndDate;
                }


                // -------------------------------------------------
                // Current Period Assignments
                // -------------------------------------------------

                var periodAssignments =
                    assignments
                        .Where(a =>
                            a.AssignedAt >= currentStart &&
                            a.AssignedAt < currentEnd)
                        .ToList();


                var periodAssignmentIds =
                    periodAssignments
                        .Select(a =>
                            a.AssignmentId)
                        .ToHashSet();


                var periodFeedbacks =
                    feedbacks
                        .Where(f =>
                            periodAssignmentIds.Contains(
                                f.AssignmentId))
                        .ToList();


                // -------------------------------------------------
                // Lead Counts
                // -------------------------------------------------

                var assignedCount =
                    periodAssignments
                        .Select(a => a.LeadId)
                        .Distinct()
                        .Count();


                var acceptedCount =
                    periodAssignments
                        .Where(a =>
                            a.AcceptedAt.HasValue)
                        .Select(a => a.LeadId)
                        .Distinct()
                        .Count();


                var completedCount =
                    periodAssignments
                        .Where(a =>
                            a.LeadStatus ==
                                LeadStatus.Completed)
                        .Select(a => a.LeadId)
                        .Distinct()
                        .Count();


                // -------------------------------------------------
                // Acceptance Rate
                // -------------------------------------------------

                var acceptanceRate =
                    assignedCount == 0
                        ? 0
                        : acceptedCount *
                          100.0 /
                          assignedCount;


                // -------------------------------------------------
                // Acceptance SLA
                // -------------------------------------------------

                var acceptanceSLAPercentage =
                    periodAssignments.Count == 0
                        ? 0
                        : periodAssignments.Count(a =>
                            !a.AcceptanceSLAMissed) *
                          100.0 /
                          periodAssignments.Count;


                // -------------------------------------------------
                // First Feedback SLA
                //
                // Rule:
                // First feedback must be submitted within
                // 3 hours after assignment.
                // -------------------------------------------------

                var firstFeedbackEligible =
                    periodAssignments
                        .Where(a =>
                            a.AcceptedAt.HasValue)
                        .ToList();


                var firstFeedbackSLAMet =
                    firstFeedbackEligible
                        .Count(a =>
                            IsFirstFeedbackWithinThreeHours(
                                a,
                                periodFeedbacks));


                var firstFeedbackSLAPercentage =
                    firstFeedbackEligible.Count == 0
                        ? 0
                        : firstFeedbackSLAMet *
                          100.0 /
                          firstFeedbackEligible.Count;


                // -------------------------------------------------
                // Next Feedback SLA
                // -------------------------------------------------

                var nextFeedbackEligible =
                    periodFeedbacks
                        .Where(f =>
                            f.NextFollowUpDate.HasValue)
                        .ToList();


                var nextFeedbackSLAPercentage =
                    nextFeedbackEligible.Count == 0
                        ? 0
                        : nextFeedbackEligible.Count(f =>
                            !f.NextFeedbackSLAMissed) *
                          100.0 /
                          nextFeedbackEligible.Count;


                // -------------------------------------------------
                // Follow-up Timeliness
                // -------------------------------------------------

                var followUpTimeliness =
                    CalculateFollowUpTimeliness(
                        periodFeedbacks);


                // -------------------------------------------------
                // Completion Rate
                // -------------------------------------------------

                var completionRate =
                    assignedCount == 0
                        ? 0
                        : completedCount *
                          100.0 /
                          assignedCount;


                // -------------------------------------------------
                // Performance Score
                // -------------------------------------------------

                var performanceScore =
                    CalculatePerformanceScore(
                        acceptanceSLAPercentage,
                        firstFeedbackSLAPercentage,
                        nextFeedbackSLAPercentage,
                        followUpTimeliness,
                        acceptanceRate,
                        completionRate);


                // -------------------------------------------------
                // Trend Point
                // -------------------------------------------------

                trendPoints.Add(
                    new PerformanceTrendPointViewModel
                    {
                        Label =
                            currentStart.ToString(
                                "dd MMM"),

                        StartDate =
                            currentStart,

                        EndDate =
                            currentEnd.AddTicks(-1),

                        AssignedLeads =
                            assignedCount,

                        AcceptedLeads =
                            acceptedCount,

                        CompletedLeads =
                            completedCount,

                        AcceptanceSLAPercentage =
                            Math.Round(
                                acceptanceSLAPercentage,
                                2),

                        FirstFeedbackSLAPercentage =
                            Math.Round(
                                firstFeedbackSLAPercentage,
                                2),

                        NextFeedbackSLAPercentage =
                            Math.Round(
                                nextFeedbackSLAPercentage,
                                2),

                        FollowUpPercentage =
                            Math.Round(
                                followUpTimeliness,
                                2),

                        PerformanceScore =
                            Math.Round(
                                performanceScore,
                                2)
                    });


                currentStart =
                    currentEnd;
            }


            // =====================================================
            // Final ViewModel
            // =====================================================

            return new TeamLeadPerformanceTrendViewModel
            {
                TeamLeadName =
                    teamLead.FullName,

                FromDate =
                    fromDate,

                ToDate =
                    toDate,

                TrendPoints =
                    trendPoints,

                TeamTarget =
                    teamTargetCount,

                TeamCompletedLeads =
                    teamCompletedLeads,

                TargetAchievementPercentage =
                    Math.Round(
                        targetAchievementPercentage,
                        2)
            };
        }


        // =========================================================
        // Team Lead Report
        // =========================================================

        public async Task<TeamLeadReportViewModel?>
            GetReportAsync(
                long teamLeadId,
                DateTime fromDate,
                DateTime toDate)
        {
            // -----------------------------------------------------
            // Validate Team Lead
            // -----------------------------------------------------

            var teamLead =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == teamLeadId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.TeamLead &&
                        u.IsActive &&
                        !u.IsDeleted);

            if (teamLead == null)
            {
                return null;
            }


            // -----------------------------------------------------
            // Normalize Date Range
            // -----------------------------------------------------

            fromDate =
                fromDate.Date;

            toDate =
                toDate.Date;


            if (fromDate > toDate)
            {
                var temp =
                    fromDate;

                fromDate =
                    toDate;

                toDate =
                    temp;
            }


            var endDateExclusive =
                toDate.AddDays(1);


            // =====================================================
            // Get Team Sales Officers
            // =====================================================

            var officers =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.TeamLeadId ==
                            teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                            RoleKeys.SalesOfficer)
                    .Select(u => new
                    {
                        u.UserId,
                        u.FullName
                    })
                    .ToListAsync();


            var officerIds =
                officers
                    .Select(o => o.UserId)
                    .ToList();


            // =====================================================
            // Load Assignments
            // =====================================================

            var assignments =
                officerIds.Count == 0
                    ? new List<ReportAssignmentData>()
                    : await _context.LeadAssignments
                        .AsNoTracking()
                        .Where(a =>
                            officerIds.Contains(
                                a.SalesOfficerId) &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.AssignedAt >=
                                fromDate &&

                            a.AssignedAt <
                                endDateExclusive &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted)
                        .Select(a =>
                            new ReportAssignmentData
                            {
                                AssignmentId =
                                    a.AssignmentId,

                                SalesOfficerId =
                                    a.SalesOfficerId,

                                LeadId =
                                    a.LeadId,

                                AssignedAt =
                                    a.AssignedAt,

                                AcceptedAt =
                                    a.AcceptedAt,

                                AcceptanceSLAMissed =
                                    a.AcceptanceSLAMissed,

                                AssignmentStatus =
                                    a.AssignmentStatus,

                                LeadStatus =
                                    a.Lead!.Status
                            })
                        .ToListAsync();


            // =====================================================
            // Load Feedbacks
            // =====================================================

            var assignmentIds =
                assignments
                    .Select(a =>
                        a.AssignmentId)
                    .ToList();


            var feedbacks =
                assignmentIds.Count == 0
                    ? new List<ReportFeedbackData>()
                    : await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            assignmentIds.Contains(
                                f.AssignmentId) &&

                            f.IsActive &&

                            !f.IsDeleted)
                        .Select(f =>
                            new ReportFeedbackData
                            {
                                FeedbackId =
                                    f.FeedbackId,

                                AssignmentId =
                                    f.AssignmentId,

                                SubmittedAt =
                                    f.SubmittedAt,

                                NextFollowUpDate =
                                    f.NextFollowUpDate,

                                NextFeedbackSLAMissed =
                                    f.NextFeedbackSLAMissed
                            })
                        .ToListAsync();


            // =====================================================
            // Team Target
            // =====================================================

            var teamTargets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsDeleted &&
                        t.UserId == teamLeadId)
                    .OrderByDescending(t =>
                        t.StartDate)
                    .ToListAsync();


            var teamTarget =
                teamTargets
                    .FirstOrDefault(t =>
                        t.StartDate.Date <= toDate &&
                        t.EndDate.Date >= fromDate);


            var teamTargetCount =
                teamTarget?.TargetCount ?? 0;


            // =====================================================
            // Overall Team Statistics
            // =====================================================

            var totalAssignedLeads =
                assignments
                    .Select(a =>
                        a.LeadId)
                    .Distinct()
                    .Count();


            var totalAcceptedLeads =
                assignments
                    .Where(a =>
                        a.AcceptedAt.HasValue)
                    .Select(a =>
                        a.LeadId)
                    .Distinct()
                    .Count();


            var totalCompletedLeads =
                assignments
                    .Where(a =>
                        a.LeadStatus ==
                            LeadStatus.Completed)
                    .Select(a =>
                        a.LeadId)
                    .Distinct()
                    .Count();


            var totalFeedbacks =
                feedbacks.Count;


            // =====================================================
            // Acceptance Rate
            // =====================================================

            var acceptanceRate =
                totalAssignedLeads == 0
                    ? 0
                    : totalAcceptedLeads *
                      100.0 /
                      totalAssignedLeads;


            // =====================================================
            // Follow-up Statistics
            // =====================================================

            var followUpFeedbacks =
                feedbacks
                    .Where(f =>
                        f.NextFollowUpDate.HasValue)
                    .ToList();


            var now =
                DateTime.UtcNow;


            var totalPendingFollowUps =
                followUpFeedbacks
                    .Count(f =>
                        f.NextFollowUpDate!.Value >=
                            now);


            var totalOverdueFollowUps =
                followUpFeedbacks
                    .Count(f =>
                        f.NextFollowUpDate!.Value <
                            now);


            // =====================================================
            // Acceptance SLA
            // =====================================================

            var acceptanceSLAMet =
                assignments.Count(a =>
                    !a.AcceptanceSLAMissed);


            var acceptanceSLAComplianceRate =
                assignments.Count == 0
                    ? 0
                    : acceptanceSLAMet *
                      100.0 /
                      assignments.Count;


            // =====================================================
            // First Feedback SLA
            //
            // Accepted assignments are eligible.
            //
            // First feedback must be within:
            //
            // AssignedAt + 3 Hours
            // =====================================================

            var firstFeedbackEligible =
                assignments
                    .Where(a =>
                        a.AcceptedAt.HasValue)
                    .ToList();


            var firstFeedbackSLAMet =
                firstFeedbackEligible
                    .Count(a =>
                        IsFirstFeedbackWithinThreeHours(
                            a,
                            feedbacks));


            var firstFeedbackSLAMissed =
                firstFeedbackEligible.Count -
                firstFeedbackSLAMet;


            var firstFeedbackSLAComplianceRate =
                firstFeedbackEligible.Count == 0
                    ? 0
                    : firstFeedbackSLAMet *
                      100.0 /
                      firstFeedbackEligible.Count;


            // =====================================================
            // Next Feedback SLA
            // =====================================================

            var nextFeedbackEligible =
                feedbacks
                    .Where(f =>
                        f.NextFollowUpDate.HasValue)
                    .ToList();


            var nextFeedbackSLAMet =
                nextFeedbackEligible
                    .Count(f =>
                        !f.NextFeedbackSLAMissed);


            var nextFeedbackSLAMissed =
                nextFeedbackEligible.Count -
                nextFeedbackSLAMet;


            var nextFeedbackSLAComplianceRate =
                nextFeedbackEligible.Count == 0
                    ? 0
                    : nextFeedbackSLAMet *
                      100.0 /
                      nextFeedbackEligible.Count;


            // =====================================================
            // Follow-up Timeliness
            // =====================================================

            var followUpMetrics =
                CalculateFollowUpMetrics(
                    feedbacks);


            var followUpTimelinessRate =
                followUpMetrics.OnTimeRate;


            // =====================================================
            // Completion Rate
            // =====================================================

            var completionRate =
                totalAssignedLeads == 0
                    ? 0
                    : totalCompletedLeads *
                      100.0 /
                      totalAssignedLeads;


            // =====================================================
            // Target Achievement
            // =====================================================

            var targetAchievementPercentage =
                teamTargetCount <= 0
                    ? 0
                    : totalCompletedLeads *
                      100.0 /
                      teamTargetCount;


            // =====================================================
            // Weighted Performance Score
            // =====================================================

            var performanceScore =
                CalculatePerformanceScore(
                    acceptanceSLAComplianceRate,
                    firstFeedbackSLAComplianceRate,
                    nextFeedbackSLAComplianceRate,
                    followUpTimelinessRate,
                    acceptanceRate,
                    completionRate);


            // =====================================================
            // Officer-wise Reports
            // =====================================================

            var officerReports =
                new List<TeamLeadOfficerReportViewModel>();


            foreach (var officer in officers)
            {
                var officerAssignments =
                    assignments
                        .Where(a =>
                            a.SalesOfficerId ==
                                officer.UserId)
                        .ToList();


                var officerAssignmentIds =
                    officerAssignments
                        .Select(a =>
                            a.AssignmentId)
                        .ToHashSet();


                var officerFeedbacks =
                    feedbacks
                        .Where(f =>
                            officerAssignmentIds.Contains(
                                f.AssignmentId))
                        .ToList();


                // -------------------------------------------------
                // Lead Statistics
                // -------------------------------------------------

                var officerAssigned =
                    officerAssignments
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .Count();


                var officerAccepted =
                    officerAssignments
                        .Where(a =>
                            a.AcceptedAt.HasValue)
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .Count();


                var officerCompleted =
                    officerAssignments
                        .Where(a =>
                            a.LeadStatus ==
                                LeadStatus.Completed)
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .Count();


                // -------------------------------------------------
                // Acceptance Rate
                // -------------------------------------------------

                var officerAcceptanceRate =
                    officerAssigned == 0
                        ? 0
                        : officerAccepted *
                          100.0 /
                          officerAssigned;


                // -------------------------------------------------
                // Acceptance SLA
                // -------------------------------------------------

                var officerAcceptanceSLAMet =
                    officerAssignments
                        .Count(a =>
                            !a.AcceptanceSLAMissed);


                var officerAcceptanceSLAMissed =
                    officerAssignments
                        .Count(a =>
                            a.AcceptanceSLAMissed);


                var officerAcceptanceSLAComplianceRate =
                    officerAssignments.Count == 0
                        ? 0
                        : officerAcceptanceSLAMet *
                          100.0 /
                          officerAssignments.Count;


                // -------------------------------------------------
                // First Feedback SLA
                // -------------------------------------------------

                var officerFirstFeedbackEligible =
                    officerAssignments
                        .Where(a =>
                            a.AcceptedAt.HasValue)
                        .ToList();


                var officerFirstFeedbackMet =
                    officerFirstFeedbackEligible
                        .Count(a =>
                            IsFirstFeedbackWithinThreeHours(
                                a,
                                officerFeedbacks));


                var officerFirstFeedbackMissed =
                    officerFirstFeedbackEligible.Count -
                    officerFirstFeedbackMet;


                var officerFirstFeedbackRate =
                    officerFirstFeedbackEligible.Count == 0
                        ? 0
                        : officerFirstFeedbackMet *
                          100.0 /
                          officerFirstFeedbackEligible.Count;


                // -------------------------------------------------
                // Next Feedback SLA
                // -------------------------------------------------

                var officerNextFeedbackEligible =
                    officerFeedbacks
                        .Where(f =>
                            f.NextFollowUpDate.HasValue)
                        .ToList();


                var officerNextFeedbackMet =
                    officerNextFeedbackEligible
                        .Count(f =>
                            !f.NextFeedbackSLAMissed);


                var officerNextFeedbackMissed =
                    officerNextFeedbackEligible.Count -
                    officerNextFeedbackMet;


                var officerNextFeedbackRate =
                    officerNextFeedbackEligible.Count == 0
                        ? 0
                        : officerNextFeedbackMet *
                          100.0 /
                          officerNextFeedbackEligible.Count;


                // -------------------------------------------------
                // Follow-up Timeliness
                // -------------------------------------------------

                var officerFollowUpMetrics =
                    CalculateFollowUpMetrics(
                        officerFeedbacks);


                // -------------------------------------------------
                // Completion Rate
                // -------------------------------------------------

                var officerCompletionRate =
                    officerAssigned == 0
                        ? 0
                        : officerCompleted *
                          100.0 /
                          officerAssigned;


                // -------------------------------------------------
                // Performance Score
                // -------------------------------------------------

                var officerPerformanceScore =
                    CalculatePerformanceScore(
                        officerAcceptanceSLAComplianceRate,
                        officerFirstFeedbackRate,
                        officerNextFeedbackRate,
                        officerFollowUpMetrics.OnTimeRate,
                        officerAcceptanceRate,
                        officerCompletionRate);


                officerReports.Add(
                    new TeamLeadOfficerReportViewModel
                    {
                        SalesOfficerId =
                            officer.UserId,

                        SalesOfficerName =
                            officer.FullName,

                        TotalAssignedLeads =
                            officerAssigned,

                        AcceptedLeads =
                            officerAccepted,

                        CompletedLeads =
                            officerCompleted,

                        TotalFeedbacks =
                            officerFeedbacks.Count,

                        AcceptanceRate =
                            Math.Round(
                                officerAcceptanceRate,
                                2),

                        AcceptanceSLAMet =
                            officerAcceptanceSLAMet,

                        AcceptanceSLAMissed =
                            officerAcceptanceSLAMissed,

                        AcceptanceSLAComplianceRate =
                            Math.Round(
                                officerAcceptanceSLAComplianceRate,
                                2),

                        FirstFeedbackSLAMet =
                            officerFirstFeedbackMet,

                        FirstFeedbackSLAMissed =
                            officerFirstFeedbackMissed,

                        FirstFeedbackSLAComplianceRate =
                            Math.Round(
                                officerFirstFeedbackRate,
                                2),

                        NextFeedbackSLAMet =
                            officerNextFeedbackMet,

                        NextFeedbackSLAMissed =
                            officerNextFeedbackMissed,

                        NextFeedbackSLAComplianceRate =
                            Math.Round(
                                officerNextFeedbackRate,
                                2),

                        FollowUpsCompletedOnTime =
                            officerFollowUpMetrics.OnTime,

                        FollowUpsCompletedLate =
                            officerFollowUpMetrics.Late,

                        OverdueFollowUps =
                            officerFollowUpMetrics.Overdue,

                        FollowUpTimelinessRate =
                            Math.Round(
                                officerFollowUpMetrics.OnTimeRate,
                                2),

                        CompletionRate =
                            Math.Round(
                                officerCompletionRate,
                                2),

                        PerformanceScore =
                            Math.Round(
                                officerPerformanceScore,
                                2)
                    });
            }


            // =====================================================
            // Final Report ViewModel
            // =====================================================

            return new TeamLeadReportViewModel
            {
                TeamLeadName =
                    teamLead.FullName,

                FromDate =
                    fromDate,

                ToDate =
                    toDate,

                TotalSalesOfficers =
                    officers.Count,

                TotalAssignedLeads =
                    totalAssignedLeads,

                TotalAcceptedLeads =
                    totalAcceptedLeads,

                TotalCompletedLeads =
                    totalCompletedLeads,

                TotalFeedbacks =
                    totalFeedbacks,

                TotalPendingFollowUps =
                    totalPendingFollowUps,

                TotalOverdueFollowUps =
                    totalOverdueFollowUps,

                TeamTarget =
                    teamTargetCount,

                TargetCompletedLeads =
                    totalCompletedLeads,

                TargetAchievementPercentage =
                    Math.Round(
                        targetAchievementPercentage,
                        2),

                AcceptanceRate =
                    Math.Round(
                        acceptanceRate,
                        2),

                AcceptanceSLAComplianceRate =
                    Math.Round(
                        acceptanceSLAComplianceRate,
                        2),

                FirstFeedbackSLAComplianceRate =
                    Math.Round(
                        firstFeedbackSLAComplianceRate,
                        2),

                NextFeedbackSLAComplianceRate =
                    Math.Round(
                        nextFeedbackSLAComplianceRate,
                        2),

                FollowUpTimelinessRate =
                    Math.Round(
                        followUpTimelinessRate,
                        2),

                CompletionRate =
                    Math.Round(
                        completionRate,
                        2),

                PerformanceScore =
                    Math.Round(
                        performanceScore,
                        2),

                OfficerReports =
                    officerReports
                        .OrderByDescending(x =>
                            x.PerformanceScore)
                        .ToList()
            };
        }


        // =========================================================
        // First Feedback SLA
        // =========================================================
        //
        // Business Rule:
        //
        // First feedback must be submitted within 3 hours
        // after lead assignment.
        //
        // Important:
        // We select the earliest feedback submitted for the
        // assignment and compare it with AssignedAt + 3 hours.
        // =========================================================

        private static bool
            IsFirstFeedbackWithinThreeHours(
                ReportAssignmentData assignment,
                IEnumerable<ReportFeedbackData> feedbacks)
        {
            var firstFeedback =
                feedbacks
                    .Where(f =>
                        f.AssignmentId ==
                            assignment.AssignmentId)
                    .OrderBy(f =>
                        f.SubmittedAt)
                    .FirstOrDefault();


            if (firstFeedback == null)
            {
                return false;
            }


            var deadline =
                assignment.AssignedAt
                    .AddHours(3);


            return firstFeedback.SubmittedAt <=
                   deadline;
        }


        // =========================================================
        // First Feedback SLA - Trend DTO version
        // =========================================================

        private static bool
            IsFirstFeedbackWithinThreeHours(
                ReportAssignmentData assignment,
                IEnumerable<FeedbackTrendData> feedbacks)
        {
            var firstFeedback =
                feedbacks
                    .Where(f =>
                        f.AssignmentId ==
                            assignment.AssignmentId)
                    .OrderBy(f =>
                        f.SubmittedAt)
                    .FirstOrDefault();


            if (firstFeedback == null)
            {
                return false;
            }


            var deadline =
                assignment.AssignedAt
                    .AddHours(3);


            return firstFeedback.SubmittedAt <=
                   deadline;
        }


        // =========================================================
        // Follow-up Timeliness
        // =========================================================
        //
        // Rule:
        //
        // Feedback #1:
        //     NextFollowUpDate = June 10
        //
        // Feedback #2:
        //     SubmittedAt = June 9
        //
        // Result:
        //     On Time
        //
        // If Feedback #2 is submitted after June 10:
        //     Late
        //
        // The last feedback does not count as completed follow-up
        // because there is no subsequent feedback yet.
        // =========================================================

        private static FollowUpMetrics
            CalculateFollowUpMetrics(
                IEnumerable<ReportFeedbackData> feedbacks)
        {
            var orderedFeedbacks =
                feedbacks
                    .OrderBy(f =>
                        f.AssignmentId)
                    .ThenBy(f =>
                        f.SubmittedAt)
                    .ToList();


            var onTime =
                0;

            var late =
                0;

            var overdue =
                0;


            var now =
                DateTime.UtcNow;


            var groupedFeedbacks =
                orderedFeedbacks
                    .GroupBy(f =>
                        f.AssignmentId);


            foreach (var group in groupedFeedbacks)
            {
                var feedbackList =
                    group
                        .OrderBy(f =>
                            f.SubmittedAt)
                        .ToList();


                for (var i = 0;
                     i < feedbackList.Count - 1;
                     i++)
                {
                    var currentFeedback =
                        feedbackList[i];

                    var nextFeedback =
                        feedbackList[i + 1];


                    if (!currentFeedback
                            .NextFollowUpDate
                            .HasValue)
                    {
                        continue;
                    }


                    if (nextFeedback.SubmittedAt <=
                        currentFeedback
                            .NextFollowUpDate
                            .Value)
                    {
                        onTime++;
                    }
                    else
                    {
                        late++;
                    }
                }


                // -------------------------------------------------
                // Last scheduled follow-up
                // -------------------------------------------------

                var lastFeedback =
                    feedbackList.LastOrDefault();


                if (lastFeedback != null &&
                    lastFeedback
                        .NextFollowUpDate
                        .HasValue &&
                    lastFeedback
                        .NextFollowUpDate
                        .Value < now)
                {
                    overdue++;
                }
            }


            var completedFollowUps =
                onTime + late;


            var onTimeRate =
                completedFollowUps == 0
                    ? 0
                    : onTime *
                      100.0 /
                      completedFollowUps;


            return new FollowUpMetrics
            {
                OnTime =
                    onTime,

                Late =
                    late,

                Overdue =
                    overdue,

                OnTimeRate =
                    onTimeRate
            };
        }


        // =========================================================
        // Follow-up Timeliness - Trend Version
        // =========================================================

        private static double
            CalculateFollowUpTimeliness(
                IEnumerable<FeedbackTrendData> feedbacks)
        {
            var orderedFeedbacks =
                feedbacks
                    .OrderBy(f =>
                        f.AssignmentId)
                    .ThenBy(f =>
                        f.SubmittedAt)
                    .ToList();


            var onTime =
                0;

            var completedFollowUps =
                0;


            foreach (var group in
                     orderedFeedbacks.GroupBy(
                         f => f.AssignmentId))
            {
                var feedbackList =
                    group
                        .OrderBy(f =>
                            f.SubmittedAt)
                        .ToList();


                for (var i = 0;
                     i < feedbackList.Count - 1;
                     i++)
                {
                    var currentFeedback =
                        feedbackList[i];

                    var nextFeedback =
                        feedbackList[i + 1];


                    if (!currentFeedback
                            .NextFollowUpDate
                            .HasValue)
                    {
                        continue;
                    }


                    completedFollowUps++;


                    if (nextFeedback.SubmittedAt <=
                        currentFeedback
                            .NextFollowUpDate
                            .Value)
                    {
                        onTime++;
                    }
                }
            }


            return completedFollowUps == 0
                ? 0
                : onTime *
                  100.0 /
                  completedFollowUps;
        }


        // =========================================================
        // Weighted Performance Score
        // =========================================================
        //
        // Completion Rate          = 25%
        // First Feedback SLA       = 20%
        // Follow-up Timeliness     = 20%
        // Acceptance SLA           = 15%
        // Next Feedback SLA        = 10%
        // Acceptance Rate          = 10%
        //
        // Total                    = 100%
        // =========================================================

        private static double
            CalculatePerformanceScore(
                double acceptanceSla,
                double firstFeedbackSla,
                double nextFeedbackSla,
                double followUpTimeliness,
                double acceptanceRate,
                double completionRate)
        {
            return
                (completionRate * 0.25) +

                (firstFeedbackSla * 0.20) +

                (followUpTimeliness * 0.20) +

                (acceptanceSla * 0.15) +

                (nextFeedbackSla * 0.10) +

                (acceptanceRate * 0.10);
        }


        // =========================================================
        // Internal Feedback DTO
        // =========================================================

        private class FeedbackTrendData
        {
            public long FeedbackId { get; set; }

            public long AssignmentId { get; set; }

            public DateTime SubmittedAt { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public bool NextFeedbackSLAMissed { get; set; }
        }


        // =========================================================
        // Internal Report Assignment DTO
        // =========================================================

        private class ReportAssignmentData
        {
            public long AssignmentId { get; set; }

            public long SalesOfficerId { get; set; }

            public long LeadId { get; set; }

            public DateTime AssignedAt { get; set; }

            public DateTime? AcceptedAt { get; set; }

            public bool AcceptanceSLAMissed { get; set; }

            public AssignmentStatus AssignmentStatus { get; set; }

            public LeadStatus LeadStatus { get; set; }
        }


        // =========================================================
        // Internal Report Feedback DTO
        // =========================================================

        private class ReportFeedbackData
        {
            public long FeedbackId { get; set; }

            public long AssignmentId { get; set; }

            public DateTime SubmittedAt { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public bool NextFeedbackSLAMissed { get; set; }
        }


        // =========================================================
        // Follow-up Metrics DTO
        // =========================================================

        private class FollowUpMetrics
        {
            public int OnTime { get; set; }

            public int Late { get; set; }

            public int Overdue { get; set; }

            public double OnTimeRate { get; set; }
        }
    }
}