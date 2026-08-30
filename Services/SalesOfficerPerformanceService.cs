using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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
        // Get Sales Officer Performance
        // =====================================================

        public async Task<SalesOfficerPerformanceViewModel?>
            GetPerformanceAsync(long salesOfficerId)
        {
            // =====================================================
            // Get Sales Officer
            // =====================================================

            var salesOfficer = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == salesOfficerId &&
                    u.IsActive);

            if (salesOfficer == null)
            {
                return null;
            }


            // =====================================================
            // Get Assignments
            // =====================================================

            var assignments = _context.LeadAssignments
                .Where(a =>
                    a.SalesOfficerId == salesOfficerId);


            // =====================================================
            // Get Feedbacks
            // =====================================================

            var feedbacks = _context.Feedbacks
                .Where(f =>
                    f.LeadAssignment != null &&
                    f.LeadAssignment.SalesOfficerId ==
                    salesOfficerId);


            // =====================================================
            // Total Assigned Leads
            // =====================================================

            var totalAssignedLeads =
                await assignments.CountAsync();


            // =====================================================
            // Accepted Leads
            // =====================================================

            var acceptedLeads =
                await assignments.CountAsync(a =>
                    a.AcceptedAt != null);


            // =====================================================
            // Pending Acceptance
            // =====================================================

            var pendingAcceptance =
                await assignments.CountAsync(a =>
                    a.AcceptedAt == null);


            // =====================================================
            // Acceptance Rate
            // =====================================================

            double acceptanceRate = 0;

            if (totalAssignedLeads > 0)
            {
                acceptanceRate =
                    (double)acceptedLeads /
                    totalAssignedLeads *
                    100;
            }


            // =====================================================
            // Acceptance SLA
            // =====================================================

            var acceptanceSLAMissed =
                await assignments.CountAsync(a =>
                    a.AcceptanceSLAMissed);


            var acceptanceSLAMet =
                await assignments.CountAsync(a =>
                    a.AcceptedAt != null &&
                    !a.AcceptanceSLAMissed);


            double acceptanceSLAComplianceRate = 0;

            var acceptanceSLATotal =
                acceptanceSLAMet +
                acceptanceSLAMissed;

            if (acceptanceSLATotal > 0)
            {
                acceptanceSLAComplianceRate =
                    (double)acceptanceSLAMet /
                    acceptanceSLATotal *
                    100;
            }


            // =====================================================
            // First Feedback SLA
            // =====================================================

            var firstFeedbackSLAMissed =
                await assignments.CountAsync(a =>
                    a.FirstFeedbackSLAMissed);


            // =====================================================
            // Get First Feedback for Each Assignment
            // =====================================================

            var firstFeedbacks = await feedbacks
                .GroupBy(f => f.AssignmentId)
                .Select(g => g
                    .OrderBy(f => f.SubmittedAt)
                    .Select(f => new
                    {
                        f.AssignmentId,
                        f.SubmittedAt
                    })
                    .First())
                .ToListAsync();


            // =====================================================
            // Calculate First Feedback SLA Met
            // =====================================================

            var assignmentTimes = await assignments
                .Select(a => new
                {
                    a.AssignmentId,
                    a.AssignedAt
                })
                .ToListAsync();


            var firstFeedbackSLAMet = 0;

            foreach (var assignment in assignmentTimes)
            {
                var firstFeedback =
                    firstFeedbacks.FirstOrDefault(f =>
                        f.AssignmentId ==
                        assignment.AssignmentId);

                if (firstFeedback == null)
                {
                    continue;
                }

                var deadline =
                    assignment.AssignedAt.AddHours(3);

                if (firstFeedback.SubmittedAt <= deadline)
                {
                    firstFeedbackSLAMet++;
                }
            }


            // =====================================================
            // First Feedback SLA Compliance Rate
            // =====================================================

            double firstFeedbackSLAComplianceRate = 0;

            var firstFeedbackSLATotal =
                firstFeedbackSLAMet +
                firstFeedbackSLAMissed;

            if (firstFeedbackSLATotal > 0)
            {
                firstFeedbackSLAComplianceRate =
                    (double)firstFeedbackSLAMet /
                    firstFeedbackSLATotal *
                    100;
            }


            // =====================================================
            // Latest Feedback Per Assignment
            // =====================================================

            var latestFeedbacks = await feedbacks
                .GroupBy(f => f.AssignmentId)
                .Select(g => g
                    .OrderByDescending(f => f.SubmittedAt)
                    .Select(f => new
                    {
                        f.AssignmentId,
                        f.FeedbackId,
                        f.Status,
                        f.SubmittedAt,
                        f.NextFollowUpDate
                    })
                    .First())
                .ToListAsync();


            // =====================================================
            // Completed Leads
            // =====================================================

            var completedLeads =
                latestFeedbacks.Count(f =>
                    f.Status ==
                    FeedbackStatus.Completed);


            // =====================================================
            // Total Feedbacks
            // =====================================================

            var totalFeedbacks =
                await feedbacks.CountAsync();


            // =====================================================
            // Feedback Status Counts
            // =====================================================

            var interestedCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.Interested);


            var followUpRequiredCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.FollowUpRequired);


            var meetingScheduledCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.MeetingScheduled);


            var visitedCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.Visited);


            var quotationSentCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.QuotationSent);


            var negotiationCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.Negotiation);


            var completedCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.Completed);


            var closedCount =
                await feedbacks.CountAsync(f =>
                    f.Status ==
                    FeedbackStatus.Closed);


            // =====================================================
            // Follow-up Performance
            // =====================================================

            var allFeedbacks = await feedbacks
                .Select(f => new
                {
                    f.FeedbackId,
                    f.AssignmentId,
                    f.SubmittedAt,
                    f.NextFollowUpDate
                })
                .OrderBy(f => f.AssignmentId)
                .ThenBy(f => f.SubmittedAt)
                .ToListAsync();


            var followUpsCompletedOnTime = 0;

            var followUpsCompletedLate = 0;

            var overdueFollowUps = 0;


            // =====================================================
            // Group Feedbacks By Assignment
            // =====================================================

            var feedbackGroups =
                allFeedbacks.GroupBy(f =>
                    f.AssignmentId);


            foreach (var group in feedbackGroups)
            {
                var feedbackList =
                    group
                        .OrderBy(f => f.SubmittedAt)
                        .ToList();


                for (int i = 0;
                     i < feedbackList.Count;
                     i++)
                {
                    var currentFeedback =
                        feedbackList[i];


                    // =============================================
                    // No next follow-up requested
                    // =============================================

                    if (!currentFeedback.NextFollowUpDate.HasValue)
                    {
                        continue;
                    }


                    var followUpDeadline =
                        currentFeedback.NextFollowUpDate.Value;


                    // =============================================
                    // Find next feedback
                    // =============================================

                    var nextFeedback =
                        feedbackList
                            .Skip(i + 1)
                            .FirstOrDefault();


                    // =============================================
                    // No next feedback
                    // =============================================

                    if (nextFeedback == null)
                    {
                        if (DateTime.UtcNow >
                            followUpDeadline)
                        {
                            overdueFollowUps++;
                        }

                        continue;
                    }


                    // =============================================
                    // Follow-up completed on time
                    // =============================================

                    if (nextFeedback.SubmittedAt <=
                        followUpDeadline)
                    {
                        followUpsCompletedOnTime++;
                    }
                    else
                    {
                        followUpsCompletedLate++;
                    }
                }
            }


            // =====================================================
            // Follow-up Timeliness Rate
            // =====================================================

            double followUpTimelinessRate = 0;

            var completedFollowUps =
                followUpsCompletedOnTime +
                followUpsCompletedLate;

            if (completedFollowUps > 0)
            {
                followUpTimelinessRate =
                    (double)followUpsCompletedOnTime /
                    completedFollowUps *
                    100;
            }


            // =====================================================
            // Return ViewModel
            // =====================================================

            return new SalesOfficerPerformanceViewModel
            {
                SalesOfficerId =
                    salesOfficer.UserId,

                SalesOfficerName =
                    $"{salesOfficer.FirstName} " +
                    $"{salesOfficer.LastName}".Trim(),


                // Lead Performance

                TotalAssignedLeads =
                    totalAssignedLeads,

                AcceptedLeads =
                    acceptedLeads,

                PendingAcceptance =
                    pendingAcceptance,

                AcceptanceRate =
                    Math.Round(
                        acceptanceRate,
                        2),


                // Acceptance SLA

                AcceptanceSLAMet =
                    acceptanceSLAMet,

                AcceptanceSLAMissed =
                    acceptanceSLAMissed,

                AcceptanceSLAComplianceRate =
                    Math.Round(
                        acceptanceSLAComplianceRate,
                        2),


                // First Feedback SLA

                FirstFeedbackSLAMet =
                    firstFeedbackSLAMet,

                FirstFeedbackSLAMissed =
                    firstFeedbackSLAMissed,

                FirstFeedbackSLAComplianceRate =
                    Math.Round(
                        firstFeedbackSLAComplianceRate,
                        2),


                // Lead Completion

                CompletedLeads =
                    completedLeads,


                // Feedback

                TotalFeedbacks =
                    totalFeedbacks,


                // Follow-up

                FollowUpsCompletedOnTime =
                    followUpsCompletedOnTime,

                FollowUpsCompletedLate =
                    followUpsCompletedLate,

                OverdueFollowUps =
                    overdueFollowUps,

                FollowUpTimelinessRate =
                    Math.Round(
                        followUpTimelinessRate,
                        2),


                // Feedback Status

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
        }
    }
}