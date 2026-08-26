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
    }
}