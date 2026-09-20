using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class TeamLeadService : ITeamLeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITargetService _targetService;

        public TeamLeadService(
            ApplicationDbContext context,
            ITargetService targetService)
        {
            _context = context;
            _targetService = targetService;
        }


        // =========================================================
        // Team Lead Dashboard
        // =========================================================

        public async Task<TeamLeadDashboardViewModel?>
            GetDashboardAsync(
                long teamLeadId)
        {
            // ---------------------------------------------------------
            // Verify current Team Lead
            // ---------------------------------------------------------

            var teamLead =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            teamLeadId &&

                            u.IsActive &&

                            !u.IsDeleted &&

                            u.Role != null &&

                            u.Role.RoleKey ==
                            RoleKeys.TeamLead);

            if (teamLead == null)
            {
                return null;
            }


            // ---------------------------------------------------------
            // Get Team Members
            // ---------------------------------------------------------

            var teamMemberIds =
                await _context.Users
                    .Where(u =>
                        u.TeamLeadId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .Select(u =>
                        u.UserId)
                    .ToListAsync();


            var teamMemberCount =
                teamMemberIds.Count;


            // ---------------------------------------------------------
            // Get Current Lead Statuses
            // ---------------------------------------------------------

            var leadStatusRows =
                await _context.LeadAssignments
                    .Where(a =>
                        teamMemberIds.Contains(
                            a.SalesOfficerId) &&

                        a.IsActive &&

                        !a.IsDeleted &&

                        a.Lead != null &&

                        !a.Lead.IsArchived &&

                        !a.Lead.IsDeleted)
                    .Select(a =>
                        new
                        {
                            LeadId =
                                a.LeadId,

                            Status =
                                a.Lead!.Status,

                            AssignedAt =
                                a.AssignedAt
                        })
                    .ToListAsync();


            // ---------------------------------------------------------
            // Get latest assignment/status for each lead
            // ---------------------------------------------------------

            var latestLeadStatuses =
                leadStatusRows
                    .GroupBy(x =>
                        x.LeadId)
                    .Select(g =>
                        g.OrderByDescending(x =>
                            x.AssignedAt)
                         .First())
                    .ToList();


            // ---------------------------------------------------------
            // Lead Status Counts
            // ---------------------------------------------------------

            var newLeadCount =
                latestLeadStatuses.Count(x =>
                    x.Status ==
                    LeadStatus.New);


            var assignedLeadCount =
                latestLeadStatuses.Count(x =>
                    x.Status ==
                    LeadStatus.Assigned);


            var acceptedLeadCount =
                latestLeadStatuses.Count(x =>
                    x.Status ==
                    LeadStatus.Accepted);


            var completedLeadCount =
                latestLeadStatuses.Count(x =>
                    x.Status ==
                    LeadStatus.Completed);


            // ---------------------------------------------------------
            // Active Leads
            //
            // Completed leads are excluded.
            // ---------------------------------------------------------

            var activeLeadCount =
                latestLeadStatuses.Count(x =>
                    x.Status !=
                    LeadStatus.Completed);


            // ---------------------------------------------------------
            // Pending Follow-ups
            // ---------------------------------------------------------

            var pendingFollowUpCount =
                await _context.Feedbacks
                    .Where(f =>
                        f.LeadAssignment != null &&

                        f.LeadAssignment.IsActive &&

                        !f.LeadAssignment.IsDeleted &&

                        teamMemberIds.Contains(
                            f.LeadAssignment
                                .SalesOfficerId) &&

                        f.NextFollowUpDate.HasValue &&

                        f.NextFollowUpDate.Value >=
                            DateTime.UtcNow.Date)
                    .Select(f =>
                        f.LeadAssignment!.LeadId)
                    .Distinct()
                    .CountAsync();


            // =========================================================
            // Current Team Lead Target
            // =========================================================

            var today =
                DateTime.UtcNow.Date;


            var currentTarget =
                await _context.SalesTargets
                    .AsNoTracking()
                    .Where(t =>
                        t.UserId ==
                        teamLeadId &&

                        !t.IsDeleted &&

                        t.StartDate <=
                        today &&

                        t.EndDate >=
                        today)
                    .OrderByDescending(t =>
                        t.StartDate)
                    .FirstOrDefaultAsync();


            var totalTarget =
                0;

            var targetFulfilled =
                0;


            // ---------------------------------------------------------
            // Calculate target fulfillment
            // using the existing TargetService business rule
            // ---------------------------------------------------------

            if (currentTarget != null)
            {
                totalTarget =
                    currentTarget.TargetCount;
                

                targetFulfilled =
      await _targetService.GetCompletedLeadCountAsync(
          teamLeadId,
          currentTarget.StartDate,
          currentTarget.EndDate);
            }


            // =========================================================
            // Return Dashboard ViewModel
            // =========================================================

            return new TeamLeadDashboardViewModel
            {
                TeamLeadName =
                    teamLead.FullName,

                TeamMemberCount =
                    teamMemberCount,

                ActiveLeadCount =
                    activeLeadCount,

                CompletedLeadCount =
                    completedLeadCount,

                PendingFollowUpCount =
                    pendingFollowUpCount,

                NewLeadCount =
                    newLeadCount,

                AssignedLeadCount =
                    assignedLeadCount,

                AcceptedLeadCount =
                    acceptedLeadCount,

                TotalTarget =
                    totalTarget,

                TargetFulfilled =
                    targetFulfilled
            };
        }


        // =========================================================
        // Team Lead - My Team
        // =========================================================

        public async Task<TeamLeadMyTeamViewModel?>
            GetMyTeamAsync(
                long teamLeadId)
        {
            var teamLead =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            teamLeadId &&

                            u.IsActive &&

                            !u.IsDeleted &&

                            u.Role != null &&

                            u.Role.RoleKey ==
                            RoleKeys.TeamLead);

            if (teamLead == null)
            {
                return null;
            }


            var teamMembers =
                await _context.Users
                    .Where(u =>
                        u.TeamLeadId ==
                        teamLeadId &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .OrderBy(u =>
                        u.FirstName)
                    .ThenBy(u =>
                        u.LastName)
                    .Select(u =>
                        new TeamMemberViewModel
                        {
                            UserId =
                                u.UserId,

                            EmployeeCode =
                                u.EmployeeCode,

                            FullName =
                                u.FullName,

                            Email =
                                u.Email,

                            PhoneNumber =
                                u.PhoneNumber,

                            IsActive =
                                u.IsActive
                        })
                    .ToListAsync();


            foreach (var member in teamMembers)
            {
                member.TotalLeads =
                    await _context.LeadAssignments
                        .Where(a =>
                            a.SalesOfficerId ==
                            member.UserId &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted)
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .CountAsync();


                member.ActiveLeads =
                    await _context.LeadAssignments
                        .Where(a =>
                            a.SalesOfficerId ==
                            member.UserId &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted &&

                            !a.Lead.IsArchived)
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .CountAsync();


                member.CompletedLeads =
                    await _context.LeadAssignments
                        .Where(a =>
                            a.SalesOfficerId ==
                            member.UserId &&

                            a.IsActive &&

                            !a.IsDeleted &&

                            a.Lead != null &&

                            !a.Lead.IsDeleted &&

                            a.Lead.Status ==
                                LeadStatus.Completed)
                        .Select(a =>
                            a.LeadId)
                        .Distinct()
                        .CountAsync();


                member.PendingFollowUps =
                    await _context.Feedbacks
                        .Where(f =>
                            f.LeadAssignment != null &&

                            f.LeadAssignment
                                .SalesOfficerId ==
                            member.UserId &&

                            f.NextFollowUpDate.HasValue &&

                            f.NextFollowUpDate.Value >=
                            DateTime.UtcNow.Date)
                        .Select(f =>
                            f.LeadAssignment!.LeadId)
                        .Distinct()
                        .CountAsync();
            }


            return new TeamLeadMyTeamViewModel
            {
                TeamLeadName =
                    teamLead.FullName,

                TeamMemberCount =
                    teamMembers.Count,

                TeamMembers =
                    teamMembers
            };
        }


        // =========================================================
        // Team Lead - Officer Details
        // =========================================================

        public async Task<UserDetailsViewModel?>
            GetOfficerDetailsAsync(
                long officerId,
                long teamLeadId)
        {
            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return null;
            }


            var officer =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.TeamLead)
                        .ThenInclude(t =>
                            t!.SalesManager)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            officerId &&

                            u.IsActive &&

                            !u.IsDeleted &&

                            u.Role != null &&

                            u.Role.RoleKey ==
                            RoleKeys.SalesOfficer &&

                            u.TeamLeadId ==
                            teamLeadId);

            if (officer == null)
            {
                return null;
            }


            return new UserDetailsViewModel
            {
                UserId =
                    officer.UserId,

                EmployeeCode =
                    officer.EmployeeCode,

                FullName =
                    officer.FullName,

                Email =
                    officer.Email,

                PhoneNumber =
                    officer.PhoneNumber,

                RoleName =
                    officer.Role!.RoleName,

                IsEmailVerified =
                    officer.IsEmailVerified,

                IsActive =
                    officer.IsActive,

                LastLoginAt =
                    officer.LastLoginAt,

                LastPasswordChangedAt =
                    officer.LastPasswordChangedAt,

                TeamLeadName =
                    officer.TeamLead?.FullName,

                SalesManagerName =
                    officer.TeamLead?
                        .SalesManager?
                        .FullName
            };
        }


        // =========================================================
        // Team Lead - Lead Details
        // =========================================================

        public async Task<LeadViewModel?>
            GetLeadDetailsAsync(
                long leadId,
                long teamLeadId)
        {
            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return null;
            }


            var lead =
                await _context.LeadAssignments
                    .Where(a =>
                        a.LeadId ==
                        leadId &&

                        a.IsActive &&

                        !a.IsDeleted &&

                        a.Lead != null &&

                        !a.Lead.IsDeleted &&

                        !a.Lead.IsArchived &&

                        a.SalesOfficer != null &&

                        a.SalesOfficer.IsActive &&

                        !a.SalesOfficer.IsDeleted &&

                        a.SalesOfficer.TeamLeadId ==
                        teamLeadId &&

                        a.SalesOfficer.Role != null &&

                        a.SalesOfficer.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .OrderByDescending(a =>
                        a.AssignedAt)
                    .Select(a =>
                        new LeadViewModel
                        {
                            LeadId =
                                a.Lead!.LeadId,

                            LeadCode =
                                a.Lead.LeadCode,

                            CompanyName =
                                a.Lead.CompanyName,

                            LeadName =
                                a.Lead.LeadName,

                            Profession =
                                a.Lead.Profession,

                            Email =
                                a.Lead.Email,

                            Phone =
                                a.Lead.Phone,

                            Address =
                                a.Lead.Address,

                            Source =
                                a.Lead.Source,

                            Priority =
                                a.Lead.Priority,

                            Status =
                                a.Lead.Status,

                            Description =
                                a.Lead.Description,

                            FollowUpDate =
                                a.Lead.FollowUpDate,

                            AssignedOfficerName =
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null,

                            AssignedAt =
                                a.AssignedAt,

                            AcceptedAt =
                                a.AcceptedAt,

                            AssignmentStatus =
                                a.AssignmentStatus,

                            AcceptanceSLAMissed =
                                a.AcceptanceSLAMissed
                        })
                    .FirstOrDefaultAsync();


            if (lead == null)
            {
                return null;
            }


            CalculateAcceptanceSLA(lead);

            return lead;
        }


        // =========================================================
        // Team Lead - Feedback History
        // =========================================================

        public async Task<List<FeedbackHistoryViewModel>>
            GetFeedbackHistoryAsync(
                long leadId,
                long teamLeadId)
        {
            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return new List<FeedbackHistoryViewModel>();
            }


            var feedbacks =
                await _context.Feedbacks
                    .Where(f =>
                        f.LeadAssignment != null &&

                        f.LeadAssignment.LeadId ==
                        leadId &&

                        f.LeadAssignment.IsActive &&

                        !f.LeadAssignment.IsDeleted &&

                        f.LeadAssignment.Lead != null &&

                        !f.LeadAssignment.Lead.IsDeleted &&

                        !f.LeadAssignment.Lead.IsArchived &&

                        f.LeadAssignment.SalesOfficer != null &&

                        f.LeadAssignment.SalesOfficer.IsActive &&

                        !f.LeadAssignment.SalesOfficer.IsDeleted &&

                        f.LeadAssignment.SalesOfficer.TeamLeadId ==
                        teamLeadId &&

                        f.LeadAssignment.SalesOfficer.Role != null &&

                        f.LeadAssignment.SalesOfficer.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .Select(f =>
                        new
                        {
                            FeedbackId =
                                f.FeedbackId,

                            AssignmentId =
                                f.AssignmentId,

                            LeadId =
                                f.LeadAssignment!.LeadId,

                            CompanyName =
                                f.LeadAssignment.Lead!.CompanyName,

                            LeadName =
                                f.LeadAssignment.Lead.LeadName,

                            Status =
                                f.Status,

                            SubmittedAt =
                                f.SubmittedAt,

                            NextFollowUpDate =
                                f.NextFollowUpDate,

                            Summary =
                                f.Summary
                        })
                    .OrderByDescending(f =>
                        f.SubmittedAt)
                    .ToListAsync();


            if (feedbacks.Count == 0)
            {
                return new List<FeedbackHistoryViewModel>();
            }


            var history =
                new List<FeedbackHistoryViewModel>();


            var chronologicalFeedbacks =
                feedbacks
                    .OrderBy(f =>
                        f.SubmittedAt)
                    .ToList();


            for (
                int i = 0;
                i < chronologicalFeedbacks.Count;
                i++)
            {
                var feedback =
                    chronologicalFeedbacks[i];


                string timelinessStatus =
                    "Pending";


                if (i == 0)
                {
                    timelinessStatus =
                        "First Feedback";
                }
                else
                {
                    var previousFeedback =
                        chronologicalFeedbacks[i - 1];


                    if (previousFeedback
                        .NextFollowUpDate
                        .HasValue)
                    {
                        timelinessStatus =
                            feedback.SubmittedAt <=
                            previousFeedback
                                .NextFollowUpDate
                                .Value
                                ? "On Time"
                                : "Late";
                    }
                }


                history.Add(
                    new FeedbackHistoryViewModel
                    {
                        FeedbackId =
                            feedback.FeedbackId,

                        AssignmentId =
                            feedback.AssignmentId,

                        LeadId =
                            feedback.LeadId,

                        CompanyName =
                            feedback.CompanyName
                            ?? string.Empty,

                        LeadName =
                            feedback.LeadName,

                        Status =
                            feedback.Status,

                        SubmittedAt =
                            feedback.SubmittedAt,

                        NextFollowUpDate =
                            feedback.NextFollowUpDate,

                        Summary =
                            feedback.Summary
                    });
            }


            return history
                .OrderByDescending(x =>
                    x.SubmittedAt)
                .ToList();
        }


        // =========================================================
        // Team Lead - Feedback Details
        // =========================================================

        public async Task<FeedbackDetailsViewModel?>
            GetFeedbackDetailsAsync(
                long feedbackId,
                long teamLeadId)
        {
            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return null;
            }


            var feedback =
                await _context.Feedbacks
                    .Where(f =>
                        f.FeedbackId ==
                        feedbackId &&

                        f.IsActive &&

                        !f.IsDeleted &&

                        f.LeadAssignment != null &&

                        f.LeadAssignment.IsActive &&

                        !f.LeadAssignment.IsDeleted &&

                        f.LeadAssignment.Lead != null &&

                        !f.LeadAssignment.Lead.IsDeleted &&

                        !f.LeadAssignment.Lead.IsArchived &&

                        f.LeadAssignment.SalesOfficer != null &&

                        f.LeadAssignment.SalesOfficer.IsActive &&

                        !f.LeadAssignment.SalesOfficer.IsDeleted &&

                        f.LeadAssignment.SalesOfficer.TeamLeadId ==
                        teamLeadId &&

                        f.LeadAssignment.SalesOfficer.Role != null &&

                        f.LeadAssignment.SalesOfficer.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .Select(f =>
                        new FeedbackDetailsViewModel
                        {
                            FeedbackId =
                                f.FeedbackId,

                            AssignmentId =
                                f.AssignmentId,

                            LeadId =
                                f.LeadAssignment!.LeadId,

                            CompanyName =
                                f.LeadAssignment.Lead!.CompanyName,

                            LeadName =
                                f.LeadAssignment.Lead.LeadName,

                            Email =
                                f.LeadAssignment.Lead.Email,

                            Phone =
                                f.LeadAssignment.Lead.Phone,

                            Summary =
                                f.Summary,

                            Status =
                                f.Status,

                            SubmittedAt =
                                f.SubmittedAt,

                            NextFollowUpDate =
                                f.NextFollowUpDate,

                            ProofImage =
                                f.ProofImage,

                            VoiceRecording =
                                f.VoiceRecording,

                            Notes =
                                f.Notes
                        })
                    .FirstOrDefaultAsync();


            return feedback;
        }


        // =========================================================
        // Team Lead - Leads
        // =========================================================

        public async Task<List<LeadViewModel>>
            GetTeamLeadsAsync(
                long teamLeadId,
                string? search = null,
                LeadStatus? status = null,
                LeadPriority? priority = null,
                LeadSource? source = null,
                string? slaStatus = null,
                string? sort = null)
        {
            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return new List<LeadViewModel>();
            }


            var query =
                _context.LeadAssignments
                    .Where(a =>
                        a.IsActive &&
                        !a.IsDeleted &&

                        a.Lead != null &&
                        !a.Lead.IsArchived &&
                        !a.Lead.IsDeleted &&

                        a.SalesOfficer != null &&
                        a.SalesOfficer.IsActive &&
                        !a.SalesOfficer.IsDeleted &&

                        a.SalesOfficer.TeamLeadId ==
                        teamLeadId &&

                        a.SalesOfficer.Role != null &&

                        a.SalesOfficer.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                search =
                    search.Trim();

                query =
                    query.Where(a =>
                        a.Lead!.LeadCode
                            .Contains(search) ||

                        a.Lead.LeadName
                            .Contains(search) ||

                        (a.Lead.CompanyName != null &&
                         a.Lead.CompanyName
                            .Contains(search)) ||

                        a.Lead.Phone
                            .Contains(search));
            }


            if (status.HasValue)
            {
                query =
                    query.Where(a =>
                        a.Lead!.Status ==
                        status.Value);
            }


            if (priority.HasValue)
            {
                query =
                    query.Where(a =>
                        a.Lead!.Priority ==
                        priority.Value);
            }


            if (source.HasValue)
            {
                query =
                    query.Where(a =>
                        a.Lead!.Source ==
                        source.Value);
            }


            var leads =
                await query
                    .Select(a =>
                        new LeadViewModel
                        {
                            LeadId =
                                a.Lead!.LeadId,

                            LeadCode =
                                a.Lead.LeadCode,

                            CompanyName =
                                a.Lead.CompanyName,

                            LeadName =
                                a.Lead.LeadName,

                            Profession =
                                a.Lead.Profession,

                            Email =
                                a.Lead.Email,

                            Phone =
                                a.Lead.Phone,

                            Address =
                                a.Lead.Address,

                            Source =
                                a.Lead.Source,

                            Priority =
                                a.Lead.Priority,

                            Status =
                                a.Lead.Status,

                            Description =
                                a.Lead.Description,

                            FollowUpDate =
                                a.Lead.FollowUpDate,

                            AssignedOfficerName =
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null,

                            AssignedAt =
                                a.AssignedAt,

                            AcceptedAt =
                                a.AcceptedAt,

                            AssignmentStatus =
                                a.AssignmentStatus,

                            AcceptanceSLAMissed =
                                a.AcceptanceSLAMissed
                        })
                    .ToListAsync();


            leads =
                leads
                    .GroupBy(x =>
                        x.LeadId)
                    .Select(g =>
                        g.OrderByDescending(x =>
                            x.AssignedAt)
                         .First())
                    .ToList();


            foreach (var lead in leads)
            {
                CalculateAcceptanceSLA(lead);
            }


            if (!string.IsNullOrWhiteSpace(
                    slaStatus))
            {
                switch (
                    slaStatus
                        .Trim()
                        .ToLowerInvariant())
                {
                    case "notassigned":

                        leads =
                            leads
                                .Where(x =>
                                    x.AcceptanceSLAStatus ==
                                    "Not Assigned")
                                .ToList();

                        break;


                    case "pending":

                        leads =
                            leads
                                .Where(x =>
                                    x.AcceptanceSLAStatus
                                        .StartsWith(
                                            "Pending",
                                            StringComparison
                                                .OrdinalIgnoreCase))
                                .ToList();

                        break;


                    case "withinsla":

                        leads =
                            leads
                                .Where(x =>
                                    x.AcceptanceSLAStatus ==
                                    "Within SLA")
                                .ToList();

                        break;


                    case "breached":

                        leads =
                            leads
                                .Where(x =>
                                    x.AcceptanceSLAMissed ||
                                    x.AcceptanceSLAStatus ==
                                    "SLA Breached")
                                .ToList();

                        break;
                }
            }


            leads =
                sort switch
                {
                    "oldest" =>
                        leads
                            .OrderBy(x =>
                                x.LeadId)
                            .ToList(),

                    "name" =>
                        leads
                            .OrderBy(x =>
                                x.LeadName)
                            .ToList(),

                    "name_desc" =>
                        leads
                            .OrderByDescending(x =>
                                x.LeadName)
                            .ToList(),

                    "priority" =>
                        leads
                            .OrderByDescending(x =>
                                x.Priority)
                            .ThenByDescending(x =>
                                x.LeadId)
                            .ToList(),

                    "priority_low" =>
                        leads
                            .OrderBy(x =>
                                x.Priority)
                            .ThenByDescending(x =>
                                x.LeadId)
                            .ToList(),

                    "status" =>
                        leads
                            .OrderBy(x =>
                                x.Status)
                            .ThenByDescending(x =>
                                x.LeadId)
                            .ToList(),

                    "sla" =>
                        leads
                            .OrderByDescending(x =>
                                x.AcceptanceSLAMissed)
                            .ThenBy(x =>
                                x.AcceptanceSLAStatus)
                            .ThenByDescending(x =>
                                x.LeadId)
                            .ToList(),

                    _ =>
                        leads
                            .OrderByDescending(x =>
                                x.LeadId)
                            .ToList()
                };


            return leads;
        }


        // =========================================================
        // Team Lead - Follow-up Monitoring
        // =========================================================

        public async Task<FollowUpFilterViewModel>
            GetTeamFollowUpsAsync(
                long teamLeadId,
                FollowUpFilterViewModel filter)
        {
            // -----------------------------------------------------
            // Verify current Team Lead
            // -----------------------------------------------------

            var isValidTeamLead =
                await _context.Users
                    .AnyAsync(u =>
                        u.UserId ==
                        teamLeadId &&

                        u.IsActive &&

                        !u.IsDeleted &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead);

            if (!isValidTeamLead)
            {
                return new FollowUpFilterViewModel
                {
                    Page = 1,
                    PageSize = 10
                };
            }


            // -----------------------------------------------------
            // Normalize pagination
            // -----------------------------------------------------

            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 10;
            }


            // -----------------------------------------------------
            // Base Query
            //
            // Feedback
            //   ↓
            // LeadAssignment
            //   ↓
            // SalesOfficer
            //   ↓
            // TeamLead
            // -----------------------------------------------------

            var query =
                _context.Feedbacks
                    .Where(f =>
                        f.IsActive &&

                        !f.IsDeleted &&

                        f.NextFollowUpDate.HasValue &&

                        f.LeadAssignment != null &&

                        f.LeadAssignment.IsActive &&

                        !f.LeadAssignment.IsDeleted &&

                        f.LeadAssignment.Lead != null &&

                        !f.LeadAssignment.Lead.IsDeleted &&

                        !f.LeadAssignment.Lead.IsArchived &&

                        f.LeadAssignment.SalesOfficer != null &&

                        f.LeadAssignment.SalesOfficer.IsActive &&

                        !f.LeadAssignment.SalesOfficer.IsDeleted &&

                        f.LeadAssignment.SalesOfficer.TeamLeadId ==
                        teamLeadId &&

                        f.LeadAssignment.SalesOfficer.Role != null &&

                        f.LeadAssignment.SalesOfficer.Role.RoleKey ==
                        RoleKeys.SalesOfficer)
                    .AsQueryable();


            // -----------------------------------------------------
            // Search
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    filter.Search))
            {
                var search =
                    filter.Search.Trim();

                query =
                    query.Where(f =>
                        f.LeadAssignment!.Lead!.LeadName
                            .Contains(search) ||

                        f.LeadAssignment.Lead.LeadCode
                            .Contains(search) ||

                        (f.LeadAssignment.Lead.CompanyName != null &&
                         f.LeadAssignment.Lead.CompanyName
                            .Contains(search)) ||

                        f.LeadAssignment.Lead.Phone
                            .Contains(search) ||

                        f.LeadAssignment.SalesOfficer!.FirstName
                            .Contains(search) ||

                        (f.LeadAssignment.SalesOfficer.LastName != null &&
                         f.LeadAssignment.SalesOfficer.LastName
                            .Contains(search)));
            }


            // -----------------------------------------------------
            // Feedback Status Filter
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    filter.FeedbackStatus))
            {
                if (Enum.TryParse<FeedbackStatus>(
                    filter.FeedbackStatus,
                    true,
                    out var feedbackStatus))
                {
                    query =
                        query.Where(f =>
                            f.Status ==
                            feedbackStatus);
                }
            }


            // -----------------------------------------------------
            // Date Range Filter
            // -----------------------------------------------------

            if (filter.FromDate.HasValue)
            {
                var fromDate =
                    filter.FromDate.Value.Date;

                query =
                    query.Where(f =>
                        f.NextFollowUpDate >=
                        fromDate);
            }


            if (filter.ToDate.HasValue)
            {
                var toDateExclusive =
                    filter.ToDate.Value.Date
                        .AddDays(1);

                query =
                    query.Where(f =>
                        f.NextFollowUpDate <
                        toDateExclusive);
            }


            // -----------------------------------------------------
            // Load Data
            // -----------------------------------------------------

            var followUps =
                await query
                    .Select(f =>
                        new FollowUpViewModel
                        {
                            FeedbackId =
                                f.FeedbackId,

                            AssignmentId =
                                f.AssignmentId,

                            LeadId =
                                f.LeadAssignment!.LeadId,

                            LeadName =
                                f.LeadAssignment.Lead!.LeadName,

                            SalesOfficerName =
                                f.LeadAssignment
                                    .SalesOfficer!
                                    .FullName,

                            Summary =
                                f.Summary,

                            FeedbackStatus =
                                f.Status.ToString(),

                            SubmittedAt =
                                f.SubmittedAt,

                            FollowUpDate =
                                f.NextFollowUpDate!.Value
                        })
                    .ToListAsync();


            // -----------------------------------------------------
            // Calculate Follow-up Status
            // -----------------------------------------------------

            var now =
                DateTime.UtcNow;

            var today =
                now.Date;


            foreach (var followUp in followUps)
            {
                var followUpDate =
                    followUp.FollowUpDate;

                if (followUpDate < now)
                {
                    followUp.IsOverdue =
                        true;

                    followUp.IsDueToday =
                        false;

                    followUp.FollowUpStatus =
                        "Overdue";
                }
                else if (followUpDate.Date ==
                         today)
                {
                    followUp.IsOverdue =
                        false;

                    followUp.IsDueToday =
                        true;

                    followUp.FollowUpStatus =
                        "Due Today";
                }
                else
                {
                    followUp.IsOverdue =
                        false;

                    followUp.IsDueToday =
                        false;

                    followUp.FollowUpStatus =
                        "Upcoming";
                }


                // -------------------------------------------------
                // Existing timeliness property
                //
                // We intentionally do not expose this as a
                // separate Team Lead filter in this module.
                // -------------------------------------------------

                followUp.TimelinessStatus =
                    "Pending";
            }


            // -----------------------------------------------------
            // Follow-up Status Filter
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    filter.FollowUpStatus))
            {
                var status =
                    filter.FollowUpStatus
                        .Trim()
                        .ToLowerInvariant();

                followUps =
                    status switch
                    {
                        "overdue" =>
                            followUps
                                .Where(x =>
                                    x.IsOverdue)
                                .ToList(),

                        "duetoday" =>
                            followUps
                                .Where(x =>
                                    x.IsDueToday)
                                .ToList(),

                        "upcoming" =>
                            followUps
                                .Where(x =>
                                    !x.IsOverdue &&
                                    !x.IsDueToday)
                                .ToList(),

                        _ =>
                            followUps
                    };
            }


            // -----------------------------------------------------
            // Summary Counts
            // -----------------------------------------------------

            filter.TotalFollowUps =
                followUps.Count;

            filter.DueTodayCount =
                followUps.Count(x =>
                    x.IsDueToday);

            filter.UpcomingCount =
                followUps.Count(x =>
                    !x.IsOverdue &&
                    !x.IsDueToday);

            filter.OverdueCount =
                followUps.Count(x =>
                    x.IsOverdue);


            // -----------------------------------------------------
            // Sorting
            // -----------------------------------------------------

            followUps =
                filter.Sort switch
                {
                    "oldest" =>
                        followUps
                            .OrderBy(x =>
                                x.FollowUpDate)
                            .ToList(),

                    "lead" =>
                        followUps
                            .OrderBy(x =>
                                x.LeadName)
                            .ToList(),

                    "lead_desc" =>
                        followUps
                            .OrderByDescending(x =>
                                x.LeadName)
                            .ToList(),

                    "officer" =>
                        followUps
                            .OrderBy(x =>
                                x.SalesOfficerName)
                            .ToList(),

                    "status" =>
                        followUps
                            .OrderBy(x =>
                                x.FollowUpStatus)
                            .ThenBy(x =>
                                x.FollowUpDate)
                            .ToList(),

                    _ =>
                        followUps
                            .OrderBy(x =>
                                x.FollowUpDate)
                            .ToList()
                };


            // -----------------------------------------------------
            // Total Items
            // -----------------------------------------------------

            filter.TotalItems =
                followUps.Count;


            // -----------------------------------------------------
            // Pagination
            // -----------------------------------------------------

            var totalPages =
                filter.TotalItems == 0
                    ? 1
                    : (int)Math.Ceiling(
                        filter.TotalItems /
                        (double)filter.PageSize);


            if (filter.Page > totalPages)
            {
                filter.Page =
                    totalPages;
            }


            filter.FollowUps =
                followUps
                    .Skip(
                        (filter.Page - 1) *
                        filter.PageSize)
                    .Take(
                        filter.PageSize)
                    .ToList();


            return filter;
        }


        // =========================================================
        // Acceptance SLA Calculator
        // =========================================================

        private void CalculateAcceptanceSLA(
            LeadViewModel lead)
        {
            if (!lead.AssignedAt.HasValue ||
                !lead.AssignmentStatus.HasValue)
            {
                lead.AcceptanceSLAStatus =
                    "Not Assigned";

                lead.AcceptanceSLAMissed =
                    false;

                return;
            }


            var deadline =
                lead.AssignedAt.Value
                    .AddHours(1);

            var now =
                DateTime.UtcNow;


            if (lead.AcceptedAt.HasValue)
            {
                if (lead.AcceptedAt.Value <=
                    deadline)
                {
                    lead.AcceptanceSLAStatus =
                        "Within SLA";

                    lead.AcceptanceSLAMissed =
                        false;
                }
                else
                {
                    lead.AcceptanceSLAStatus =
                        "SLA Breached";

                    lead.AcceptanceSLAMissed =
                        true;
                }

                return;
            }


            if (lead.AssignmentStatus.Value ==
                AssignmentStatus.Pending)
            {
                if (now > deadline)
                {
                    lead.AcceptanceSLAStatus =
                        "SLA Breached";

                    lead.AcceptanceSLAMissed =
                        true;
                }
                else
                {
                    var remaining =
                        deadline - now;

                    var minutes =
                        Math.Max(
                            0,
                            (int)Math.Ceiling(
                                remaining.TotalMinutes));

                    lead.AcceptanceSLAStatus =
                        $"Pending • {minutes} min left";

                    lead.AcceptanceSLAMissed =
                        false;
                }

                return;
            }


            lead.AcceptanceSLAStatus =
                lead.AssignmentStatus.Value
                    .ToString();
        }
    }
}