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

        public async Task<SalesOfficerDashboardViewModel> GetDashboardAsync(long salesOfficerId)
        {
            var dashboard = new SalesOfficerDashboardViewModel();

            dashboard.TotalAssigned = await _context.LeadAssignments
                .CountAsync(a => a.SalesOfficerId == salesOfficerId);

            dashboard.PendingLeads = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.AssignmentStatus == AssignmentStatus.Pending);

            dashboard.AcceptedLeads = await _context.LeadAssignments
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.AssignmentStatus == AssignmentStatus.Accepted);

            dashboard.CompletedLeads = await _context.LeadAssignments
                .Include(a => a.Lead)
                .CountAsync(a =>
                    a.SalesOfficerId == salesOfficerId &&
                    a.Lead != null &&
                    a.Lead.Status == LeadStatus.Completed);

            return dashboard;
        }
    }
}