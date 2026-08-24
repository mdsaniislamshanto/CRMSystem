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


        // =====================================================
        // Sales Manager Dashboard
        // =====================================================

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
                model.AutoAssignmentEnabled =
                    systemSettings.AutoAssignmentEnabled;
            }

            return model;
        }


        // =====================================================
        // Sales Manager Follow-ups
        // =====================================================

        public async Task<List<SalesManagerFollowUpViewModel>>
            GetFollowUpsAsync()
        {
            var now = DateTime.UtcNow;

            var feedbacks = await _context.Feedbacks

                // Load LeadAssignment
                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)

                // Load Sales Officer
                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.SalesOfficer)

                .Where(f =>
                    f.NextFollowUpDate.HasValue &&
                    f.LeadAssignment != null)

                .AsNoTracking()

                .OrderBy(f => f.NextFollowUpDate)

                .ToListAsync();


            // =================================================
            // Only Latest Follow-up Per Lead
            // =================================================

            var followUps = feedbacks
                .GroupBy(f => f.LeadAssignment!.LeadId)
                .Select(g =>
                {
                    var f = g
                        .OrderByDescending(x => x.NextFollowUpDate)
                        .First();

                    var followUpDate =
                        f.NextFollowUpDate!.Value;

                    var isOverdue =
                        followUpDate < now;

                    var isDueToday =
                        followUpDate.Date == now.Date;

                    string followUpStatus;

                    if (isOverdue)
                    {
                        followUpStatus = "Overdue";
                    }
                    else if (isDueToday)
                    {
                        followUpStatus = "Due Today";
                    }
                    else
                    {
                        followUpStatus = "Upcoming";
                    }


                    return new SalesManagerFollowUpViewModel
                    {
                        FeedbackId =
                            f.FeedbackId,

                        AssignmentId =
                            f.AssignmentId,

                        LeadId =
                            f.LeadAssignment!.LeadId,

                        LeadName =
                            f.LeadAssignment.Lead?.LeadName
                            ?? "Unknown Lead",

                        SalesOfficerName =
                            f.LeadAssignment.SalesOfficer?.FullName
                            ?? "Unknown Sales Officer",

                        Summary =
                            f.Summary,

                        FeedbackStatus =
                            f.Status.ToString(),

                        SubmittedAt =
                            f.SubmittedAt,

                        FollowUpDate =
                            followUpDate,

                        IsOverdue =
                            isOverdue,

                        IsDueToday =
                            isDueToday,

                        FollowUpStatus =
                            followUpStatus
                    };
                })

                .OrderBy(f => f.FollowUpDate)

                .ToList();


            return followUps;
        }


        // =====================================================
        // Sales Manager Follow-up Details
        // =====================================================

        public async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(long leadId)
        {
            var now = DateTime.UtcNow;


            // =================================================
            // Get all feedback history for the selected Lead
            // =================================================

            var feedbacks = await _context.Feedbacks

                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)

                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.SalesOfficer)

                .Where(f =>
                    f.LeadAssignment != null &&
                    f.LeadAssignment.LeadId == leadId)

                .AsNoTracking()

                .OrderByDescending(f => f.SubmittedAt)

                .ToListAsync();


            // =================================================
            // Lead has no feedback
            // =================================================

            if (!feedbacks.Any())
            {
                return null;
            }


            // =================================================
            // Get first available assignment
            // =================================================

            var firstFeedback =
                feedbacks.First();

            var assignment =
                firstFeedback.LeadAssignment;


            if (assignment == null)
            {
                return null;
            }


            // =================================================
            // Create Details ViewModel
            // =================================================

            var model =
                new SalesManagerFollowUpDetailsViewModel
                {
                    LeadId =
                        leadId,

                    LeadName =
                        assignment.Lead?.LeadName
                        ?? "Unknown Lead",

                    SalesOfficerName =
                        assignment.SalesOfficer?.FullName
                        ?? "Unknown Sales Officer"
                };


            // =================================================
            // Map Complete Feedback History
            // =================================================

            model.FeedbackHistory =
                feedbacks
                    .Select(feedback =>
                    {
                        var nextFollowUpDate =
                            feedback.NextFollowUpDate;

                        var isNextFollowUpOverdue =
                            nextFollowUpDate.HasValue &&
                            nextFollowUpDate.Value < now;


                        return new SalesManagerFeedbackHistoryViewModel
                        {
                            FeedbackId =
                                feedback.FeedbackId,

                            AssignmentId =
                                feedback.AssignmentId,

                            SubmittedAt =
                                feedback.SubmittedAt,

                            Summary =
                                feedback.Summary,

                            FeedbackStatus =
                                feedback.Status.ToString(),

                            NextFollowUpDate =
                                nextFollowUpDate,

                            ProofImage =
                                feedback.ProofImage,

                            VoiceRecording =
                                feedback.VoiceRecording,

                            Notes =
                                feedback.Notes,

                            IsNextFollowUpOverdue =
                                isNextFollowUpOverdue
                        };
                    })
                    .ToList();

            return model;
        }

        // =====================================================
        // Sales Officer Performance
        // =====================================================

        public async Task<List<SalesOfficerPerformanceViewModel>>
            GetPerformanceAsync()
        {
            var now = DateTime.UtcNow;

            var performance = await _context.Users

                // Only Sales Officers
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleKey == "SALES_OFFICER" &&
                    u.IsActive &&
                    !u.IsDeleted)

                .Select(u => new SalesOfficerPerformanceViewModel
                {
                    // -----------------------------------------
                    // Sales Officer Information
                    // -----------------------------------------

                    SalesOfficerId = u.UserId,

                    SalesOfficerName = u.FullName,


                    // -----------------------------------------
                    // Total Assigned Leads
                    // -----------------------------------------

                    TotalAssignedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted),


                    // -----------------------------------------
                    // Accepted Leads
                    // -----------------------------------------

                    AcceptedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.AcceptedAt.HasValue),


                    // -----------------------------------------
                    // Pending Acceptance
                    // -----------------------------------------

                    PendingAcceptance = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.AssignmentStatus == AssignmentStatus.Pending),


                    // -----------------------------------------
                    // Completed Leads
                    // -----------------------------------------

                    CompletedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.Lead != null &&
                            a.Lead.Status == LeadStatus.Completed),


                    // -----------------------------------------
                    // Total Feedbacks
                    // -----------------------------------------

                    TotalFeedbacks = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId == u.UserId &&
                            f.IsActive &&
                            !f.IsDeleted),


                    // -----------------------------------------
                    // Acceptance SLA Missed
                    // -----------------------------------------

                    AcceptanceSLAMissed = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.AcceptanceSLAMissed),


                    // -----------------------------------------
                    // First Feedback SLA Missed
                    // -----------------------------------------

                    FirstFeedbackSLAMissed = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.FirstFeedbackSLAMissed),


                    // -----------------------------------------
                    // Overdue Follow-ups
                    // -----------------------------------------

                    OverdueFollowUps = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId == u.UserId &&
                            f.IsActive &&
                            !f.IsDeleted &&
                            f.NextFollowUpDate.HasValue &&
                            f.NextFollowUpDate.Value < now)
                })

                .AsNoTracking()

                .ToListAsync();

            return performance;
        }

    }
}