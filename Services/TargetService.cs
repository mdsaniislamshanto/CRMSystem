using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.DTOs;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class TargetService : ITargetService
    {
        private readonly ApplicationDbContext _context;

        // =====================================================
        // Notification Service
        // =====================================================

        private readonly INotificationService
            _notificationService;


        public TargetService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context =
                context;

            _notificationService =
                notificationService;
        }


        // =====================================================
        // Get Create Target ViewModel
        // Loads active Sales Managers for Admin
        // =====================================================

        public async Task<CreateTargetViewModel>
            GetCreateTargetViewModelAsync()
        {
            var model =
                new CreateTargetViewModel();

            var salesManagers =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "SALES_MANAGER")
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

            model.Users =
                salesManagers.Select(u =>
                    new SelectListItem
                    {
                        Value =
                            u.UserId.ToString(),

                        Text =
                            string.IsNullOrWhiteSpace(
                                u.LastName)
                                ? u.FirstName
                                : $"{u.FirstName} {u.LastName}"
                    });

            return model;
        }


        // =====================================================
        // Create Target
        // Admin → Sales Manager
        // Sales Manager → Sales Officer
        // Existing functionality preserved
        // =====================================================

        public async Task<ServiceResult> CreateTargetAsync(
            CreateTargetViewModel model,
            long createdByUserId)
        {
            if (model.TargetCount <= 0)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Target count must be greater than zero."
                };
            }

            if (model.StartDate.Date >
                model.EndDate.Date)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Start date cannot be after end date."
                };
            }

            var targetUser =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == model.UserId &&
                        u.IsActive);

            if (targetUser == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Selected user was not found or is inactive."
                };
            }

            var creator =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == createdByUserId &&
                        u.IsActive);

            if (creator == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Target creator was not found or is inactive."
                };
            }

            var targetUserRole =
                targetUser.Role?.RoleKey;

            var creatorRole =
                creator.Role?.RoleKey;


            // -------------------------------------------------
            // Target can only be assigned to Manager or Officer
            // Existing functionality preserved
            // -------------------------------------------------

            if (targetUserRole != "SALES_MANAGER" &&
                targetUserRole != "SALES_OFFICER")
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Targets can only be assigned to Sales Managers or Sales Officers."
                };
            }


            // -------------------------------------------------
            // Admin → Sales Manager
            // -------------------------------------------------

            if (creatorRole == "ADMIN")
            {
                if (targetUserRole != "SALES_MANAGER")
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        Message =
                            "Admin can assign targets only to Sales Managers."
                    };
                }
            }


            // -------------------------------------------------
            // Sales Manager → Own Sales Officer
            // Existing functionality preserved
            // -------------------------------------------------

            else if (creatorRole == "SALES_MANAGER")
            {
                if (targetUserRole != "SALES_OFFICER")
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        Message =
                            "Sales Manager can assign targets only to Sales Officers."
                    };
                }

                if (targetUser.SalesManagerId !=
                    createdByUserId)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        Message =
                            "You can only assign targets to your own Sales Officers."
                    };
                }
            }


            // -------------------------------------------------
            // Other roles are not authorized
            // -------------------------------------------------

            else
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "You are not authorized to create targets."
                };
            }


            // -------------------------------------------------
            // Prevent overlapping targets
            // -------------------------------------------------

            var overlappingTarget =
                await _context.SalesTargets
                    .AnyAsync(t =>
                        t.UserId == model.UserId &&
                        !t.IsDeleted &&
                        t.StartDate.Date <=
                            model.EndDate.Date &&
                        t.EndDate.Date >=
                            model.StartDate.Date);

            if (overlappingTarget)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "An overlapping target already exists for this user."
                };
            }


            // -------------------------------------------------
            // Create target
            // -------------------------------------------------

            var target =
                new SalesTarget
                {
                    UserId =
                        model.UserId,

                    PeriodType =
                        model.PeriodType,

                    TargetCount =
                        model.TargetCount,

                    StartDate =
                        model.StartDate.Date,

                    EndDate =
                        model.EndDate.Date
                };

            target.CreatedBy =
                createdByUserId;

            _context.SalesTargets.Add(target);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Target created successfully."
            };
        }


        // =====================================================
        // Get Target Achievements For Admin
        // Admin → Sales Manager Targets Only
        // =====================================================

        public async Task<List<TargetAchievementViewModel>>
            GetTargetAchievementsForAdminAsync()
        {
            var targets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsDeleted &&
                        t.User != null &&
                        t.User.Role != null &&
                        t.User.Role.RoleKey ==
                            "SALES_MANAGER")
                    .Include(t => t.User)
                        .ThenInclude(u => u!.Role)
                    .OrderByDescending(
                        t => t.StartDate)
                    .ToListAsync();

            var result =
                new List<TargetAchievementViewModel>();

            foreach (var target in targets)
            {
                var completedCount =
                    await GetCompletedLeadCountAsync(
                        target.UserId,
                        target.StartDate,
                        target.EndDate);

                result.Add(
                    BuildTargetAchievement(
                        target,
                        completedCount));
            }

            return result;
        }


        // =====================================================
        // Get Officer Target Achievements
        // Existing functionality preserved
        // =====================================================

        public async Task<List<OfficerTargetAchievementViewModel>>
            GetOfficerTargetAchievementsAsync(
                long salesManagerId)
        {
            var officerIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.IsActive &&
                        u.SalesManagerId ==
                            salesManagerId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "SALES_OFFICER")
                    .Select(u => u.UserId)
                    .ToListAsync();

            var targets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsDeleted &&
                        officerIds.Contains(t.UserId))
                    .Include(t => t.User)
                    .OrderByDescending(
                        t => t.StartDate)
                    .ToListAsync();

            var result =
                new List<OfficerTargetAchievementViewModel>();

            foreach (var target in targets)
            {
                var completedCount =
                    await GetCompletedLeadCountAsync(
                        target.UserId,
                        target.StartDate,
                        target.EndDate);

                result.Add(
                    new OfficerTargetAchievementViewModel
                    {
                        TargetId =
                            target.TargetId,

                        SalesOfficerId =
                            target.UserId,

                        SalesOfficerName =
                            target.User?.FullName ??
                            "Unknown",

                        PeriodType =
                            target.PeriodType.ToString(),

                        TargetCount =
                            target.TargetCount,

                        CompletedCount =
                            completedCount,

                        StartDate =
                            target.StartDate,

                        EndDate =
                            target.EndDate,

                        AchievementPercentage =
                            CalculateAchievementPercentage(
                                completedCount,
                                target.TargetCount),

                        RemainingCount =
                            Math.Max(
                                target.TargetCount -
                                completedCount,
                                0),

                        Status =
                            GetTargetStatus(
                                completedCount,
                                target.TargetCount)
                    });
            }

            return result;
        }


        // =====================================================
        // Get Create Team Lead Target ViewModel
        // Sales Manager → Team Lead
        // =====================================================

        public async Task<CreateTargetViewModel>
            GetCreateTeamLeadTargetViewModelAsync(
                long salesManagerId)
        {
            var model =
                new CreateTargetViewModel();

            var salesManager =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == salesManagerId &&
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "SALES_MANAGER");

            if (salesManager == null)
            {
                return model;
            }

            var teamLeads =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.IsActive &&
                        u.SalesManagerId ==
                            salesManagerId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "TEAM_LEAD")
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

            model.Users =
                teamLeads.Select(u =>
                    new SelectListItem
                    {
                        Value =
                            u.UserId.ToString(),

                        Text =
                            string.IsNullOrWhiteSpace(
                                u.LastName)
                                ? u.FirstName
                                : $"{u.FirstName} {u.LastName}"
                    });

            return model;
        }


        // =====================================================
        // Create Team Lead Target
        // Sales Manager → Own Team Lead
        // =====================================================

        public async Task<ServiceResult>
            CreateTeamLeadTargetAsync(
                CreateTargetViewModel model,
                long salesManagerId)
        {
            if (model.TargetCount <= 0)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Target count must be greater than zero."
                };
            }

            if (model.StartDate.Date >
                model.EndDate.Date)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Start date cannot be after end date."
                };
            }

            var salesManager =
                await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == salesManagerId &&
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "SALES_MANAGER");

            if (salesManager == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Sales Manager was not found or is inactive."
                };
            }

            var teamLead =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == model.UserId &&
                        u.IsActive &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "TEAM_LEAD");

            if (teamLead == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Selected Team Lead was not found or is inactive."
                };
            }


            // -------------------------------------------------
            // Ownership validation
            // -------------------------------------------------

            if (teamLead.SalesManagerId !=
                salesManagerId)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "You can only assign targets to your own Team Leads."
                };
            }


            // -------------------------------------------------
            // Prevent overlapping targets
            // -------------------------------------------------

            var overlappingTarget =
                await _context.SalesTargets
                    .AnyAsync(t =>
                        t.UserId == model.UserId &&
                        !t.IsDeleted &&
                        t.StartDate.Date <=
                            model.EndDate.Date &&
                        t.EndDate.Date >=
                            model.StartDate.Date);

            if (overlappingTarget)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "An overlapping target already exists for this Team Lead."
                };
            }


            // -------------------------------------------------
            // Create Team Lead target
            // -------------------------------------------------

            var target =
                new SalesTarget
                {
                    UserId =
                        teamLead.UserId,

                    PeriodType =
                        model.PeriodType,

                    TargetCount =
                        model.TargetCount,

                    StartDate =
                        model.StartDate.Date,

                    EndDate =
                        model.EndDate.Date,

                    CreatedBy =
                        salesManagerId
                };

            _context.SalesTargets.Add(target);

            await _context.SaveChangesAsync();


            // =================================================
            // Team Lead Notification
            // Sales Manager → Team Lead
            // =================================================

            await _notificationService
                .CreateNotificationAsync(
                    teamLead.UserId,
                    NotificationType.TargetAssigned,
                    "New Target Assigned",
                    $"A new target of {target.TargetCount} leads has been assigned to you by {salesManager.FullName} for the period {target.StartDate:dd MMM yyyy} to {target.EndDate:dd MMM yyyy}.");


            // =================================================
            // Success
            // =================================================

            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Team Lead target created successfully."
            };
        }


        // =====================================================
        // Get Team Lead Target Achievements
        // Sales Manager → Own Team Leads
        // =====================================================

        public async Task<List<TargetAchievementViewModel>>
            GetTeamLeadTargetAchievementsAsync(
                long salesManagerId)
        {
            var teamLeadIds =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.IsActive &&
                        u.SalesManagerId ==
                            salesManagerId &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            "TEAM_LEAD")
                    .Select(u => u.UserId)
                    .ToListAsync();

            if (!teamLeadIds.Any())
            {
                return new List<TargetAchievementViewModel>();
            }

            var targets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsDeleted &&
                        teamLeadIds.Contains(t.UserId))
                    .Include(t => t.User)
                        .ThenInclude(u => u!.Role)
                    .OrderByDescending(
                        t => t.StartDate)
                    .ToListAsync();

            var result =
                new List<TargetAchievementViewModel>();

            foreach (var target in targets)
            {
                var completedCount =
                    await GetCompletedLeadCountAsync(
                        target.UserId,
                        target.StartDate,
                        target.EndDate);

                result.Add(
                    BuildTargetAchievement(
                        target,
                        completedCount));
            }

            return result;
        }


        // =====================================================
        // Get Completed Lead Count
        // =====================================================

        public async Task<int>
            GetCompletedLeadCountAsync(
                long userId,
                DateTime startDate,
                DateTime endDate)
        {
            var roleKey =
                await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.UserId == userId)
                    .Select(u =>
                        u.Role!.RoleKey)
                    .FirstOrDefaultAsync();


            // -------------------------------------------------
            // Sales Manager
            // Count completed leads of all officers
            // under all Team Leads of this Manager
            // -------------------------------------------------

            if (roleKey == "SALES_MANAGER")
            {
                // =================================================
                // Step 1: Get all Team Leads under this Sales Manager
                // =================================================

                var teamLeadIds =
                    await _context.Users
                        .AsNoTracking()
                        .Where(u =>
                            u.IsActive &&
                            u.SalesManagerId == userId &&
                            u.Role != null &&
                            u.Role.RoleKey == "TEAM_LEAD")
                        .Select(u => u.UserId)
                        .ToListAsync();


                if (!teamLeadIds.Any())
                {
                    return 0;
                }


                // =================================================
                // Step 2: Get all Sales Officers under those Team Leads
                // =================================================

                var officerIds =
                    await _context.Users
                        .AsNoTracking()
                        .Where(u =>
                            u.IsActive &&
                            u.TeamLeadId.HasValue &&
                            teamLeadIds.Contains(u.TeamLeadId.Value) &&
                            u.Role != null &&
                            u.Role.RoleKey == "SALES_OFFICER")
                        .Select(u => u.UserId)
                        .ToListAsync();


                if (!officerIds.Any())
                {
                    return 0;
                }


                // =================================================
                // Step 3: Count completed leads of all those officers
                // within the target period
                // =================================================

                return await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        officerIds.Contains(a.SalesOfficerId) &&
                        a.Lead != null &&
                        a.Lead.Status == LeadStatus.Completed &&
                        a.Lead.UpdatedAt >= startDate &&
                        a.Lead.UpdatedAt <
                            endDate.Date.AddDays(1))
                    .Select(a => a.LeadId)
                    .Distinct()
                    .CountAsync();
            }


            // -------------------------------------------------
            // Team Lead
            // Count completed leads of own team officers
            // -------------------------------------------------

            if (roleKey == "TEAM_LEAD")
            {
                var officerIds =
                    await _context.Users
                        .AsNoTracking()
                        .Where(u =>
                            u.TeamLeadId ==
                                userId &&
                            u.IsActive &&
                            u.Role != null &&
                            u.Role.RoleKey ==
                                "SALES_OFFICER")
                        .Select(u => u.UserId)
                        .ToListAsync();

                if (!officerIds.Any())
                {
                    return 0;
                }

                return await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        officerIds.Contains(
                            a.SalesOfficerId) &&
                        a.Lead != null &&
                        a.Lead.Status ==
                            LeadStatus.Completed &&
                        a.Lead.UpdatedAt >=
                            startDate &&
                        a.Lead.UpdatedAt <
                            endDate.Date.AddDays(1))
                    .Select(a => a.LeadId)
                    .Distinct()
                    .CountAsync();
            }


            // -------------------------------------------------
            // Sales Officer
            // Count own completed leads
            // -------------------------------------------------

            return await _context.LeadAssignments
                .AsNoTracking()
                .Where(a =>
                    a.SalesOfficerId ==
                        userId &&
                    a.Lead != null &&
                    a.Lead.Status ==
                        LeadStatus.Completed &&
                    a.Lead.UpdatedAt >=
                        startDate &&
                    a.Lead.UpdatedAt <
                        endDate.Date.AddDays(1))
                .Select(a => a.LeadId)
                .Distinct()
                .CountAsync();
        }


        // =====================================================
        // Build Target Achievement
        // =====================================================

        private static TargetAchievementViewModel
            BuildTargetAchievement(
                SalesTarget target,
                int completedCount)
        {
            return new TargetAchievementViewModel
            {
                TargetId =
                    target.TargetId,

                UserId =
                    target.UserId,

                UserName =
                    target.User?.FullName ??
                    "Unknown",

                Role =
                    target.User?.Role?.RoleKey ??
                    "Unknown",

                PeriodType =
                    target.PeriodType.ToString(),

                TargetCount =
                    target.TargetCount,

                CompletedCount =
                    completedCount,

                RemainingCount =
                    Math.Max(
                        target.TargetCount -
                        completedCount,
                        0),

                AchievementPercentage =
                    CalculateAchievementPercentage(
                        completedCount,
                        target.TargetCount),

                StartDate =
                    target.StartDate,

                EndDate =
                    target.EndDate,

                Status =
                    GetTargetStatus(
                        completedCount,
                        target.TargetCount)
            };
        }


        // =====================================================
        // Achievement Percentage
        // =====================================================

        private static decimal
            CalculateAchievementPercentage(
                int completedCount,
                int targetCount)
        {
            if (targetCount <= 0)
            {
                return 0;
            }

            return Math.Round(
                completedCount * 100m /
                targetCount,
                2);
        }


        // =====================================================
        // Target Status
        // =====================================================

        private static string
            GetTargetStatus(
                int completedCount,
                int targetCount)
        {
            return completedCount >= targetCount
                ? "Target Achieved"
                : "Target Not Achieved";
        }


        // =====================================================
        // Admin Dashboard → Overall Sales Manager Target
        // =====================================================

        public async Task<(int TotalTarget, int TargetFulfilled)>
            GetAdminTargetSummaryAsync()
        {
            var salesManagerTargets =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Include(t => t.User)
                    .ThenInclude(u => u!.Role)
                    .Where(t =>
                        !t.IsDeleted &&
                        t.User != null &&
                        t.User.Role != null &&
                        t.User.Role.RoleKey == "SALES_MANAGER")
                    .Select(t => new
                    {
                        t.TargetCount,
                        t.UserId,
                        t.StartDate,
                        t.EndDate
                    })
                    .ToListAsync();


            if (!salesManagerTargets.Any())
            {
                return (0, 0);
            }


            // =====================================================
            // Total Target
            // Include ALL Sales Manager targets
            // regardless of current user active status
            // =====================================================

            var totalTarget =
                salesManagerTargets.Sum(t => t.TargetCount);


            // =====================================================
            // Total Target Fulfilled
            // =====================================================

            var totalFulfilled = 0;


            foreach (var target in salesManagerTargets)
            {
                var completedCount =
                    await GetCompletedLeadCountAsync(
                        target.UserId,
                        target.StartDate,
                        target.EndDate);

                totalFulfilled += completedCount;
            }


            // =====================================================
            // Return Overall Summary
            // =====================================================

            return (
                totalTarget,
                totalFulfilled
            );
        }
    }
}