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

        public async Task<SalesManagerFollowUpFilterViewModel> GetFollowUpsAsync(
    SalesManagerFollowUpFilterViewModel filter)
        {
            filter ??= new SalesManagerFollowUpFilterViewModel();

            var now = DateTime.UtcNow;

            // =====================================================
            // Load Feedbacks
            // =====================================================

            var feedbacks = await _context.Feedbacks

                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.Lead)

                .Include(f => f.LeadAssignment)
                    .ThenInclude(a => a!.SalesOfficer)

                .Where(f =>
                    f.NextFollowUpDate.HasValue &&
                    f.LeadAssignment != null)

                .AsNoTracking()

                .ToListAsync();


            // =====================================================
            // Create Latest Follow-up Per Lead
            // =====================================================

            var followUps = feedbacks

                .GroupBy(f => f.LeadAssignment!.LeadId)

                .Select(group =>
                {
                    // =================================================
                    // Sort all feedbacks chronologically
                    // =================================================

                    var orderedFeedbacks = group
                        .OrderBy(f => f.SubmittedAt)
                        .ToList();


                    // =================================================
                    // Get latest feedback
                    // =================================================

                    var latestFeedback =
                        orderedFeedbacks.Last();


                    // =================================================
                    // Current follow-up date
                    // =================================================

                    var followUpDate =
                        latestFeedback.NextFollowUpDate!.Value;


                    // =================================================
                    // Follow-up Status
                    // =================================================

                    var isOverdue =
                        followUpDate < now;

                    var isDueToday =
                        followUpDate.Date == now.Date;

                    var followUpStatus =
                        isOverdue
                            ? "Overdue"
                            : isDueToday
                                ? "Due Today"
                                : "Upcoming";


                    // =================================================
                    // Timeliness
                    // =================================================

                    string timelinessStatus;


                    // =================================================
                    // Find previous feedback
                    // =================================================

                    var latestFeedbackIndex =
                        orderedFeedbacks.Count - 1;


                    var previousFeedback =
                        latestFeedbackIndex > 0
                            ? orderedFeedbacks[latestFeedbackIndex - 1]
                            : null;


                    // =================================================
                    // Case 1:
                    // This is NOT the first feedback
                    // =================================================

                    if (previousFeedback != null &&
                        previousFeedback.NextFollowUpDate.HasValue)
                    {
                        var expectedFeedbackDate =
                            previousFeedback.NextFollowUpDate.Value;


                        // ---------------------------------------------
                        // Latest feedback submitted on/before deadline
                        // ---------------------------------------------

                        if (latestFeedback.SubmittedAt <=
                            expectedFeedbackDate)
                        {
                            timelinessStatus = "On Time";
                        }

                        // ---------------------------------------------
                        // Latest feedback submitted after deadline
                        // ---------------------------------------------

                        else
                        {
                            timelinessStatus = "Late";
                        }
                    }

                    // =================================================
                    // Case 2:
                    // This is the first feedback
                    // =================================================

                    else
                    {
                        // First feedback does not have a previous
                        // follow-up deadline.
                        //
                        // First-feedback SLA is handled separately
                        // by the existing assignment SLA logic.

                        if (followUpDate < now)
                        {
                            timelinessStatus = "Overdue";
                        }
                        else
                        {
                            timelinessStatus = "Pending";
                        }
                    }


                    // =================================================
                    // Create ViewModel
                    // =================================================

                    return new SalesManagerFollowUpViewModel
                    {
                        FeedbackId =
                            latestFeedback.FeedbackId,

                        AssignmentId =
                            latestFeedback.AssignmentId,

                        LeadId =
                            latestFeedback.LeadAssignment!.LeadId,

                        LeadName =
                            latestFeedback.LeadAssignment.Lead?.LeadName
                            ?? "Unknown Lead",

                        SalesOfficerName =
                            latestFeedback.LeadAssignment.SalesOfficer?.FullName
                            ?? "Unknown Sales Officer",

                        Summary =
                            latestFeedback.Summary,

                        FeedbackStatus =
                            latestFeedback.Status.ToString(),

                        SubmittedAt =
                            latestFeedback.SubmittedAt,

                        FollowUpDate =
                            followUpDate,

                        IsOverdue =
                            isOverdue,

                        IsDueToday =
                            isDueToday,

                        FollowUpStatus =
                            followUpStatus,

                        TimelinessStatus =
                            timelinessStatus
                    };
                })

                .ToList();


            // =====================================================
            // Summary Counts
            // =====================================================

            filter.TotalFollowUps =
                followUps.Count;

            filter.OverdueCount =
                followUps.Count(f => f.IsOverdue);

            filter.DueTodayCount =
                followUps.Count(f => f.IsDueToday);

            filter.UpcomingCount =
                followUps.Count(f =>
                    !f.IsOverdue &&
                    !f.IsDueToday);


            // =====================================================
            // Date Range Filter
            // =====================================================

            if (filter.FromDate.HasValue)
            {
                var fromDate =
                    filter.FromDate.Value.Date;

                followUps =
                    followUps
                        .Where(f =>
                            f.FollowUpDate.Date >= fromDate)
                        .ToList();
            }


            if (filter.ToDate.HasValue)
            {
                var toDate =
                    filter.ToDate.Value.Date;

                followUps =
                    followUps
                        .Where(f =>
                            f.FollowUpDate.Date <= toDate)
                        .ToList();
            }


            // =====================================================
            // Search
            // =====================================================

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search =
                    filter.Search.Trim();

                followUps =
                    followUps
                        .Where(f =>
                            f.LeadName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            f.SalesOfficerName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // Follow-up Status Filter
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                filter.FollowUpStatus))
            {
                followUps =
                    followUps
                        .Where(f =>
                            string.Equals(
                                f.FollowUpStatus,
                                filter.FollowUpStatus,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // Feedback Status Filter
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                filter.FeedbackStatus))
            {
                followUps =
                    followUps
                        .Where(f =>
                            string.Equals(
                                f.FeedbackStatus,
                                filter.FeedbackStatus,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // Sorting
            // =====================================================

            filter.Sort ??= "nearest";

            followUps =
                filter.Sort.ToLowerInvariant() switch
                {
                    "latest" =>
                        followUps
                            .OrderByDescending(
                                f => f.SubmittedAt)
                            .ToList(),

                    "oldest" =>
                        followUps
                            .OrderBy(
                                f => f.SubmittedAt)
                            .ToList(),

                    "overdue" =>
                        followUps
                            .OrderByDescending(
                                f => f.IsOverdue)
                            .ThenBy(
                                f => f.FollowUpDate)
                            .ToList(),

                    "lead" =>
                        followUps
                            .OrderBy(
                                f => f.LeadName)
                            .ToList(),

                    _ =>
                        followUps
                            .OrderBy(
                                f => f.FollowUpDate)
                            .ToList()
                };


            // =====================================================
            // Pagination Count
            // =====================================================

            filter.TotalItems =
                followUps.Count;


            // =====================================================
            // Validate Page
            // =====================================================

            if (filter.Page < 1)
            {
                filter.Page = 1;
            }

            if (filter.Page > filter.TotalPages)
            {
                filter.Page = filter.TotalPages;
            }


            // =====================================================
            // Pagination
            // =====================================================

            filter.FollowUps =
                followUps
                    .Skip(
                        (filter.Page - 1)
                        * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();


            return filter;
        }

        


        // =====================================================
        // Sales Manager Follow-up Details
        // =====================================================

        public async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(long leadId)
        {
            var now = DateTime.UtcNow;

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


            if (!feedbacks.Any())
            {
                return null;
            }


            var firstFeedback =
                feedbacks.First();

            var assignment =
                firstFeedback.LeadAssignment;


            if (assignment == null)
            {
                return null;
            }


            var model =
                new SalesManagerFollowUpDetailsViewModel
                {
                    LeadId = leadId,

                    LeadName =
                        assignment.Lead?.LeadName
                        ?? "Unknown Lead",

                    SalesOfficerName =
                        assignment.SalesOfficer?.FullName
                        ?? "Unknown Sales Officer"
                };


            // =====================================================
            // Prepare Feedback History For Timing Calculation
            // =====================================================

            var chronologicalFeedbacks =
                feedbacks
                    .OrderBy(f => f.SubmittedAt)
                    .ToList();


            // =====================================================
            // Create Feedback History
            // =====================================================

            model.FeedbackHistory =
                chronologicalFeedbacks
                    .Select((feedback, index) =>
                    {
                        var nextFollowUpDate =
                            feedback.NextFollowUpDate;


                        var isNextFollowUpOverdue =
                            nextFollowUpDate.HasValue &&
                            nextFollowUpDate.Value < now;


                        // ==========================================
                        // Determine Last Feedback Timing
                        // ==========================================

                        string lastFeedbackTiming;


                        // First feedback
                        if (index == 0)
                        {
                            var firstFeedbackDeadline =
                                assignment.AssignedAt.AddHours(3);


                            lastFeedbackTiming =
                                feedback.SubmittedAt <= firstFeedbackDeadline
                                    ? "On Time"
                                    : "Late";
                        }

                        // Subsequent feedback
                        else
                        {
                            var previousFeedback =
                                chronologicalFeedbacks[index - 1];


                            if (previousFeedback.NextFollowUpDate.HasValue)
                            {
                                lastFeedbackTiming =
                                    feedback.SubmittedAt <=
                                    previousFeedback.NextFollowUpDate.Value
                                        ? "On Time"
                                        : "Late";
                            }
                            else
                            {
                                lastFeedbackTiming = "N/A";
                            }
                        }


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
                                isNextFollowUpOverdue,

                            LastFeedbackTiming =
                                lastFeedbackTiming
                        };
                    })
                    .OrderByDescending(f => f.SubmittedAt)
                    .ToList();

            return model;
        }



    }
}