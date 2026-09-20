using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class TeamLeadPerformanceService
        : ITeamLeadPerformanceService
    {
        private readonly ApplicationDbContext _context;

        private readonly ISalesOfficerPerformanceService
            _salesOfficerPerformanceService;


        // =========================================================
        // Constructor
        // =========================================================

        public TeamLeadPerformanceService(
            ApplicationDbContext context,
            ISalesOfficerPerformanceService
                salesOfficerPerformanceService)
        {
            _context = context;

            _salesOfficerPerformanceService =
                salesOfficerPerformanceService;
        }


        // =========================================================
        // Get Team Performance
        // =========================================================

        public async Task<List<SalesOfficerPerformanceViewModel>>
            GetTeamPerformanceAsync(
                long teamLeadId,
                PerformanceFilterViewModel? filter = null)
        {
            // -----------------------------------------------------
            // 1. Verify Current Team Lead
            // -----------------------------------------------------

            var isTeamLead =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.UserId == teamLeadId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.TeamLead &&
                        u.IsActive &&
                        !u.IsDeleted);

            if (!isTeamLead)
            {
                return new List<
                    SalesOfficerPerformanceViewModel>();
            }


            // -----------------------------------------------------
            // 2. Get Sales Officers Of This Team
            // -----------------------------------------------------

            var salesOfficerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.TeamLeadId == teamLeadId &&

                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.SalesOfficer &&

                        u.IsActive &&
                        !u.IsDeleted)
                    .Select(u => u.UserId)
                    .ToListAsync();


            // -----------------------------------------------------
            // 3. No Team Members
            // -----------------------------------------------------

            if (salesOfficerIds.Count == 0)
            {
                return new List<
                    SalesOfficerPerformanceViewModel>();
            }


            // -----------------------------------------------------
            // 4. Get Existing Performance Calculation
            //    For Each Team Member
            // -----------------------------------------------------

            var performance =
                new List<
                    SalesOfficerPerformanceViewModel>();


            foreach (var salesOfficerId
                     in salesOfficerIds)
            {
                var officerPerformance =
                    await _salesOfficerPerformanceService
                        .GetPerformanceAsync(
                            salesOfficerId,
                            filter);


                if (officerPerformance != null)
                {
                    performance.Add(
                        officerPerformance);
                }
            }


            // -----------------------------------------------------
            // 5. Rank By Performance Score
            // -----------------------------------------------------

            return performance
                .OrderByDescending(
                    x => x.PerformanceScore)
                .ThenBy(
                    x => x.SalesOfficerName)
                .ToList();
        }
    }
}