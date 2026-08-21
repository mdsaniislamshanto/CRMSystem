using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SLAService : ISLAService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public SLAService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }


        // =====================================================
        // Check Acceptance SLA
        // =====================================================

        public async Task CheckAcceptanceSLAsAsync()
        {
            var now = DateTime.UtcNow;

            var assignments = await _context.LeadAssignments
                .Include(a => a.Lead)
                .Where(a =>
                    a.AssignmentStatus == AssignmentStatus.Pending &&
                    !a.AcceptanceSLAMissed &&
                    a.AssignedAt.AddHours(1) < now)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.AcceptanceSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.AcceptanceSLAMissed,
                    "Lead Acceptance SLA Missed",
                    $"The 1-hour acceptance SLA for lead {leadName} has been missed.",
                    assignment.LeadId,
                    assignment.AssignmentId);

                if (assignment.AssignedBy > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.AcceptanceSLAMissed,
                        "Sales Officer Missed Acceptance SLA",
                        $"The Sales Officer assigned to lead {leadName} did not accept the lead within 1 hour.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }
            }

            await _context.SaveChangesAsync();
        }


        // =====================================================
        // Check First Feedback SLA
        // =====================================================

        public async Task CheckFirstFeedbackSLAsAsync()
        {
            var now = DateTime.UtcNow;

            var assignments = await _context.LeadAssignments
                .Include(a => a.Lead)
                .Include(a => a.Feedbacks)
                .Where(a =>
                    a.AssignmentStatus == AssignmentStatus.Accepted &&
                    a.AcceptedAt.HasValue &&
                    !a.FirstFeedbackSLAMissed &&
                    a.AcceptedAt.Value.AddHours(3) < now &&
                    !a.Feedbacks.Any())
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                assignment.FirstFeedbackSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.FirstFeedbackSLAMissed,
                    "First Feedback SLA Missed",
                    $"The 3-hour first feedback SLA for lead {leadName} has been missed.",
                    assignment.LeadId,
                    assignment.AssignmentId);

                if (assignment.AssignedBy > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.FirstFeedbackSLAMissed,
                        "First Feedback SLA Missed",
                        $"The Sales Officer assigned to lead {leadName} did not submit the first feedback within 3 hours of acceptance.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }
            }

            await _context.SaveChangesAsync();
        }


        // =====================================================
        // Check Next Feedback SLA
        // =====================================================

        public async Task CheckNextFeedbackSLAsAsync()
        {
            var now = DateTime.UtcNow;

            var feedbacks = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)
                .Where(f =>
                    f.NextFollowUpDate.HasValue &&
                    f.NextFollowUpDate.Value < now &&
                    !f.NextFeedbackSLAMissed &&
                    f.LeadAssignment != null)
                .ToListAsync();

            foreach (var feedback in feedbacks)
            {
                var assignment = feedback.LeadAssignment!;

                var hasNewerFeedback = await _context.Feedbacks
                    .AnyAsync(f =>
                        f.AssignmentId == feedback.AssignmentId &&
                        f.SubmittedAt > feedback.SubmittedAt);

                if (hasNewerFeedback)
                {
                    continue;
                }

                feedback.NextFeedbackSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.NextFeedbackOverdue,
                    "Next Feedback Overdue",
                    $"The next feedback for lead {leadName} is overdue.",
                    assignment.LeadId,
                    assignment.AssignmentId);

                if (assignment.AssignedBy > 0)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.NextFeedbackOverdue,
                        "Next Feedback Overdue",
                        $"The next feedback for lead {leadName} is overdue.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}