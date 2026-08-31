using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CRMSystem.Services
{
    public class SalesOfficerPerformanceService
        : ISalesOfficerPerformanceService
    {
        private readonly ApplicationDbContext _context;

        public SalesOfficerPerformanceService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // Get Performance Of All Active Sales Officers
        // =====================================================

        public async Task<List<SalesOfficerPerformanceViewModel>>
            GetPerformanceAsync(
                PerformanceFilterViewModel? filter = null)
        {
            // =================================================
            // 1. Resolve Date Range
            // =================================================

            var dateRange =
                ResolveDateRange(filter);

            var fromDate =
                dateRange.FromDate;

            var toDateExclusive =
                dateRange.ToDateExclusive;


            // =================================================
            // 2. Load Active Sales Officers
            // =================================================

            var salesOfficers =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleKey == "SALES_OFFICER" &&
                        u.IsActive &&
                        !u.IsDeleted)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new
                    {
                        u.UserId,

                        FullName =
                            (u.FirstName + " " +
                             (u.LastName ?? "")).Trim()
                    })
                    .ToListAsync();


            // =================================================
            // No Active Sales Officers
            // =================================================

            if (salesOfficers.Count == 0)
            {
                return new List<SalesOfficerPerformanceViewModel>();
            }


            // =================================================
            // 3. Sales Officer IDs
            // =================================================

            var salesOfficerIds =
                salesOfficers
                    .Select(u => u.UserId)
                    .ToList();


            // =================================================
            // 4. Load Lead Assignments
            //
            // Date filter is based on AssignedAt.
            // =================================================

            var assignments =
                await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        salesOfficerIds.Contains(
                            a.SalesOfficerId) &&

                        a.AssignedAt >= fromDate &&

                        a.AssignedAt < toDateExclusive)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        a.SalesOfficerId,
                        a.AssignedAt,
                        a.AcceptedAt,
                        a.AcceptanceSLAMissed,
                        a.FirstFeedbackSLAMissed,

                        LeadStatus =
                            a.Lead != null
                                ? a.Lead.Status
                                : default
                    })
                    .ToListAsync();


            // =================================================
            // 5. Load Feedbacks
            // =================================================

            var assignmentIds =
                assignments
                    .Select(a => a.AssignmentId)
                    .ToList();


            var feedbacks =
                assignmentIds.Count == 0
                    ? new List<FeedbackPerformanceData>()
                    : await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            assignmentIds.Contains(
                                f.AssignmentId))
                        .Select(f =>
                            new FeedbackPerformanceData
                            {
                                FeedbackId =
                                    f.FeedbackId,

                                AssignmentId =
                                    f.AssignmentId,

                                Status =
                                    f.Status,

                                SubmittedAt =
                                    f.SubmittedAt,

                                NextFollowUpDate =
                                    f.NextFollowUpDate,

                                NextFeedbackSLAMissed =
                                    f.NextFeedbackSLAMissed
                            })
                        .ToListAsync();


            // =================================================
            // 6. Current Time
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // 7. Final Performance Collection
            // =================================================

            var performance =
                new List<SalesOfficerPerformanceViewModel>();


            // =================================================
            // 8. Calculate Performance Officer By Officer
            // =================================================

            foreach (var officer in salesOfficers)
            {
                // =================================================
                // Officer Assignments
                // =================================================

                var officerAssignments =
                    assignments
                        .Where(a =>
                            a.SalesOfficerId ==
                            officer.UserId)
                        .ToList();


                // =================================================
                // Basic Lead Statistics
                // =================================================

                var totalAssignedLeads =
                    officerAssignments.Count;


                var acceptedLeads =
                    officerAssignments.Count(a =>
                        a.AcceptedAt.HasValue);


                var pendingAcceptance =
                    officerAssignments.Count(a =>
                        !a.AcceptedAt.HasValue);


                // =================================================
                // Acceptance Rate
                // =================================================

                var acceptanceRate =
                    totalAssignedLeads > 0
                        ? (double)acceptedLeads /
                          totalAssignedLeads * 100
                        : 0;


                // =================================================
                // Acceptance SLA
                // =================================================

                var acceptanceSLAMet = 0;

                var acceptanceSLAMissed = 0;


                foreach (var assignment in officerAssignments)
                {
                    if (assignment.AcceptedAt.HasValue)
                    {
                        var acceptanceDeadline =
                            assignment.AssignedAt.AddHours(1);


                        if (assignment.AcceptedAt.Value
                            <= acceptanceDeadline)
                        {
                            acceptanceSLAMet++;
                        }
                        else
                        {
                            acceptanceSLAMissed++;
                        }

                        continue;
                    }


                    if (assignment.AssignedAt.AddHours(1) < now)
                    {
                        acceptanceSLAMissed++;
                    }
                }


                var acceptanceSLAApplicable =
                    acceptanceSLAMet +
                    acceptanceSLAMissed;


                var acceptanceSLAComplianceRate =
                    acceptanceSLAApplicable > 0
                        ? (double)acceptanceSLAMet /
                          acceptanceSLAApplicable * 100
                        : 0;


                // =================================================
                // Officer Feedbacks
                // =================================================

                var officerAssignmentIds =
                    officerAssignments
                        .Select(a => a.AssignmentId)
                        .ToHashSet();


                var officerFeedbacks =
                    feedbacks
                        .Where(f =>
                            officerAssignmentIds.Contains(
                                f.AssignmentId))
                        .OrderBy(f => f.SubmittedAt)
                        .ToList();


                var totalFeedbacks =
                    officerFeedbacks.Count;


                // =================================================
                // First Feedback SLA
                // =================================================

                var firstFeedbackSLAMet = 0;

                var firstFeedbackSLAMissed = 0;


                foreach (var assignment in officerAssignments)
                {
                    var firstFeedback =
                        officerFeedbacks
                            .Where(f =>
                                f.AssignmentId ==
                                assignment.AssignmentId)
                            .OrderBy(f => f.SubmittedAt)
                            .FirstOrDefault();


                    if (firstFeedback != null)
                    {
                        var firstFeedbackDeadline =
                            assignment.AssignedAt.AddHours(3);


                        if (firstFeedback.SubmittedAt
                            <= firstFeedbackDeadline)
                        {
                            firstFeedbackSLAMet++;
                        }
                        else
                        {
                            firstFeedbackSLAMissed++;
                        }

                        continue;
                    }


                    if (assignment.AssignedAt.AddHours(3) < now)
                    {
                        firstFeedbackSLAMissed++;
                    }
                }


                var firstFeedbackSLAApplicable =
                    firstFeedbackSLAMet +
                    firstFeedbackSLAMissed;


                var firstFeedbackSLAComplianceRate =
                    firstFeedbackSLAApplicable > 0
                        ? (double)firstFeedbackSLAMet /
                          firstFeedbackSLAApplicable * 100
                        : 0;


                // =================================================
                // Completed Leads
                // =================================================

                var completedLeads =
                    officerAssignments.Count(a =>
                        a.LeadStatus ==
                        LeadStatus.Completed);


                // =================================================
                // Completion Rate
                //
                // Completed Leads / Accepted Leads * 100
                // =================================================

                var completionRate =
                    acceptedLeads > 0
                        ? (double)completedLeads /
                          acceptedLeads * 100
                        : 0;


                // =================================================
                // Next Feedback SLA
                // =================================================

                var nextFeedbackSLAMet = 0;

                var nextFeedbackSLAMissed = 0;


                foreach (var feedback in officerFeedbacks)
                {
                    if (!feedback.NextFollowUpDate.HasValue)
                    {
                        continue;
                    }


                    var expectedDate =
                        feedback.NextFollowUpDate.Value;


                    var nextFeedback =
                        officerFeedbacks
                            .Where(next =>
                                next.AssignmentId ==
                                    feedback.AssignmentId &&

                                next.SubmittedAt >
                                    feedback.SubmittedAt)
                            .OrderBy(next =>
                                next.SubmittedAt)
                            .FirstOrDefault();


                    if (nextFeedback != null)
                    {
                        if (nextFeedback.SubmittedAt
                            <= expectedDate)
                        {
                            nextFeedbackSLAMet++;
                        }
                        else
                        {
                            nextFeedbackSLAMissed++;
                        }

                        continue;
                    }


                    if (feedback.NextFeedbackSLAMissed)
                    {
                        nextFeedbackSLAMissed++;

                        continue;
                    }


                    if (expectedDate < now)
                    {
                        nextFeedbackSLAMissed++;
                    }
                }


                // =================================================
                // Next Feedback SLA Compliance Rate
                // =================================================

                var nextFeedbackSLAApplicable =
                    nextFeedbackSLAMet +
                    nextFeedbackSLAMissed;


                var nextFeedbackSLAComplianceRate =
                    nextFeedbackSLAApplicable > 0
                        ? (double)nextFeedbackSLAMet /
                          nextFeedbackSLAApplicable * 100
                        : 0;


                // =================================================
                // Follow-up Statistics
                // =================================================

                var followUps =
                    officerFeedbacks
                        .Where(f =>
                            f.NextFollowUpDate.HasValue)
                        .ToList();


                var followUpsCompletedOnTime = 0;

                var followUpsCompletedLate = 0;


                foreach (var feedback in followUps)
                {
                    var expectedDate =
                        feedback.NextFollowUpDate!.Value;


                    var nextFeedback =
                        officerFeedbacks
                            .Where(next =>
                                next.AssignmentId ==
                                    feedback.AssignmentId &&

                                next.SubmittedAt >
                                    feedback.SubmittedAt)
                            .OrderBy(next =>
                                next.SubmittedAt)
                            .FirstOrDefault();


                    if (nextFeedback == null)
                    {
                        continue;
                    }


                    if (nextFeedback.SubmittedAt
                        <= expectedDate)
                    {
                        followUpsCompletedOnTime++;
                    }
                    else
                    {
                        followUpsCompletedLate++;
                    }
                }


                // =================================================
                // Current Overdue Follow-ups
                // =================================================

                var overdueFollowUps =
                    followUps.Count(f =>
                        f.NextFollowUpDate!.Value < now &&

                        !officerFeedbacks.Any(next =>
                            next.AssignmentId ==
                                f.AssignmentId &&

                            next.SubmittedAt >
                                f.SubmittedAt));


                // =================================================
                // Follow-up Timeliness Rate
                // =================================================

                var followUpApplicable =
                    followUpsCompletedOnTime +
                    followUpsCompletedLate;


                var followUpTimelinessRate =
                    followUpApplicable > 0
                        ? (double)followUpsCompletedOnTime /
                          followUpApplicable * 100
                        : 0;


                // =================================================
                // Feedback Status Counts
                // =================================================

                var interestedCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Interested);


                var followUpRequiredCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.FollowUpRequired);


                var meetingScheduledCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.MeetingScheduled);


                var visitedCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Visited);


                var quotationSentCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.QuotationSent);


                var negotiationCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Negotiation);


                var completedCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Completed);


                var closedCount =
                    officerFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Closed);


                // =================================================
                // Create ViewModel
                // =================================================

                var officerPerformance =
    new SalesOfficerPerformanceViewModel
    {
        SalesOfficerId =
            officer.UserId,

        SalesOfficerName =
            officer.FullName,

        TotalAssignedLeads =
            totalAssignedLeads,

        AcceptedLeads =
            acceptedLeads,

        PendingAcceptance =
            pendingAcceptance,

        AcceptanceRate =
            acceptanceRate,

        AcceptanceSLAMet =
            acceptanceSLAMet,

        AcceptanceSLAMissed =
            acceptanceSLAMissed,

        AcceptanceSLAComplianceRate =
            acceptanceSLAComplianceRate,

        FirstFeedbackSLAMet =
            firstFeedbackSLAMet,

        FirstFeedbackSLAMissed =
            firstFeedbackSLAMissed,

        FirstFeedbackSLAComplianceRate =
            firstFeedbackSLAComplianceRate,

        NextFeedbackSLAMet =
            nextFeedbackSLAMet,

        NextFeedbackSLAMissed =
            nextFeedbackSLAMissed,

        NextFeedbackSLAComplianceRate =
            nextFeedbackSLAComplianceRate,

        CompletedLeads =
            completedLeads,

        CompletionRate =
            completionRate,

        TotalFeedbacks =
            totalFeedbacks,

        FollowUpsCompletedOnTime =
            followUpsCompletedOnTime,

        FollowUpsCompletedLate =
            followUpsCompletedLate,

        OverdueFollowUps =
            overdueFollowUps,

        FollowUpTimelinessRate =
            followUpTimelinessRate,

        InterestedCount =
            interestedCount,

        FollowUpRequiredCount =
            followUpRequiredCount,

        MeetingScheduledCount =
            meetingScheduledCount,

        VisitedCount =
            visitedCount,

        QuotationSentCount =
            quotationSentCount,

        NegotiationCount =
            negotiationCount,

        CompletedCount =
            completedCount,

        ClosedCount =
            closedCount
    };


                // =====================================================
                // Calculate Weighted Performance Score
                // =====================================================

                officerPerformance.PerformanceScore =
                    CalculatePerformanceScore(
                        officerPerformance);


                // =====================================================
                // Add Final Officer Performance
                // =====================================================

                performance.Add(
                    officerPerformance);
            }


            return performance;
        }


        // =====================================================
        // Resolve Performance Date Range
        // =====================================================

        private static (
            DateTime FromDate,
            DateTime ToDateExclusive)
            ResolveDateRange(
                PerformanceFilterViewModel? filter)
        {
            var today =
                DateTime.UtcNow.Date;


            var range =
                filter?.Range?.Trim();


            // =================================================
            // Today
            // =================================================

            if (string.Equals(
                    range,
                    "Today",
                    StringComparison.OrdinalIgnoreCase))
            {
                return (
                    today,
                    today.AddDays(1));
            }


            // =================================================
            // This Week
            // Monday -> Sunday
            // =================================================

            if (string.Equals(
                    range,
                    "ThisWeek",
                    StringComparison.OrdinalIgnoreCase))
            {
                var daysFromMonday =
                    ((int)today.DayOfWeek + 6) % 7;


                var weekStart =
                    today.AddDays(-daysFromMonday);


                var weekEnd =
                    weekStart.AddDays(7);


                return (
                    weekStart,
                    weekEnd);
            }


            // =================================================
            // Custom Range
            // =================================================

            if (string.Equals(
                    range,
                    "Custom",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (filter?.FromDate.HasValue == true &&
                    filter.ToDate.HasValue &&
                    filter.FromDate.Value.Date
                        <= filter.ToDate.Value.Date)
                {
                    var customFrom =
                        filter.FromDate.Value.Date;


                    var customToExclusive =
                        filter.ToDate.Value.Date.AddDays(1);


                    return (
                        customFrom,
                        customToExclusive);
                }
            }


            // =================================================
            // Default: This Month
            // =================================================

            var monthStart =
                new DateTime(
                    today.Year,
                    today.Month,
                    1);


            var nextMonthStart =
                monthStart.AddMonths(1);


            return (
                monthStart,
                nextMonthStart);
        }


        // =====================================================
        // Get Performance Of A Single Sales Officer
        // =====================================================

        public async Task<SalesOfficerPerformanceViewModel?>
            GetPerformanceAsync(
                long salesOfficerId,
                PerformanceFilterViewModel? filter = null)
        {
            var allPerformance =
                await GetPerformanceAsync(filter);


            return allPerformance
                .FirstOrDefault(p =>
                    p.SalesOfficerId ==
                    salesOfficerId);
        }


        // =====================================================
        // Get Performance Trend
        // =====================================================

        public async Task<List<PerformanceTrendViewModel>>
            GetPerformanceTrendAsync(
                PerformanceFilterViewModel? filter = null)
        {
            // =================================================
            // 1. Resolve Date Range
            // =================================================

            var dateRange =
                ResolveDateRange(filter);


            var fromDate =
                dateRange.FromDate;


            var toDateExclusive =
                dateRange.ToDateExclusive;


            // =================================================
            // 2. Load Active Sales Officers
            // =================================================

            var salesOfficerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleKey == "SALES_OFFICER" &&
                        u.IsActive &&
                        !u.IsDeleted)
                    .Select(u => u.UserId)
                    .ToListAsync();


            if (salesOfficerIds.Count == 0)
            {
                return new List<PerformanceTrendViewModel>();
            }


            // =================================================
            // 3. Load Lead Assignments
            // =================================================

            var assignments =
                await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        salesOfficerIds.Contains(
                            a.SalesOfficerId) &&

                        a.AssignedAt >= fromDate &&

                        a.AssignedAt < toDateExclusive)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        a.SalesOfficerId,
                        a.AssignedAt,
                        a.AcceptedAt,

                        LeadStatus =
                            a.Lead != null
                                ? a.Lead.Status
                                : default
                    })
                    .ToListAsync();


            // =================================================
            // 4. Load Feedbacks
            // =================================================

            var assignmentIds =
                assignments
                    .Select(a => a.AssignmentId)
                    .ToList();


            var feedbacks =
                assignmentIds.Count == 0
                    ? new List<FeedbackPerformanceData>()
                    : await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            assignmentIds.Contains(
                                f.AssignmentId))
                        .Select(f =>
                            new FeedbackPerformanceData
                            {
                                FeedbackId =
                                    f.FeedbackId,

                                AssignmentId =
                                    f.AssignmentId,

                                Status =
                                    f.Status,

                                SubmittedAt =
                                    f.SubmittedAt,

                                NextFollowUpDate =
                                    f.NextFollowUpDate,

                                NextFeedbackSLAMissed =
                                    f.NextFeedbackSLAMissed
                            })
                        .ToListAsync();


            // =================================================
            // 5. Generate Date Collection
            // =================================================

            var dates =
                Enumerable
                    .Range(
                        0,
                        Math.Max(
                            0,
                            (toDateExclusive.Date -
                             fromDate.Date).Days))
                    .Select(offset =>
                        fromDate.Date.AddDays(offset))
                    .ToList();


            // =================================================
            // 6. Current Time
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // 7. Build Trend
            // =================================================

            var trend =
                new List<PerformanceTrendViewModel>();


            foreach (var date in dates)
            {
                var nextDate =
                    date.AddDays(1);


                // =================================================
                // Assigned Leads
                // =================================================

                var dailyAssignments =
                    assignments
                        .Where(a =>
                            a.AssignedAt >= date &&
                            a.AssignedAt < nextDate)
                        .ToList();


                var assignedLeads =
                    dailyAssignments.Count;


                // =================================================
                // Accepted Leads
                // =================================================

                var acceptedLeads =
                    dailyAssignments.Count(a =>
                        a.AcceptedAt.HasValue &&

                        a.AcceptedAt.Value >= date &&

                        a.AcceptedAt.Value < nextDate);


                // =================================================
                // Completed Leads
                // =================================================

                var completedLeads =
                    dailyAssignments.Count(a =>
                        a.LeadStatus ==
                        LeadStatus.Completed);


                // =================================================
                // Feedbacks
                // =================================================

                var dailyFeedbacks =
                    feedbacks
                        .Where(f =>
                            f.SubmittedAt >= date &&
                            f.SubmittedAt < nextDate)
                        .ToList();


                var totalFeedbacks =
                    dailyFeedbacks.Count;


                // =================================================
                // Overdue Follow-ups
                // =================================================

                var overdueFollowUps =
                    feedbacks.Count(f =>
                        f.NextFollowUpDate.HasValue &&

                        f.NextFollowUpDate.Value >= date &&

                        f.NextFollowUpDate.Value < nextDate &&

                        f.NextFollowUpDate.Value < now &&

                        !feedbacks.Any(next =>
                            next.AssignmentId ==
                                f.AssignmentId &&

                            next.SubmittedAt >
                                f.SubmittedAt));


                // =================================================
                // Add Daily Trend
                // =================================================

                trend.Add(
                    new PerformanceTrendViewModel
                    {
                        Date =
                            date,

                        AssignedLeads =
                            assignedLeads,

                        AcceptedLeads =
                            acceptedLeads,

                        CompletedLeads =
                            completedLeads,

                        Feedbacks =
                            totalFeedbacks,

                        OverdueFollowUps =
                            overdueFollowUps
                    });
            }


            return trend;
        }


        // =====================================================
        // Internal Feedback Projection Class
        // =====================================================

        private class FeedbackPerformanceData
        {
            public long FeedbackId { get; set; }

            public long AssignmentId { get; set; }

            public FeedbackStatus Status { get; set; }

            public DateTime SubmittedAt { get; set; }

            public DateTime? NextFollowUpDate { get; set; }

            public bool NextFeedbackSLAMissed { get; set; }
        }


        // =====================================================
        // Get Top Performer
        // =====================================================

        public async Task<PerformanceHighlightViewModel?>
            GetTopPerformerAsync(
                PerformanceFilterViewModel? filter = null)
        {
            var performance =
                await GetPerformanceAsync(filter);


            if (performance.Count == 0)
            {
                return null;
            }


            // =================================================
            // Calculate Weighted Performance Score
            //
            // Completion Rate          = 25%
            // First Feedback SLA       = 20%
            // Follow-up Timeliness     = 20%
            // Acceptance SLA           = 15%
            // Next Feedback SLA        = 10%
            // Acceptance Rate          = 10%
            //
            // Total                    = 100%
            // =================================================

            var scoredPerformance =
                performance
                    .Select(officer => new
                    {
                        Officer = officer,

                        Score =
                            (
                                officer.CompletionRate
                                * 0.25
                            )
                            +
                            (
                                officer.FirstFeedbackSLAComplianceRate
                                * 0.20
                            )
                            +
                            (
                                officer.FollowUpTimelinessRate
                                * 0.20
                            )
                            +
                            (
                                officer.AcceptanceSLAComplianceRate
                                * 0.15
                            )
                            +
                            (
                                officer.NextFeedbackSLAComplianceRate
                                * 0.10
                            )
                            +
                            (
                                officer.AcceptanceRate
                                * 0.10
                            )
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(
                        x => x.Officer.CompletedLeads)
                    .ThenByDescending(
                        x => x.Officer.TotalFeedbacks)
                    .FirstOrDefault();


            if (scoredPerformance == null)
            {
                return null;
            }


            var officerData =
                scoredPerformance.Officer;


            return new PerformanceHighlightViewModel
            {
                SalesOfficerId =
                    officerData.SalesOfficerId,

                SalesOfficerName =
                    officerData.SalesOfficerName,

                PerformanceScore =
                    Math.Round(
                        scoredPerformance.Score,
                        2),

                AcceptanceRate =
                    officerData.AcceptanceRate,

                AcceptanceSLAComplianceRate =
                    officerData.AcceptanceSLAComplianceRate,

                FirstFeedbackSLAComplianceRate =
                    officerData.FirstFeedbackSLAComplianceRate,

                NextFeedbackSLAComplianceRate =
                    officerData.NextFeedbackSLAComplianceRate,

                FollowUpTimelinessRate =
                    officerData.FollowUpTimelinessRate,

                CompletionRate =
                    officerData.CompletionRate,

                TotalAssignedLeads =
                    officerData.TotalAssignedLeads,

                AcceptedLeads =
                    officerData.AcceptedLeads,

                CompletedLeads =
                    officerData.CompletedLeads,

                TotalFeedbacks =
                    officerData.TotalFeedbacks,

                AcceptanceSLAMissed =
                    officerData.AcceptanceSLAMissed,

                FirstFeedbackSLAMissed =
                    officerData.FirstFeedbackSLAMissed,

                NextFeedbackSLAMissed =
                    officerData.NextFeedbackSLAMissed,

                OverdueFollowUps =
                    officerData.OverdueFollowUps
            };
        }


        // =====================================================
        // Get Needs Attention Officer
        // =====================================================

        public async Task<PerformanceHighlightViewModel?>
            GetNeedsAttentionAsync(
                PerformanceFilterViewModel? filter = null)
        {
            var performance =
                await GetPerformanceAsync(filter);


            if (performance.Count == 0)
            {
                return null;
            }


            // =================================================
            // Calculate Attention Score
            //
            // Higher score = More problems
            //
            // Acceptance SLA Missed     = 3 points
            // First Feedback Missed     = 3 points
            // Next Feedback Missed      = 3 points
            // Overdue Follow-ups        = 4 points
            // Low Acceptance Rate       = up to 10 points
            // Low Follow-up Timeliness  = up to 10 points
            // Low Completion Rate       = up to 10 points
            // =================================================
            //Performance Metric        Weight
            //Completion Rate           25 %
            //First Feedback SLA        20 %
            //Follow - up Timeliness    20 %
            //Acceptance SLA            15 %
            //Next Feedback SLA         10 %
            //Acceptance Rate           10 %
            //Total                     100 %

            var scoredPerformance =
                performance
                    .Select(officer =>
                    {
                        var attentionScore =
                            0.0;


                        // -------------------------------------
                        // Acceptance SLA
                        // -------------------------------------

                        attentionScore +=
                            officer.AcceptanceSLAMissed
                            * 3;


                        // -------------------------------------
                        // First Feedback SLA
                        // -------------------------------------

                        attentionScore +=
                            officer.FirstFeedbackSLAMissed
                            * 3;


                        // -------------------------------------
                        // Next Feedback SLA
                        // -------------------------------------

                        attentionScore +=
                            officer.NextFeedbackSLAMissed
                            * 3;


                        // -------------------------------------
                        // Overdue Follow-ups
                        // -------------------------------------

                        attentionScore +=
                            officer.OverdueFollowUps
                            * 4;


                        // -------------------------------------
                        // Low Acceptance Rate
                        // -------------------------------------

                        if (
                            officer.TotalAssignedLeads > 0 &&
                            officer.AcceptanceRate < 50)
                        {
                            attentionScore +=
                                (50 -
                                 officer.AcceptanceRate)
                                / 5;
                        }


                        // -------------------------------------
                        // Low Follow-up Timeliness
                        // -------------------------------------

                        if (
                            officer.FollowUpTimelinessRate < 70)
                        {
                            attentionScore +=
                                (70 -
                                 officer.FollowUpTimelinessRate)
                                / 7;
                        }


                        // -------------------------------------
                        // Low Completion Rate
                        // -------------------------------------

                        if (
                            officer.AcceptedLeads > 0 &&
                            officer.CompletionRate < 50)
                        {
                            attentionScore +=
                                (50 -
                                 officer.CompletionRate)
                                / 5;
                        }


                        return new
                        {
                            Officer = officer,

                            Score =
                                attentionScore
                        };
                    })
                    .OrderByDescending(
                        x => x.Score)
                    .ThenByDescending(
                        x => x.Officer.OverdueFollowUps)
                    .ThenByDescending(
                        x => x.Officer.NextFeedbackSLAMissed)
                    .ThenByDescending(
                        x => x.Officer.CompletedLeads)
                    .FirstOrDefault();


            if (scoredPerformance == null)
            {
                return null;
            }


            var officerData =
                scoredPerformance.Officer;


            return new PerformanceHighlightViewModel
            {
                SalesOfficerId =
                    officerData.SalesOfficerId,

                SalesOfficerName =
                    officerData.SalesOfficerName,

                PerformanceScore =
                    Math.Round(
                        scoredPerformance.Score,
                        2),

                AcceptanceRate =
                    officerData.AcceptanceRate,

                AcceptanceSLAComplianceRate =
                    officerData.AcceptanceSLAComplianceRate,

                FirstFeedbackSLAComplianceRate =
                    officerData.FirstFeedbackSLAComplianceRate,

                NextFeedbackSLAComplianceRate =
                    officerData.NextFeedbackSLAComplianceRate,

                FollowUpTimelinessRate =
                    officerData.FollowUpTimelinessRate,

                CompletionRate =
                    officerData.CompletionRate,

                TotalAssignedLeads =
                    officerData.TotalAssignedLeads,

                AcceptedLeads =
                    officerData.AcceptedLeads,

                CompletedLeads =
                    officerData.CompletedLeads,

                TotalFeedbacks =
                    officerData.TotalFeedbacks,

                AcceptanceSLAMissed =
                    officerData.AcceptanceSLAMissed,

                FirstFeedbackSLAMissed =
                    officerData.FirstFeedbackSLAMissed,

                NextFeedbackSLAMissed =
                    officerData.NextFeedbackSLAMissed,

                OverdueFollowUps =
                    officerData.OverdueFollowUps
            };
        }

        // =====================================================
        // Get SLA Trend
        // =====================================================

        public async Task<List<SLATrendViewModel>>
            GetSLATrendAsync(
                PerformanceFilterViewModel? filter = null)
        {
            // =================================================
            // 1. Resolve Date Range
            // =================================================

            var dateRange =
                ResolveDateRange(filter);


            var fromDate =
                dateRange.FromDate;


            var toDateExclusive =
                dateRange.ToDateExclusive;


            // =================================================
            // 2. Load Active Sales Officers
            // =================================================

            var salesOfficerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleKey == "SALES_OFFICER" &&
                        u.IsActive &&
                        !u.IsDeleted)
                    .Select(u => u.UserId)
                    .ToListAsync();


            if (salesOfficerIds.Count == 0)
            {
                return new List<SLATrendViewModel>();
            }


            // =================================================
            // 3. Load Assignments
            // =================================================

            var assignments =
                await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        salesOfficerIds.Contains(
                            a.SalesOfficerId) &&

                        a.AssignedAt >= fromDate &&

                        a.AssignedAt < toDateExclusive)
                    .Select(a => new
                    {
                        a.AssignmentId,

                        a.SalesOfficerId,

                        a.AssignedAt,

                        a.AcceptedAt
                    })
                    .ToListAsync();


            // =================================================
            // 4. Load Feedbacks
            // =================================================

            var assignmentIds =
                assignments
                    .Select(a => a.AssignmentId)
                    .ToList();


            var feedbacks =
                assignmentIds.Count == 0
                    ? new List<FeedbackPerformanceData>()
                    : await _context.Feedbacks
                        .AsNoTracking()
                        .Where(f =>
                            assignmentIds.Contains(
                                f.AssignmentId))
                        .Select(f =>
                            new FeedbackPerformanceData
                            {
                                FeedbackId =
                                    f.FeedbackId,

                                AssignmentId =
                                    f.AssignmentId,

                                Status =
                                    f.Status,

                                SubmittedAt =
                                    f.SubmittedAt,

                                NextFollowUpDate =
                                    f.NextFollowUpDate,

                                NextFeedbackSLAMissed =
                                    f.NextFeedbackSLAMissed
                            })
                        .ToListAsync();


            // =================================================
            // 5. Current Time
            // =================================================

            var now =
                DateTime.UtcNow;


            // =================================================
            // 6. Generate Dates
            // =================================================

            var dates =
                Enumerable
                    .Range(
                        0,
                        Math.Max(
                            0,
                            (
                                toDateExclusive.Date -
                                fromDate.Date
                            ).Days))
                    .Select(offset =>
                        fromDate.Date.AddDays(offset))
                    .ToList();


            // =================================================
            // 7. Final Trend Collection
            // =================================================

            var trend =
                new List<SLATrendViewModel>();


            // =================================================
            // 8. Calculate SLA For Each Date
            // =================================================

            foreach (var date in dates)
            {
                var nextDate =
                    date.AddDays(1);


                // =================================================
                // Daily Assignments
                // =================================================

                var dailyAssignments =
                    assignments
                        .Where(a =>
                            a.AssignedAt >= date &&
                            a.AssignedAt < nextDate)
                        .ToList();


                // =================================================
                // Acceptance SLA
                // =================================================

                var acceptanceSLAMet = 0;

                var acceptanceSLAMissed = 0;


                foreach (var assignment in dailyAssignments)
                {
                    if (assignment.AcceptedAt.HasValue)
                    {
                        var deadline =
                            assignment.AssignedAt
                                .AddHours(1);


                        if (assignment.AcceptedAt.Value
                            <= deadline)
                        {
                            acceptanceSLAMet++;
                        }
                        else
                        {
                            acceptanceSLAMissed++;
                        }

                        continue;
                    }


                    if (
                        assignment.AssignedAt
                            .AddHours(1)
                        < now)
                    {
                        acceptanceSLAMissed++;
                    }
                }


                var acceptanceApplicable =
                    acceptanceSLAMet +
                    acceptanceSLAMissed;


                var acceptanceRate =
                    acceptanceApplicable > 0
                        ? (double)acceptanceSLAMet /
                          acceptanceApplicable * 100
                        : 0;


                // =================================================
                // Daily Feedbacks
                // =================================================

                var dailyFeedbacks =
                    feedbacks
                        .Where(f =>
                            f.SubmittedAt >= date &&
                            f.SubmittedAt < nextDate)
                        .ToList();


                // =================================================
                // First Feedback SLA
                // =================================================

                var firstFeedbackSLAMet = 0;

                var firstFeedbackSLAMissed = 0;


                foreach (var assignment in dailyAssignments)
                {
                    var firstFeedback =
                        feedbacks
                            .Where(f =>
                                f.AssignmentId ==
                                    assignment.AssignmentId)
                            .OrderBy(f =>
                                f.SubmittedAt)
                            .FirstOrDefault();


                    if (firstFeedback != null)
                    {
                        var deadline =
                            assignment.AssignedAt
                                .AddHours(3);


                        if (firstFeedback.SubmittedAt
                            <= deadline)
                        {
                            firstFeedbackSLAMet++;
                        }
                        else
                        {
                            firstFeedbackSLAMissed++;
                        }

                        continue;
                    }


                    if (
                        assignment.AssignedAt
                            .AddHours(3)
                        < now)
                    {
                        firstFeedbackSLAMissed++;
                    }
                }


                var firstFeedbackApplicable =
                    firstFeedbackSLAMet +
                    firstFeedbackSLAMissed;


                var firstFeedbackRate =
                    firstFeedbackApplicable > 0
                        ? (double)firstFeedbackSLAMet /
                          firstFeedbackApplicable * 100
                        : 0;


                // =================================================
                // Next Feedback SLA
                // =================================================

                var nextFeedbackSLAMet = 0;

                var nextFeedbackSLAMissed = 0;


                foreach (var feedback in dailyFeedbacks)
                {
                    if (!feedback.NextFollowUpDate.HasValue)
                    {
                        continue;
                    }


                    var expectedDate =
                        feedback.NextFollowUpDate.Value;


                    var nextFeedback =
                        feedbacks
                            .Where(next =>
                                next.AssignmentId ==
                                    feedback.AssignmentId &&

                                next.SubmittedAt >
                                    feedback.SubmittedAt)
                            .OrderBy(next =>
                                next.SubmittedAt)
                            .FirstOrDefault();


                    if (nextFeedback != null)
                    {
                        if (
                            nextFeedback.SubmittedAt
                            <= expectedDate)
                        {
                            nextFeedbackSLAMet++;
                        }
                        else
                        {
                            nextFeedbackSLAMissed++;
                        }

                        continue;
                    }


                    if (feedback.NextFeedbackSLAMissed)
                    {
                        nextFeedbackSLAMissed++;

                        continue;
                    }


                    if (expectedDate < now)
                    {
                        nextFeedbackSLAMissed++;
                    }
                }


                var nextFeedbackApplicable =
                    nextFeedbackSLAMet +
                    nextFeedbackSLAMissed;


                var nextFeedbackRate =
                    nextFeedbackApplicable > 0
                        ? (double)nextFeedbackSLAMet /
                          nextFeedbackApplicable * 100
                        : 0;


                // =================================================
                // Add Daily SLA Trend
                // =================================================

                trend.Add(
                    new SLATrendViewModel
                    {
                        Date =
                            date,

                        AcceptanceSLAComplianceRate =
                            Math.Round(
                                acceptanceRate,
                                2),

                        FirstFeedbackSLAComplianceRate =
                            Math.Round(
                                firstFeedbackRate,
                                2),

                        NextFeedbackSLAComplianceRate =
                            Math.Round(
                                nextFeedbackRate,
                                2)
                    });
            }


            return trend;
        }

        // =====================================================
        // Get Completion Trend
        // =====================================================

        public async Task<List<CompletionTrendViewModel>>
            GetCompletionTrendAsync(
                PerformanceFilterViewModel? filter = null)
        {
            // =================================================
            // 1. Resolve Selected Date Range
            // =================================================

            var dateRange =
                ResolveDateRange(filter);

            var fromDate =
                dateRange.FromDate;

            var toDateExclusive =
                dateRange.ToDateExclusive;


            // =================================================
            // 2. Get Active Sales Officer IDs
            // =================================================

            var salesOfficerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleKey == "SALES_OFFICER" &&
                        u.IsActive &&
                        !u.IsDeleted)
                    .Select(u => u.UserId)
                    .ToListAsync();


            // =================================================
            // 3. No Active Sales Officers
            // =================================================

            if (salesOfficerIds.Count == 0)
            {
                return new List<CompletionTrendViewModel>();
            }


            // =================================================
            // 4. Get Lead Assignments Within Selected Range
            // =================================================

            var assignments =
                await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        salesOfficerIds.Contains(a.SalesOfficerId) &&
                        a.AssignedAt >= fromDate &&
                        a.AssignedAt < toDateExclusive)
                    .Select(a => new
                    {
                        a.LeadId,
                        a.AssignedAt,
                        a.AcceptedAt
                    })
                    .ToListAsync();


            // =================================================
            // 5. Get Completed Lead IDs
            // =================================================

            var leadIds =
                assignments
                    .Select(a => a.LeadId)
                    .Distinct()
                    .ToList();


            var completedLeadIds =
                leadIds.Count == 0
                    ? new List<long>()
                    : await _context.Leads
                        .AsNoTracking()
                        .Where(l =>
                            leadIds.Contains(l.LeadId) &&
                            l.Status == LeadStatus.Completed)
                        .Select(l => l.LeadId)
                        .ToListAsync();


            // =================================================
            // 6. Generate Every Date In Selected Range
            // =================================================

            var totalDays =
                Math.Max(
                    0,
                    (toDateExclusive.Date - fromDate.Date).Days);


            var dates =
                Enumerable
                    .Range(0, totalDays)
                    .Select(offset =>
                        fromDate.Date.AddDays(offset))
                    .ToList();


            // =================================================
            // 7. Prepare Trend Result
            // =================================================

            var trend =
                new List<CompletionTrendViewModel>();


            // =================================================
            // 8. Calculate Daily Completion Performance
            // =================================================

            foreach (var date in dates)
            {
                var nextDate =
                    date.AddDays(1);


                // =============================================
                // Daily Assignments
                // =============================================

                var dailyAssignments =
                    assignments
                        .Where(a =>
                            a.AssignedAt >= date &&
                            a.AssignedAt < nextDate)
                        .ToList();


                // =============================================
                // Assigned Leads
                // =============================================

                var assignedCount =
                    dailyAssignments.Count;


                // =============================================
                // Accepted Leads
                // =============================================

                var acceptedCount =
                    dailyAssignments.Count(a =>
                        a.AcceptedAt.HasValue &&
                        a.AcceptedAt.Value >= date &&
                        a.AcceptedAt.Value < nextDate);


                // =============================================
                // Completed Leads
                // =============================================

                var completedCount =
                    dailyAssignments
                        .Where(a =>
                            completedLeadIds.Contains(a.LeadId))
                        .Select(a => a.LeadId)
                        .Distinct()
                        .Count();


                // =============================================
                // Completion Rate
                // =============================================

                var completionRate =
                    assignedCount > 0
                        ? (double)completedCount /
                          assignedCount *
                          100
                        : 0;


                // =============================================
                // Add Daily Trend Record
                // =============================================

                trend.Add(
                    new CompletionTrendViewModel
                    {
                        Date =
                            date,

                        AssignedLeads =
                            assignedCount,

                        AcceptedLeads =
                            acceptedCount,

                        CompletedLeads =
                            completedCount,

                        CompletionRate =
                            Math.Round(
                                completionRate,
                                2)
                    });
            }


            // =================================================
            // 9. Return Completion Trend
            // =================================================

            return trend;
        }

        // =====================================================
        // Calculate Weighted Performance Score
        // =====================================================

        private static double CalculatePerformanceScore(
            SalesOfficerPerformanceViewModel officer)
        {
            return
                (
                    officer.CompletionRate * 0.25
                )
                +
                (
                    officer.FirstFeedbackSLAComplianceRate * 0.20
                )
                +
                (
                    officer.FollowUpTimelinessRate * 0.20
                )
                +
                (
                    officer.AcceptanceSLAComplianceRate * 0.15
                )
                +
                (
                    officer.NextFeedbackSLAComplianceRate * 0.10
                )
                +
                (
                    officer.AcceptanceRate * 0.10
                );
        }
    }
}