using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Services.Interfaces;
using CRMSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITargetService _targetService;

        public AdminDashboardService(
            ApplicationDbContext context,
            ITargetService targetService)
        {
            _context = context;
            _targetService = targetService;
        }

        // =====================================================
        // Admin Dashboard
        // =====================================================

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var model = new AdminDashboardViewModel();

            // =================================================
            // Lead Status Statistics
            // =================================================

            model.NewLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.New);

            model.AssignedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.Assigned);

            model.AcceptedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.Accepted);

            model.InProgressLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.InProgress);

            model.CompletedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.Completed);

            model.RejectedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.Rejected);


            // =================================================
            // Lead Overview
            // =================================================

            model.TotalLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived);


            // =================================================
            // Unassigned Leads
            // =================================================

            model.UnassignedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.Status == LeadStatus.New &&
                        !_context.LeadAssignments.Any(a =>
                            a.LeadId == l.LeadId &&
                            a.IsActive &&
                            !a.IsDeleted));


            // =================================================
            // Archived Leads
            // =================================================

            model.ArchivedLeads =
                await _context.Leads
                    .CountAsync(l =>
                        l.IsArchived &&
                        !l.IsDeleted);


            // =================================================
            // User / Workforce Statistics
            // =================================================

            var users =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .Where(u => !u.IsDeleted)
                    .Select(u => new
                    {
                        u.IsActive,
                        RoleKey = u.Role != null
                            ? u.Role.RoleKey
                            : null
                    })
                    .ToListAsync();


            model.TotalUsers =
                users.Count;

            model.ActiveUsers =
                users.Count(u =>
                    u.IsActive);

            model.InactiveUsers =
                users.Count(u =>
                    !u.IsActive);

            model.AdminUsers =
                users.Count(u =>
                    u.RoleKey == "ADMIN");

            model.SalesManagers =
                users.Count(u =>
                    u.RoleKey == "SALES_MANAGER");

            model.TeamLeads =
                users.Count(u =>
                    u.RoleKey == "TEAM_LEAD");

            model.SalesOfficers =
                users.Count(u =>
                    u.RoleKey == "SALES_OFFICER");

            model.AccountUsers =
                users.Count(u =>
                    u.RoleKey == "ACCOUNT");

            model.HRUsers =
                users.Count(u =>
                    u.RoleKey == "HR");


            // =================================================
            // Overall Sales Manager Target Overview
            // =================================================

            var targetSummary =
                await _targetService
                    .GetAdminTargetSummaryAsync();

            model.TotalTarget =
                targetSummary.TotalTarget;

            model.TargetFulfilled =
                targetSummary.TargetFulfilled;

            model.TargetProgress =
                model.TotalTarget > 0
                    ? Math.Round(
                        (decimal)model.TargetFulfilled /
                        model.TotalTarget *
                        100,
                        2)
                    : 0;



            // =================================================
            // Monthly Lead Trend - Last 6 Months
            // =================================================

            var currentMonth = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1);

            var firstMonth =
                currentMonth.AddMonths(-5);

            var monthlyLeads =
                await _context.Leads
                    .AsNoTracking()
                    .Where(l =>
                        !l.IsDeleted &&
                        !l.IsArchived &&
                        l.CreatedAt >= firstMonth)
                    .Select(l => new
                    {
                        l.CreatedAt
                    })
                    .ToListAsync();

            model.MonthlyLeadTrend =
                Enumerable.Range(0, 6)
                    .Select(i =>
                    {
                        var month =
                            firstMonth.AddMonths(i);

                        return new MonthlyLeadTrendItem
                        {
                            Month = month.ToString("MMM"),

                            LeadCount =
                                monthlyLeads.Count(l =>
                                    l.CreatedAt.Year == month.Year &&
                                    l.CreatedAt.Month == month.Month)
                        };
                    })
                    .ToList();

            // =================================================
            // Return Dashboard Model
            // =================================================

            return model;
        }
    }
}