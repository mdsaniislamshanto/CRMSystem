using CRMSystem.Data;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using CRMSystem.Enums;
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

        public async Task<SalesManagerDashboardViewModel> GetDashboardAsync()
        {
            var model = new SalesManagerDashboardViewModel();

            // ==========================
            // Total New Leads
            // ==========================
            model.TotalNewLeads = await _context.Leads
                .CountAsync(l => l.Status == LeadStatus.New);

     

            // ==========================
            // Auto Assignment Status
            // ==========================
            var systemSettings = await _context.SystemSettings
                .FirstOrDefaultAsync();

            if (systemSettings != null)
            {
                model.AutoAssignmentEnabled = systemSettings.AutoAssignmentEnabled;
            }

            return model;
        }
    }
}