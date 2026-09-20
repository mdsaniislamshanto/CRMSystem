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

            if (!assignments.Any())
            {
                return;
            }


            // -------------------------------------------------
            // Load Sales Officers and their Team Leads
            // -------------------------------------------------

            var salesOfficerIds =
                assignments
                    .Select(a => a.SalesOfficerId)
                    .Distinct()
                    .ToList();

            var salesOfficerTeamLeads =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        salesOfficerIds.Contains(u.UserId) &&
                        u.IsActive)
                    .Select(u => new
                    {
                        SalesOfficerId = u.UserId,
                        TeamLeadId = u.TeamLeadId
                    })
                    .ToDictionaryAsync(
                        x => x.SalesOfficerId,
                        x => x.TeamLeadId);


            foreach (var assignment in assignments)
            {
                assignment.AcceptanceSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";


                // =================================================
                // 1. Notify Sales Officer
                // =================================================

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.AcceptanceSLAMissed,
                    "Lead Acceptance SLA Missed",
                    $"The 1-hour acceptance SLA for lead {leadName} has been missed.",
                    assignment.LeadId,
                    assignment.AssignmentId);


                // =================================================
                // 2. Notify Assigned By
                // Existing functionality preserved
                // =================================================

                if (assignment.AssignedBy > 0 &&
                    assignment.AssignedBy != assignment.SalesOfficerId)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.AcceptanceSLAMissed,
                        "Sales Officer Missed Acceptance SLA",
                        $"The Sales Officer assigned to lead {leadName} did not accept the lead within 1 hour.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }


                // =================================================
                // 3. Notify Team Lead
                // New functionality
                // =================================================

                if (salesOfficerTeamLeads.TryGetValue(
                        assignment.SalesOfficerId,
                        out var teamLeadId) &&
                    teamLeadId.HasValue &&
                    teamLeadId.Value > 0)
                {
                    var teamLeadUserId =
                        teamLeadId.Value;

                    // Prevent duplicate recipient
                    if (teamLeadUserId != assignment.SalesOfficerId &&
                        teamLeadUserId != assignment.AssignedBy)
                    {
                        await _notificationService.CreateNotificationAsync(
                            teamLeadUserId,
                            NotificationType.AcceptanceSLAMissed,
                            "Sales Officer Missed Acceptance SLA",
                            $"The Sales Officer assigned to lead {leadName} did not accept the lead within 1 hour.",
                            assignment.LeadId,
                            assignment.AssignmentId);
                    }
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

                    // -------------------------------------------------
                    // First Feedback SLA:
                    // 3 hours from Lead Assignment
                    // -------------------------------------------------
                    a.AssignedAt.AddHours(3) < now &&

                    !a.Feedbacks.Any())
                .ToListAsync();

            if (!assignments.Any())
            {
                return;
            }


            // -------------------------------------------------
            // Load Sales Officers and their Team Leads
            // -------------------------------------------------

            var salesOfficerIds =
                assignments
                    .Select(a => a.SalesOfficerId)
                    .Distinct()
                    .ToList();

            var salesOfficerTeamLeads =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        salesOfficerIds.Contains(u.UserId) &&
                        u.IsActive)
                    .Select(u => new
                    {
                        SalesOfficerId = u.UserId,
                        TeamLeadId = u.TeamLeadId
                    })
                    .ToDictionaryAsync(
                        x => x.SalesOfficerId,
                        x => x.TeamLeadId);


            foreach (var assignment in assignments)
            {
                assignment.FirstFeedbackSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";


                // =================================================
                // 1. Notify Sales Officer
                // =================================================

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.FirstFeedbackSLAMissed,
                    "First Feedback SLA Missed",
                    $"The 3-hour first feedback SLA for lead {leadName} has been missed.",
                    assignment.LeadId,
                    assignment.AssignmentId);


                // =================================================
                // 2. Notify Assigned By
                // Existing functionality preserved
                // =================================================

                if (assignment.AssignedBy > 0 &&
                    assignment.AssignedBy != assignment.SalesOfficerId)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.FirstFeedbackSLAMissed,
                        "First Feedback SLA Missed",
                        $"The Sales Officer assigned to lead {leadName} did not submit the first feedback within 3 hours of assignment.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }


                // =================================================
                // 3. Notify Team Lead
                // New functionality
                // =================================================

                if (salesOfficerTeamLeads.TryGetValue(
                        assignment.SalesOfficerId,
                        out var teamLeadId) &&
                    teamLeadId.HasValue &&
                    teamLeadId.Value > 0)
                {
                    var teamLeadUserId =
                        teamLeadId.Value;

                    // Prevent duplicate recipient
                    if (teamLeadUserId != assignment.SalesOfficerId &&
                        teamLeadUserId != assignment.AssignedBy)
                    {
                        await _notificationService.CreateNotificationAsync(
                            teamLeadUserId,
                            NotificationType.FirstFeedbackSLAMissed,
                            "First Feedback SLA Missed",
                            $"The Sales Officer assigned to lead {leadName} did not submit the first feedback within 3 hours of assignment.",
                            assignment.LeadId,
                            assignment.AssignmentId);
                    }
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

            if (!feedbacks.Any())
            {
                return;
            }


            // -------------------------------------------------
            // Load Sales Officer IDs
            // -------------------------------------------------

            var salesOfficerIds =
                feedbacks
                    .Select(f => f.LeadAssignment!.SalesOfficerId)
                    .Distinct()
                    .ToList();

            var salesOfficerTeamLeads =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        salesOfficerIds.Contains(u.UserId) &&
                        u.IsActive)
                    .Select(u => new
                    {
                        SalesOfficerId = u.UserId,
                        TeamLeadId = u.TeamLeadId
                    })
                    .ToDictionaryAsync(
                        x => x.SalesOfficerId,
                        x => x.TeamLeadId);


            foreach (var feedback in feedbacks)
            {
                var assignment =
                    feedback.LeadAssignment!;


                // -------------------------------------------------
                // Check whether newer feedback exists
                // -------------------------------------------------

                var hasNewerFeedback =
                    await _context.Feedbacks
                        .AnyAsync(f =>
                            f.AssignmentId ==
                                feedback.AssignmentId &&
                            f.SubmittedAt >
                                feedback.SubmittedAt);

                if (hasNewerFeedback)
                {
                    continue;
                }


                // -------------------------------------------------
                // Mark SLA as missed
                // -------------------------------------------------

                feedback.NextFeedbackSLAMissed = true;

                var leadName =
                    assignment.Lead?.LeadName ?? "Lead";


                // =================================================
                // 1. Notify Sales Officer
                // =================================================

                await _notificationService.CreateNotificationAsync(
                    assignment.SalesOfficerId,
                    NotificationType.NextFeedbackOverdue,
                    "Next Feedback Overdue",
                    $"The next feedback for lead {leadName} is overdue.",
                    assignment.LeadId,
                    assignment.AssignmentId);


                // =================================================
                // 2. Notify Assigned By
                // Existing functionality preserved
                // =================================================

                if (assignment.AssignedBy > 0 &&
                    assignment.AssignedBy !=
                        assignment.SalesOfficerId)
                {
                    await _notificationService.CreateNotificationAsync(
                        assignment.AssignedBy,
                        NotificationType.NextFeedbackOverdue,
                        "Next Feedback Overdue",
                        $"The next feedback for lead {leadName} is overdue.",
                        assignment.LeadId,
                        assignment.AssignmentId);
                }


                // =================================================
                // 3. Notify Team Lead
                // New functionality
                // =================================================

                if (salesOfficerTeamLeads.TryGetValue(
                        assignment.SalesOfficerId,
                        out var teamLeadId) &&
                    teamLeadId.HasValue &&
                    teamLeadId.Value > 0)
                {
                    var teamLeadUserId =
                        teamLeadId.Value;

                    // Prevent duplicate recipient
                    if (teamLeadUserId !=
                            assignment.SalesOfficerId &&
                        teamLeadUserId !=
                            assignment.AssignedBy)
                    {
                        await _notificationService.CreateNotificationAsync(
                            teamLeadUserId,
                            NotificationType.NextFeedbackOverdue,
                            "Next Feedback Overdue",
                            $"The next feedback for lead {leadName} is overdue.",
                            assignment.LeadId,
                            assignment.AssignmentId);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}


//using CRMSystem.Data;
//using CRMSystem.Enums;
//using CRMSystem.Services.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace CRMSystem.Services
//{
//    public class SLAService : ISLAService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly INotificationService _notificationService;

//        public SLAService(
//            ApplicationDbContext context,
//            INotificationService notificationService)
//        {
//            _context = context;
//            _notificationService = notificationService;
//        }


//        // =====================================================
//        // Check Acceptance SLA
//        // =====================================================

//        public async Task CheckAcceptanceSLAsAsync()
//        {
//            var now = DateTime.UtcNow;

//            var assignments = await _context.LeadAssignments
//                .Include(a => a.Lead)
//                .Where(a =>
//                    a.AssignmentStatus == AssignmentStatus.Pending &&
//                    !a.AcceptanceSLAMissed &&
//                    a.AssignedAt.AddHours(1) < now)
//                .ToListAsync();

//            foreach (var assignment in assignments)
//            {
//                assignment.AcceptanceSLAMissed = true;

//                var leadName =
//                    assignment.Lead?.LeadName ?? "Lead";

//                await _notificationService.CreateNotificationAsync(
//                    assignment.SalesOfficerId,
//                    NotificationType.AcceptanceSLAMissed,
//                    "Lead Acceptance SLA Missed",
//                    $"The 1-hour acceptance SLA for lead {leadName} has been missed.",
//                    assignment.LeadId,
//                    assignment.AssignmentId);

//                if (assignment.AssignedBy > 0)
//                {
//                    await _notificationService.CreateNotificationAsync(
//                        assignment.AssignedBy,
//                        NotificationType.AcceptanceSLAMissed,
//                        "Sales Officer Missed Acceptance SLA",
//                        $"The Sales Officer assigned to lead {leadName} did not accept the lead within 1 hour.",
//                        assignment.LeadId,
//                        assignment.AssignmentId);
//                }
//            }

//            await _context.SaveChangesAsync();
//        }


//        // =====================================================
//        // Check First Feedback SLA
//        // =====================================================

//        public async Task CheckFirstFeedbackSLAsAsync()
//        {
//            var now = DateTime.UtcNow;

//            var assignments = await _context.LeadAssignments
//                .Include(a => a.Lead)
//                .Include(a => a.Feedbacks)
//                .Where(a =>
//                    a.AssignmentStatus == AssignmentStatus.Accepted &&
//                    a.AcceptedAt.HasValue &&
//                    !a.FirstFeedbackSLAMissed &&
//                    a.AcceptedAt.Value.AddHours(3) < now &&
//                    !a.Feedbacks.Any())
//                .ToListAsync();

//            foreach (var assignment in assignments)
//            {
//                assignment.FirstFeedbackSLAMissed = true;

//                var leadName =
//                    assignment.Lead?.LeadName ?? "Lead";

//                await _notificationService.CreateNotificationAsync(
//                    assignment.SalesOfficerId,
//                    NotificationType.FirstFeedbackSLAMissed,
//                    "First Feedback SLA Missed",
//                    $"The 3-hour first feedback SLA for lead {leadName} has been missed.",
//                    assignment.LeadId,
//                    assignment.AssignmentId);

//                if (assignment.AssignedBy > 0)
//                {
//                    await _notificationService.CreateNotificationAsync(
//                        assignment.AssignedBy,
//                        NotificationType.FirstFeedbackSLAMissed,
//                        "First Feedback SLA Missed",
//                        $"The Sales Officer assigned to lead {leadName} did not submit the first feedback within 3 hours of acceptance.",
//                        assignment.LeadId,
//                        assignment.AssignmentId);
//                }
//            }

//            await _context.SaveChangesAsync();
//        }


//        // =====================================================
//        // Check Next Feedback SLA
//        // =====================================================

//        public async Task CheckNextFeedbackSLAsAsync()
//        {
//            var now = DateTime.UtcNow;

//            var feedbacks = await _context.Feedbacks
//                .Include(f => f.LeadAssignment)
//                    .ThenInclude(a => a!.Lead)
//                .Where(f =>
//                    f.NextFollowUpDate.HasValue &&
//                    f.NextFollowUpDate.Value < now &&
//                    !f.NextFeedbackSLAMissed &&
//                    f.LeadAssignment != null)
//                .ToListAsync();

//            foreach (var feedback in feedbacks)
//            {
//                var assignment = feedback.LeadAssignment!;

//                var hasNewerFeedback = await _context.Feedbacks
//                    .AnyAsync(f =>
//                        f.AssignmentId == feedback.AssignmentId &&
//                        f.SubmittedAt > feedback.SubmittedAt);

//                if (hasNewerFeedback)
//                {
//                    continue;
//                }

//                feedback.NextFeedbackSLAMissed = true;

//                var leadName =
//                    assignment.Lead?.LeadName ?? "Lead";

//                await _notificationService.CreateNotificationAsync(
//                    assignment.SalesOfficerId,
//                    NotificationType.NextFeedbackOverdue,
//                    "Next Feedback Overdue",
//                    $"The next feedback for lead {leadName} is overdue.",
//                    assignment.LeadId,
//                    assignment.AssignmentId);

//                if (assignment.AssignedBy > 0)
//                {
//                    await _notificationService.CreateNotificationAsync(
//                        assignment.AssignedBy,
//                        NotificationType.NextFeedbackOverdue,
//                        "Next Feedback Overdue",
//                        $"The next feedback for lead {leadName} is overdue.",
//                        assignment.LeadId,
//                        assignment.AssignmentId);
//                }
//            }

//            await _context.SaveChangesAsync();
//        }
//    }
//}