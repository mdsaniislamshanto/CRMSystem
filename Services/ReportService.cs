using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminReportViewModel> GetAdminReportAsync()
        {
            // =====================================================
            // 1. Lead Summary
            // =====================================================

            var totalLeads = await _context.Leads
                .CountAsync(l => !l.IsArchived);

            var newLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.New);

            var assignedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.Assigned);

            var acceptedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.Accepted);

            var inProgressLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.InProgress);

            var completedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.Completed);

            var rejectedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    l.Status == LeadStatus.Rejected);


            // =====================================================
            // 2. Unassigned Leads
            // =====================================================

            var unassignedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsArchived &&
                    !_context.LeadAssignments
                        .Any(a => a.LeadId == l.LeadId));


            // =====================================================
            // 3. Sales Officer Performance
            // =====================================================

            var salesOfficerReports =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName == "Sales Officer")
                    .Select(u => new SalesOfficerReportViewModel
                    {
                        UserId = u.UserId,

                        EmployeeCode =
                            u.EmployeeCode,

                        FullName =
                            u.FullName,

                        AssignedLeads =
                            _context.LeadAssignments
                                .Count(a =>
                                    a.SalesOfficerId == u.UserId),

                        AcceptedLeads =
                            _context.LeadAssignments
                                .Count(a =>
                                    a.SalesOfficerId == u.UserId &&
                                    a.AssignmentStatus ==
                                    AssignmentStatus.Accepted),

                        InProgressLeads =
                            _context.Leads
                                .Count(l =>
                                    !l.IsArchived &&
                                    l.Status ==
                                    LeadStatus.InProgress &&
                                    _context.LeadAssignments
                                        .Any(a =>
                                            a.LeadId == l.LeadId &&
                                            a.SalesOfficerId ==
                                            u.UserId)),

                        CompletedLeads =
                            _context.Leads
                                .Count(l =>
                                    !l.IsArchived &&
                                    l.Status ==
                                    LeadStatus.Completed &&
                                    _context.LeadAssignments
                                        .Any(a =>
                                            a.LeadId == l.LeadId &&
                                            a.SalesOfficerId ==
                                            u.UserId)),

                        RejectedLeads =
                            _context.Leads
                                .Count(l =>
                                    !l.IsArchived &&
                                    l.Status ==
                                    LeadStatus.Rejected &&
                                    _context.LeadAssignments
                                        .Any(a =>
                                            a.LeadId == l.LeadId &&
                                            a.SalesOfficerId ==
                                            u.UserId))
                    })
                    .ToListAsync();


            // =====================================================
            // 4. Lead Source Summary
            // =====================================================

            var leadSourceReports =
                await _context.Leads
                    .Where(l => !l.IsArchived)
                    .GroupBy(l => l.Source)
                    .Select(g => new LeadSourceReportViewModel
                    {
                        Source = g.Key.ToString(),

                        LeadCount = g.Count()
                    })
                    .OrderByDescending(x => x.LeadCount)
                    .ToListAsync();


            // =====================================================
            // 5. Build Final Report ViewModel
            // =====================================================

            return new AdminReportViewModel
            {
                TotalLeads = totalLeads,

                NewLeads = newLeads,

                AssignedLeads = assignedLeads,

                AcceptedLeads = acceptedLeads,

                InProgressLeads = inProgressLeads,

                CompletedLeads = completedLeads,

                RejectedLeads = rejectedLeads,

                UnassignedLeads = unassignedLeads,

                SalesOfficerReports = salesOfficerReports,

                LeadSourceReports = leadSourceReports
            };
        }



        // =====================================================
        // Sales Manager Report
        // =====================================================

        public async Task<SalesManagerReportViewModel>
            GetSalesManagerReportAsync()
        {
            // =====================================================
            // 1. Get Active Sales Officers
            // =====================================================

            var salesOfficers = await _context.Users
                .Where(u =>
                    u.IsActive &&
                    u.Role != null &&
                    u.Role.RoleKey == "SALES_OFFICER")
                .ToListAsync();


            // =====================================================
            // 2. Get Assignments
            // =====================================================

            var assignments = await _context.LeadAssignments
                .Where(a =>
                    salesOfficers
                        .Select(u => u.UserId)
                        .Contains(a.SalesOfficerId))
                .ToListAsync();


            // =====================================================
            // 3. Get Feedbacks
            // =====================================================

            var feedbacks = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                .Where(f =>
                    f.LeadAssignment != null &&
                    salesOfficers
                        .Select(u => u.UserId)
                        .Contains(
                            f.LeadAssignment.SalesOfficerId))
                .ToListAsync();


            // =====================================================
            // 4. Build Officer-wise Reports
            // =====================================================

            var officerReports =
                new List<SalesManagerOfficerReportViewModel>();


            foreach (var officer in salesOfficers)
            {
                var officerAssignments =
                    assignments
                        .Where(a =>
                            a.SalesOfficerId ==
                            officer.UserId)
                        .ToList();


                var officerFeedbacks =
                    feedbacks
                        .Where(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId ==
                            officer.UserId)
                        .ToList();


                // =================================================
                // Lead Metrics
                // =================================================

                var assignedCount =
                    officerAssignments.Count;


                var acceptedCount =
                    officerAssignments.Count(a =>
                        a.AcceptedAt != null);


                var pendingCount =
                    officerAssignments.Count(a =>
                        a.AcceptedAt == null);


                var acceptanceRate = 0.0;

                if (assignedCount > 0)
                {
                    acceptanceRate =
                        (double)acceptedCount /
                        assignedCount *
                        100;
                }


                // =================================================
                // Acceptance SLA
                // =================================================

                var acceptanceSLAMissed =
                    officerAssignments.Count(a =>
                        a.AcceptanceSLAMissed);


                // =================================================
                // First Feedback SLA
                // =================================================

                var firstFeedbackSLAMissed =
                    officerAssignments.Count(a =>
                        a.FirstFeedbackSLAMissed);


                // =================================================
                // Total Feedbacks
                // =================================================

                var totalFeedbacks =
                    officerFeedbacks.Count;


                // =================================================
                // Latest Feedback Per Assignment
                // =================================================

                var latestFeedbacks =
                    officerFeedbacks
                        .GroupBy(f =>
                            f.AssignmentId)
                        .Select(g =>
                            g.OrderByDescending(f =>
                                f.SubmittedAt)
                             .First())
                        .ToList();


                // =================================================
                // Completed Leads
                // =================================================

                var completedLeads =
                    latestFeedbacks.Count(f =>
                        f.Status ==
                        FeedbackStatus.Completed);


                // =================================================
                // Overdue Follow-ups
                // =================================================

                var overdueFollowUps =
                    officerFeedbacks.Count(f =>
                        f.NextFeedbackSLAMissed);


                // =================================================
                // Add Officer Report
                // =================================================

                officerReports.Add(
                    new SalesManagerOfficerReportViewModel
                    {
                        UserId =
                            officer.UserId,

                        EmployeeCode =
                            officer.EmployeeCode ?? string.Empty,

                        FullName =
                            officer.FullName,

                        AssignedLeads =
                            assignedCount,

                        AcceptedLeads =
                            acceptedCount,

                        PendingAcceptance =
                            pendingCount,

                        AcceptanceRate =
                            Math.Round(
                                acceptanceRate,
                                2),

                        CompletedLeads =
                            completedLeads,

                        TotalFeedbacks =
                            totalFeedbacks,

                        AcceptanceSLAMissed =
                            acceptanceSLAMissed,

                        FirstFeedbackSLAMissed =
                            firstFeedbackSLAMissed,

                        OverdueFollowUps =
                            overdueFollowUps
                    });
            }


            // =====================================================
            // 5. Team Summary
            // =====================================================

            var totalAssigned =
                assignments.Count;


            var totalAccepted =
                assignments.Count(a =>
                    a.AcceptedAt != null);


            var totalPending =
                assignments.Count(a =>
                    a.AcceptedAt == null);


            var overallAcceptanceRate = 0.0;

            if (totalAssigned > 0)
            {
                overallAcceptanceRate =
                    (double)totalAccepted /
                    totalAssigned *
                    100;
            }


            // =====================================================
            // Team Completed Leads
            // =====================================================

            var teamLatestFeedbacks =
                feedbacks
                    .GroupBy(f =>
                        f.AssignmentId)
                    .Select(g =>
                        g.OrderByDescending(f =>
                            f.SubmittedAt)
                         .First())
                    .ToList();


            var totalCompleted =
                teamLatestFeedbacks.Count(f =>
                    f.Status ==
                    FeedbackStatus.Completed);


            // =====================================================
            // Team SLA Metrics
            // =====================================================

            var totalAcceptanceSLAMissed =
                assignments.Count(a =>
                    a.AcceptanceSLAMissed);


            var totalFirstFeedbackSLAMissed =
                assignments.Count(a =>
                    a.FirstFeedbackSLAMissed);


            var totalOverdueFollowUps =
                feedbacks.Count(f =>
                    f.NextFeedbackSLAMissed);


            // =====================================================
            // Return Final Report
            // =====================================================

            return new SalesManagerReportViewModel
            {
                TotalSalesOfficers =
                    salesOfficers.Count,

                TotalAssignedLeads =
                    totalAssigned,

                TotalAcceptedLeads =
                    totalAccepted,

                TotalPendingAcceptance =
                    totalPending,

                OverallAcceptanceRate =
                    Math.Round(
                        overallAcceptanceRate,
                        2),

                TotalCompletedLeads =
                    totalCompleted,

                TotalFeedbacks =
                    feedbacks.Count,

                TotalAcceptanceSLAMissed =
                    totalAcceptanceSLAMissed,

                TotalFirstFeedbackSLAMissed =
                    totalFirstFeedbackSLAMissed,

                TotalOverdueFollowUps =
                    totalOverdueFollowUps,

                SalesOfficerReports =
                    officerReports
            };
        }

    }
}