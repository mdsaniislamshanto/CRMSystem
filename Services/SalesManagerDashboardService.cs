using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SalesManagerDashboardService : ISalesManagerDashboardService
    {
        private readonly ApplicationDbContext _context;

        public SalesManagerDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Sales Manager Dashboard
        // =====================================================

        public async Task<SalesManagerDashboardViewModel> GetDashboardAsync()
        {
            var model = new SalesManagerDashboardViewModel();

            // =====================================================
            // Date Range
            // =====================================================

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var now = DateTime.UtcNow;

            // =====================================================
            // New Leads Today
            // =====================================================

            model.NewLeadsToday = await _context.Leads
                .CountAsync(l =>
                    !l.IsDeleted &&
                    !l.IsArchived &&
                    l.Status == LeadStatus.New &&
                    l.CreatedAt >= today &&
                    l.CreatedAt < tomorrow);

            // =====================================================
            // Unassigned Leads
            // =====================================================

            model.TotalUnassignedLeads = await _context.Leads
                .CountAsync(l =>
                    !l.IsDeleted &&
                    !l.IsArchived &&
                    l.Status == LeadStatus.New &&
                    !_context.LeadAssignments.Any(a =>
                        a.LeadId == l.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted));

            // =====================================================
            // Assigned Today
            // =====================================================

            model.AssignedToday = await _context.LeadAssignments
                .CountAsync(a =>
                    !a.IsDeleted &&
                    a.AssignedAt >= today &&
                    a.AssignedAt < tomorrow);

            // =====================================================
            // Completed Today
            // =====================================================

            model.CompletedToday = await _context.Leads
                .CountAsync(l =>
                    !l.IsDeleted &&
                    !l.IsArchived &&
                    l.Status == LeadStatus.Completed &&
                    l.UpdatedAt >= today &&
                    l.UpdatedAt < tomorrow);

            // =====================================================
            // Pending Assignments
            // =====================================================

            model.PendingAssignments =
                model.TotalUnassignedLeads;

            // =====================================================
            // Missed 1-Hour Acceptance SLA
            // =====================================================

            model.MissedAcceptanceSLA =
                await _context.LeadAssignments
                    .CountAsync(a =>
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.AcceptanceSLAMissed);

            // =====================================================
            // Missed 3-Hour First Feedback SLA
            // =====================================================

            model.MissedFirstFeedbackSLA =
                await _context.LeadAssignments
                    .CountAsync(a =>
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.FirstFeedbackSLAMissed);

            // =====================================================
            // Follow-ups Due Today
            // =====================================================

            var followUpFeedbacks =
                await _context.Feedbacks
                    .Where(f =>
                        f.IsActive &&
                        !f.IsDeleted &&
                        f.NextFollowUpDate.HasValue &&
                        f.LeadAssignment != null &&
                        f.LeadAssignment.IsActive &&
                        !f.LeadAssignment.IsDeleted &&
                        f.LeadAssignment.Lead != null &&
                        !f.LeadAssignment.Lead.IsDeleted &&
                        !f.LeadAssignment.Lead.IsArchived)
                    .Select(f => new
                    {
                        f.AssignmentId,
                        LeadId = f.LeadAssignment!.LeadId,
                        f.SubmittedAt,
                        f.NextFollowUpDate
                    })
                    .AsNoTracking()
                    .ToListAsync();

            // =====================================================
            // Only Latest Feedback Per Lead
            // =====================================================

            var latestFollowUps =
                followUpFeedbacks
                    .GroupBy(f => f.LeadId)
                    .Select(g =>
                        g.OrderByDescending(f => f.SubmittedAt)
                         .First())
                    .ToList();

            model.DueFollowUps =
                latestFollowUps.Count(f =>
                    f.NextFollowUpDate!.Value >= today &&
                    f.NextFollowUpDate.Value < tomorrow);

            // =====================================================
            // Leads Archived Today
            // =====================================================

            model.ArchivedToday =
                await _context.Leads
                    .CountAsync(l =>
                        l.IsArchived &&
                        !l.IsDeleted &&
                        l.ArchivedAt.HasValue &&
                        l.ArchivedAt.Value >= today &&
                        l.ArchivedAt.Value < tomorrow);

            // =====================================================
            // Auto Assignment Status
            // =====================================================

            var systemSettings =
                await _context.SystemSettings
                    .FirstOrDefaultAsync();

            if (systemSettings != null)
            {
                model.AutoAssignmentEnabled =
                    systemSettings.AutoAssignmentEnabled;
            }

            return model;
        }
    }
}