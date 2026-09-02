using CRMSystem.Data;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;

        public FollowUpService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesManagerFollowUpFilterViewModel> GetFollowUpsAsync(
            SalesManagerFollowUpFilterViewModel filter)
        {
            var now = DateTime.UtcNow;

            var feedbacks = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                    .ThenInclude(la => la.Lead)
                .Include(f => f.LeadAssignment)
                    .ThenInclude(la => la.SalesOfficer)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            var latestFollowUps = feedbacks
                .Where(f =>
                    f.LeadAssignment != null &&
                    f.NextFollowUpDate.HasValue)
                .GroupBy(f => f.LeadAssignment!.LeadId)
                .Select(g => g
                    .OrderByDescending(f => f.NextFollowUpDate)
                    .First())
                .ToList();

            var followUps = new List<SalesManagerFollowUpViewModel>();

            foreach (var feedback in latestFollowUps)
            {
                if (feedback.LeadAssignment?.Lead == null)
                    continue;

                var followUpDate = feedback.NextFollowUpDate!.Value;

                bool isOverdue = followUpDate < now;
                bool isDueToday = followUpDate.Date == now.Date;

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

                string timelinessStatus = "Pending";

                var previousFeedback = feedbacks
                    .Where(f =>
                        f.LeadAssignment != null &&
                        f.LeadAssignment.LeadId ==
                            feedback.LeadAssignment.LeadId &&
                        f.SubmittedAt < feedback.SubmittedAt)
                    .OrderByDescending(f => f.SubmittedAt)
                    .FirstOrDefault();

                if (previousFeedback != null &&
                    previousFeedback.NextFollowUpDate.HasValue)
                {
                    timelinessStatus =
                        feedback.SubmittedAt <=
                        previousFeedback.NextFollowUpDate.Value
                            ? "On Time"
                            : "Late";
                }

                followUps.Add(new SalesManagerFollowUpViewModel
                {
                    FeedbackId = feedback.FeedbackId,
                    AssignmentId = feedback.AssignmentId,
                    LeadId = feedback.LeadAssignment.LeadId,

                    LeadName = feedback.LeadAssignment.Lead.LeadName,

                    SalesOfficerName =
                        feedback.LeadAssignment.SalesOfficer != null
                            ? feedback.LeadAssignment.SalesOfficer.FullName
                            : "Unassigned",

                    Summary = feedback.Summary,

                    FeedbackStatus = feedback.Status.ToString(),

                    SubmittedAt = feedback.SubmittedAt,
                    FollowUpDate = followUpDate,

                    IsOverdue = isOverdue,
                    IsDueToday = isDueToday,

                    FollowUpStatus = followUpStatus,
                    TimelinessStatus = timelinessStatus
                });
            }

            // Search
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();

                followUps = followUps
                    .Where(x =>
                        x.LeadName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        x.SalesOfficerName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        x.Summary.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Follow-up Status Filter
            if (!string.IsNullOrWhiteSpace(filter.FollowUpStatus))
            {
                followUps = followUps
                    .Where(x =>
                        x.FollowUpStatus.Equals(
                            filter.FollowUpStatus,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Feedback Status Filter
            if (!string.IsNullOrWhiteSpace(filter.FeedbackStatus))
            {
                followUps = followUps
                    .Where(x =>
                        x.FeedbackStatus.Equals(
                            filter.FeedbackStatus,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // From Date
            if (filter.FromDate.HasValue)
            {
                var fromDate = filter.FromDate.Value.Date;

                followUps = followUps
                    .Where(x =>
                        x.FollowUpDate.Date >= fromDate)
                    .ToList();
            }

            // To Date
            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date;

                followUps = followUps
                    .Where(x =>
                        x.FollowUpDate.Date <= toDate)
                    .ToList();
            }

            // Summary Counts
            filter.TotalFollowUps = followUps.Count;

            filter.DueTodayCount = followUps.Count(x =>
                x.IsDueToday);

            filter.UpcomingCount = followUps.Count(x =>
                x.FollowUpStatus == "Upcoming");

            filter.OverdueCount = followUps.Count(x =>
                x.IsOverdue);

            // Sorting
            followUps = filter.Sort?.ToLower() switch
            {
                "latest" =>
                    followUps
                        .OrderByDescending(x => x.FollowUpDate)
                        .ToList(),

                "oldest" =>
                    followUps
                        .OrderBy(x => x.FollowUpDate)
                        .ToList(),

                "overdue" =>
                    followUps
                        .OrderByDescending(x => x.IsOverdue)
                        .ThenBy(x => x.FollowUpDate)
                        .ToList(),

                "leadaz" =>
                    followUps
                        .OrderBy(x => x.LeadName)
                        .ToList(),

                _ =>
                    followUps
                        .OrderBy(x => x.FollowUpDate)
                        .ToList()
            };

            // Pagination
            filter.TotalItems = followUps.Count;

            if (filter.Page < 1)
                filter.Page = 1;

            if (filter.PageSize <= 0)
                filter.PageSize = 10;

            filter.FollowUps = followUps
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return filter;
        }

        public async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(long leadId)
        {
            var feedbacks = await _context.Feedbacks
                .Include(f => f.LeadAssignment)
                    .ThenInclude(la => la.Lead)
                .Include(f => f.LeadAssignment)
                    .ThenInclude(la => la.SalesOfficer)
                .Where(f =>
                    f.LeadAssignment != null &&
                    f.LeadAssignment.LeadId == leadId)
                .OrderBy(f => f.SubmittedAt)
                .ToListAsync();

            if (!feedbacks.Any())
                return null;

            var history =
                new List<SalesManagerFeedbackHistoryViewModel>();

            for (int i = 0; i < feedbacks.Count; i++)
            {
                var feedback = feedbacks[i];

                string lastFeedbackTiming = "Pending";

                if (i > 0)
                {
                    var previousFeedback = feedbacks[i - 1];

                    if (previousFeedback.NextFollowUpDate.HasValue)
                    {
                        lastFeedbackTiming =
                            feedback.SubmittedAt <=
                            previousFeedback.NextFollowUpDate.Value
                                ? "On Time"
                                : "Late";
                    }
                }

                bool isNextFollowUpOverdue =
                    feedback.NextFollowUpDate.HasValue &&
                    feedback.NextFollowUpDate.Value < DateTime.UtcNow;

                history.Add(
                    new SalesManagerFeedbackHistoryViewModel
                    {
                        FeedbackId = feedback.FeedbackId,
                        AssignmentId = feedback.AssignmentId,

                        SubmittedAt = feedback.SubmittedAt,

                        Summary = feedback.Summary,

                        FeedbackStatus =
                            feedback.Status.ToString(),

                        NextFollowUpDate =
                            feedback.NextFollowUpDate,

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
                    });
            }

            var latestFeedback = feedbacks
                .OrderByDescending(f => f.SubmittedAt)
                .First();

            var lead =
                latestFeedback.LeadAssignment?.Lead;

            if (lead == null)
                return null;

            return new SalesManagerFollowUpDetailsViewModel
            {
                LeadId = lead.LeadId,

                LeadName = lead.LeadName,

                SalesOfficerName =
                    latestFeedback.LeadAssignment?.SalesOfficer != null
                        ? latestFeedback
                            .LeadAssignment
                            .SalesOfficer
                            .FullName
                        : "Unassigned",

                FeedbackHistory = history
            };
        }
    }
}