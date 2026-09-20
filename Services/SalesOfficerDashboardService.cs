using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SalesOfficerDashboardService : ISalesOfficerDashboardService
    {
        private readonly ApplicationDbContext _context;

        public SalesOfficerDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesOfficerDashboardViewModel> GetDashboardAsync(
            long salesOfficerId)
        {
            var dashboard = new SalesOfficerDashboardViewModel();

            // ==========================================
            // Lead Statistics
            // ==========================================

            dashboard.TotalAssigned = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId);

            dashboard.PendingLeads = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.AssignmentStatus == AssignmentStatus.Pending);

            dashboard.AcceptedLeads = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.AssignmentStatus == AssignmentStatus.Accepted);

            dashboard.CompletedLeads = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.Lead != null &&
                    a.Lead.Status == LeadStatus.Completed);

            // ==========================================
            // Current Target
            // ==========================================

            var today = DateTime.UtcNow.Date;

            var currentTarget = await _context.SalesTargets
                .AsNoTracking()
                .Where(t =>
                    t.UserId == salesOfficerId &&
                    !t.IsDeleted &&
                    t.StartDate <= today &&
                    t.EndDate >= today)
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefaultAsync();

            if (currentTarget != null)
            {
                dashboard.TotalTarget = currentTarget.TargetCount;

                // ==========================================
                // Target Fulfillment
                // ==========================================

                dashboard.TargetFulfilled =
                    await _context.LeadAssignments
                        .AsNoTracking()
                        .Where(a =>
                            a.SalesOfficerId == salesOfficerId &&
                            a.Lead != null &&
                            a.Feedbacks.Any(f =>
                                f.Status == FeedbackStatus.Completed &&
                                f.SubmittedAt >= currentTarget.StartDate &&
                                f.SubmittedAt <
                                    currentTarget.EndDate.Date.AddDays(1)))
                        .Select(a => a.LeadId)
                        .Distinct()
                        .CountAsync();
            }

            return dashboard;
        }
    }
}