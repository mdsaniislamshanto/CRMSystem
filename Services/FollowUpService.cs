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


        // =========================================================
        // GLOBAL FOLLOW-UPS
        // =========================================================
        // This method is used for the existing global Follow-up flow.
        //
        // Passing null means:
        // No Sales Manager-specific restriction will be applied.
        //
        // Existing behavior remains unchanged.
        // =========================================================

        public async Task<FollowUpFilterViewModel> GetFollowUpsAsync(
            FollowUpFilterViewModel filter)
        {
            return await GetFollowUpsCoreAsync(
                filter,
                null);
        }


        // =========================================================
        // SALES MANAGER FOLLOW-UPS
        // =========================================================
        // This method is used by Sales Manager.
        //
        // Only Follow-ups belonging to:
        //
        // 1. Leads created by this Sales Manager
        // OR
        // 2. Leads assigned to Sales Officers under this
        //    Sales Manager's hierarchy
        //
        // will be included.
        // =========================================================

        public async Task<FollowUpFilterViewModel>
            GetFollowUpsForSalesManagerAsync(
                FollowUpFilterViewModel filter,
                long salesManagerId)
        {
            return await GetFollowUpsCoreAsync(
                filter,
                salesManagerId);
        }


        // =========================================================
        // COMMON FOLLOW-UP PROCESSING
        // =========================================================
        // Both Global and Sales Manager Follow-ups use this method.
        //
        // salesManagerId = null
        //      → Global access
        //
        // salesManagerId = actual ID
        //      → Sales Manager scoped access
        //
        // Keeping the common logic here prevents code duplication.
        // =========================================================

        private async Task<FollowUpFilterViewModel>
            GetFollowUpsCoreAsync(
                FollowUpFilterViewModel filter,
                long? salesManagerId)
        {
            var now = DateTime.UtcNow;


            // =====================================================
            // STEP 1: BUILD FEEDBACK QUERY
            // =====================================================
            // We start from Feedback because NextFollowUpDate,
            // Summary, Status and SubmittedAt are stored there.
            //
            // LeadAssignment gives us:
            //      Lead
            //      Sales Officer
            //
            // Sales Officer -> TeamLead gives us:
            //      Sales Manager
            // =====================================================

            var feedbackQuery =
                _context.Feedbacks
                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la.Lead)

                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la.SalesOfficer)
                            .ThenInclude(so => so.TeamLead)

                    .AsQueryable();


            // =====================================================
            // STEP 2: SALES MANAGER DATA SCOPE
            // =====================================================
            // If salesManagerId has a value, this is a
            // Sales Manager request.
            //
            // A Sales Manager can see:
            //
            // A. Leads created by himself/herself
            //
            // OR
            //
            // B. Leads assigned to Sales Officers who belong
            //    to the Sales Manager's hierarchy.
            //
            // Admin/global request passes null, so this block
            // is skipped.
            // =====================================================

            if (salesManagerId.HasValue)
            {
                var managerId = salesManagerId.Value;

                feedbackQuery =
                    feedbackQuery.Where(f =>
                        f.LeadAssignment != null &&

                        (
                            // ---------------------------------
                            // Rule 1:
                            // Sales Manager created the Lead
                            // ---------------------------------
                            (
                                f.LeadAssignment.Lead != null &&
                                f.LeadAssignment.Lead.CreatedBy ==
                                    managerId
                            )

                            ||

                            // ---------------------------------
                            // Rule 2:
                            // Sales Officer belongs to this
                            // Sales Manager's hierarchy
                            // ---------------------------------
                            (
                                f.LeadAssignment.SalesOfficer != null &&

                                f.LeadAssignment.SalesOfficer.TeamLead != null &&

                                f.LeadAssignment.SalesOfficer.TeamLead
                                    .SalesManagerId == managerId
                            )
                        ));
            }


            // =====================================================
            // STEP 3: EXECUTE DATABASE QUERY
            // =====================================================
            // Only after applying the Sales Manager scope do we
            // load the Feedback records into memory.
            //
            // This is important because unauthorized records
            // should not be loaded first and filtered later.
            // =====================================================

            var feedbacks =
                await feedbackQuery
                    .OrderByDescending(f => f.SubmittedAt)
                    .ToListAsync();


            // =====================================================
            // STEP 4: GET LATEST FOLLOW-UP FOR EACH LEAD
            // =====================================================
            // A Lead can have multiple Feedback records.
            //
            // We need the latest Feedback that contains a
            // NextFollowUpDate.
            // =====================================================

            var latestFollowUps =
                feedbacks
                    .Where(f =>
                        f.LeadAssignment != null &&
                        f.NextFollowUpDate.HasValue)
                    .GroupBy(f =>
                        f.LeadAssignment!.LeadId)
                    .Select(g =>
                        g.OrderByDescending(
                            f => f.NextFollowUpDate)
                         .First())
                    .ToList();


            // =====================================================
            // STEP 5: CREATE FOLLOW-UP VIEW MODELS
            // =====================================================

            var followUps =
                new List<FollowUpViewModel>();


            foreach (var feedback in latestFollowUps)
            {
                // Safety check:
                // A Feedback must have its LeadAssignment and Lead.
                if (feedback.LeadAssignment?.Lead == null)
                {
                    continue;
                }


                var followUpDate =
                    feedback.NextFollowUpDate!.Value;


                // =================================================
                // DETERMINE CURRENT FOLLOW-UP STATUS
                // =================================================

                bool isOverdue =
                    followUpDate < now;

                bool isDueToday =
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


                // =================================================
                // DETERMINE LAST FOLLOW-UP TIMELINESS
                // =================================================
                // We compare the current Feedback's SubmittedAt
                // against the previous Feedback's
                // NextFollowUpDate.
                // =================================================

                string timelinessStatus =
                    "Pending";


                var previousFeedback =
                    feedbacks
                        .Where(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.LeadId ==
                                feedback.LeadAssignment.LeadId &&
                            f.SubmittedAt <
                                feedback.SubmittedAt)
                        .OrderByDescending(
                            f => f.SubmittedAt)
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


                // =================================================
                // CREATE FOLLOW-UP VIEW MODEL
                // =================================================

                followUps.Add(
                    new FollowUpViewModel
                    {
                        FeedbackId =
                            feedback.FeedbackId,

                        AssignmentId =
                            feedback.AssignmentId,

                        LeadId =
                            feedback.LeadAssignment.LeadId,

                        LeadName =
                            feedback.LeadAssignment
                                .Lead.LeadName,

                        SalesOfficerName =
                            feedback.LeadAssignment
                                .SalesOfficer != null
                                ? feedback.LeadAssignment
                                    .SalesOfficer.FullName
                                : "Unassigned",

                        Summary =
                            feedback.Summary,

                        FeedbackStatus =
                            feedback.Status.ToString(),

                        SubmittedAt =
                            feedback.SubmittedAt,

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
                    });
            }


            // =====================================================
            // STEP 6: SEARCH FILTER
            // =====================================================
            // Search is performed only against the already scoped
            // Follow-up list.
            //
            // Therefore a Sales Manager cannot search into another
            // Manager's Follow-ups.
            // =====================================================

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search =
                    filter.Search.Trim();

                followUps =
                    followUps
                        .Where(x =>
                            x.LeadName.Contains(
                                search,
                                StringComparison
                                    .OrdinalIgnoreCase)

                            ||

                            x.SalesOfficerName.Contains(
                                search,
                                StringComparison
                                    .OrdinalIgnoreCase)

                            ||

                            x.Summary.Contains(
                                search,
                                StringComparison
                                    .OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // STEP 7: FOLLOW-UP STATUS FILTER
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    filter.FollowUpStatus))
            {
                followUps =
                    followUps
                        .Where(x =>
                            x.FollowUpStatus.Equals(
                                filter.FollowUpStatus,
                                StringComparison
                                    .OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // STEP 8: FEEDBACK STATUS FILTER
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    filter.FeedbackStatus))
            {
                followUps =
                    followUps
                        .Where(x =>
                            x.FeedbackStatus.Equals(
                                filter.FeedbackStatus,
                                StringComparison
                                    .OrdinalIgnoreCase))
                        .ToList();
            }


            // =====================================================
            // STEP 9: FROM DATE FILTER
            // =====================================================

            if (filter.FromDate.HasValue)
            {
                var fromDate =
                    filter.FromDate.Value.Date;

                followUps =
                    followUps
                        .Where(x =>
                            x.FollowUpDate.Date >=
                                fromDate)
                        .ToList();
            }


            // =====================================================
            // STEP 10: TO DATE FILTER
            // =====================================================

            if (filter.ToDate.HasValue)
            {
                var toDate =
                    filter.ToDate.Value.Date;

                followUps =
                    followUps
                        .Where(x =>
                            x.FollowUpDate.Date <=
                                toDate)
                        .ToList();
            }


            // =====================================================
            // STEP 11: SUMMARY COUNTS / KPI
            // =====================================================
            // Because the list is already scoped, these KPI values
            // are automatically scoped as well.
            // =====================================================

            filter.TotalFollowUps =
                followUps.Count;

            filter.DueTodayCount =
                followUps.Count(x =>
                    x.IsDueToday);

            filter.UpcomingCount =
                followUps.Count(x =>
                    x.FollowUpStatus == "Upcoming");

            filter.OverdueCount =
                followUps.Count(x =>
                    x.IsOverdue);


            // =====================================================
            // STEP 12: SORTING
            // =====================================================

            followUps =
                filter.Sort?.ToLower() switch
                {
                    "latest" =>
                        followUps
                            .OrderByDescending(
                                x => x.FollowUpDate)
                            .ToList(),

                    "oldest" =>
                        followUps
                            .OrderBy(
                                x => x.FollowUpDate)
                            .ToList(),

                    "overdue" =>
                        followUps
                            .OrderByDescending(
                                x => x.IsOverdue)
                            .ThenBy(
                                x => x.FollowUpDate)
                            .ToList(),

                    "leadaz" =>
                        followUps
                            .OrderBy(
                                x => x.LeadName)
                            .ToList(),

                    _ =>
                        followUps
                            .OrderBy(
                                x => x.FollowUpDate)
                            .ToList()
                };


            // =====================================================
            // STEP 13: PAGINATION
            // =====================================================

            filter.TotalItems =
                followUps.Count;


            if (filter.Page < 1)
            {
                filter.Page = 1;
            }


            if (filter.PageSize <= 0)
            {
                filter.PageSize = 10;
            }


            filter.FollowUps =
                followUps
                    .Skip(
                        (filter.Page - 1) *
                        filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();


            // =====================================================
            // STEP 14: RETURN RESULT
            // =====================================================

            return filter;
        }


        // =========================================================
        // GLOBAL FOLLOW-UP DETAILS
        // =========================================================
        // Existing global Details method.
        //
        // Passing null means no Sales Manager restriction.
        // =========================================================

        public async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsAsync(
                long leadId)
        {
            return await GetFollowUpDetailsCoreAsync(
                leadId,
                null);
        }


        // =========================================================
        // SALES MANAGER FOLLOW-UP DETAILS
        // =========================================================
        // This method prevents a Sales Manager from opening another
        // Manager's Follow-up Details through a manually modified URL.
        // =========================================================

        public async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsForSalesManagerAsync(
                long leadId,
                long salesManagerId)
        {
            return await GetFollowUpDetailsCoreAsync(
                leadId,
                salesManagerId);
        }


        // =========================================================
        // COMMON FOLLOW-UP DETAILS PROCESSING
        // =========================================================
        // Used by both:
        //
        // Global Details
        // Sales Manager Details
        //
        // This avoids duplicating the complete history calculation.
        // =========================================================

        private async Task<SalesManagerFollowUpDetailsViewModel?>
            GetFollowUpDetailsCoreAsync(
                long leadId,
                long? salesManagerId)
        {
            // =====================================================
            // STEP 1: BUILD FEEDBACK DETAILS QUERY
            // =====================================================

            var feedbackQuery =
                _context.Feedbacks
                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la.Lead)

                    .Include(f => f.LeadAssignment)
                        .ThenInclude(la => la.SalesOfficer)
                            .ThenInclude(so => so.TeamLead)

                    .Where(f =>
                        f.LeadAssignment != null &&
                        f.LeadAssignment.LeadId == leadId)
                    .AsQueryable();


            // =====================================================
            // STEP 2: SALES MANAGER AUTHORIZATION
            // =====================================================
            // A Sales Manager can access the Details when:
            //
            // 1. The Lead was created by that Sales Manager
            //
            // OR
            //
            // 2. The Sales Officer belongs to that Sales
            //    Manager's hierarchy.
            // =====================================================

            if (salesManagerId.HasValue)
            {
                var managerId =
                    salesManagerId.Value;

                feedbackQuery =
                    feedbackQuery.Where(f =>
                        f.LeadAssignment != null &&

                        (
                            // ---------------------------------
                            // Rule 1:
                            // Lead created by current Manager
                            // ---------------------------------
                            (
                                f.LeadAssignment.Lead != null &&
                                f.LeadAssignment.Lead.CreatedBy ==
                                    managerId
                            )

                            ||

                            // ---------------------------------
                            // Rule 2:
                            // Sales Officer belongs to Manager
                            // ---------------------------------
                            (
                                f.LeadAssignment.SalesOfficer != null &&

                                f.LeadAssignment.SalesOfficer.TeamLead != null &&

                                f.LeadAssignment.SalesOfficer.TeamLead
                                    .SalesManagerId == managerId
                            )
                        ));
            }


            // =====================================================
            // STEP 3: LOAD FEEDBACK HISTORY
            // =====================================================

            var feedbacks =
                await feedbackQuery
                    .OrderBy(
                        f => f.SubmittedAt)
                    .ToListAsync();


            // No Feedback means no Follow-up Details.
            if (!feedbacks.Any())
            {
                return null;
            }


            // =====================================================
            // STEP 4: BUILD FEEDBACK HISTORY
            // =====================================================

            var history =
                new List<SalesManagerFeedbackHistoryViewModel>();


            for (int i = 0;
                 i < feedbacks.Count;
                 i++)
            {
                var feedback =
                    feedbacks[i];


                // Default timing status.
                string lastFeedbackTiming =
                    "Pending";


                // =================================================
                // DETERMINE PREVIOUS FOLLOW-UP TIMELINESS
                // =================================================

                if (i > 0)
                {
                    var previousFeedback =
                        feedbacks[i - 1];


                    if (previousFeedback
                        .NextFollowUpDate
                        .HasValue)
                    {
                        lastFeedbackTiming =
                            feedback.SubmittedAt <=
                            previousFeedback
                                .NextFollowUpDate
                                .Value
                                ? "On Time"
                                : "Late";
                    }
                }


                // =================================================
                // CHECK WHETHER NEXT FOLLOW-UP IS OVERDUE
                // =================================================

                bool isNextFollowUpOverdue =
                    feedback.NextFollowUpDate.HasValue &&
                    feedback.NextFollowUpDate.Value <
                        DateTime.UtcNow;


                // =================================================
                // ADD HISTORY RECORD
                // =================================================

                history.Add(
                    new SalesManagerFeedbackHistoryViewModel
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


            // =====================================================
            // STEP 5: GET LATEST FEEDBACK
            // =====================================================

            var latestFeedback =
                feedbacks
                    .OrderByDescending(
                        f => f.SubmittedAt)
                    .First();


            // =====================================================
            // STEP 6: GET LEAD
            // =====================================================

            var lead =
                latestFeedback
                    .LeadAssignment?
                    .Lead;


            if (lead == null)
            {
                return null;
            }


            // =====================================================
            // STEP 7: RETURN DETAILS VIEW MODEL
            // =====================================================

            return new SalesManagerFollowUpDetailsViewModel
            {
                LeadId =
                    lead.LeadId,

                LeadName =
                    lead.LeadName,

                SalesOfficerName =
                    latestFeedback
                        .LeadAssignment?
                        .SalesOfficer != null
                        ? latestFeedback
                            .LeadAssignment
                            .SalesOfficer
                            .FullName
                        : "Unassigned",

                FeedbackHistory =
                    history
            };
        }
    }
}

//using CRMSystem.Data;
//using CRMSystem.Models.ViewModels;
//using CRMSystem.Services.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace CRMSystem.Services
//{
//    public class FollowUpService : IFollowUpService
//    {
//        private readonly ApplicationDbContext _context;

//        public FollowUpService(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<FollowUpFilterViewModel> GetFollowUpsAsync(
//            FollowUpFilterViewModel filter)
//        {
//            var now = DateTime.UtcNow;

//            var feedbacks = await _context.Feedbacks
//                .Include(f => f.LeadAssignment)
//                    .ThenInclude(la => la.Lead)
//                .Include(f => f.LeadAssignment)
//                    .ThenInclude(la => la.SalesOfficer)
//                .OrderByDescending(f => f.SubmittedAt)
//                .ToListAsync();

//            var latestFollowUps = feedbacks
//                .Where(f =>
//                    f.LeadAssignment != null &&
//                    f.NextFollowUpDate.HasValue)
//                .GroupBy(f => f.LeadAssignment!.LeadId)
//                .Select(g => g
//                    .OrderByDescending(f => f.NextFollowUpDate)
//                    .First())
//                .ToList();

//            var followUps = new List<FollowUpViewModel>();

//            foreach (var feedback in latestFollowUps)
//            {
//                if (feedback.LeadAssignment?.Lead == null)
//                    continue;

//                var followUpDate = feedback.NextFollowUpDate!.Value;

//                bool isOverdue = followUpDate < now;
//                bool isDueToday = followUpDate.Date == now.Date;

//                string followUpStatus;

//                if (isOverdue)
//                {
//                    followUpStatus = "Overdue";
//                }
//                else if (isDueToday)
//                {
//                    followUpStatus = "Due Today";
//                }
//                else
//                {
//                    followUpStatus = "Upcoming";
//                }

//                string timelinessStatus = "Pending";

//                var previousFeedback = feedbacks
//                    .Where(f =>
//                        f.LeadAssignment != null &&
//                        f.LeadAssignment.LeadId ==
//                            feedback.LeadAssignment.LeadId &&
//                        f.SubmittedAt < feedback.SubmittedAt)
//                    .OrderByDescending(f => f.SubmittedAt)
//                    .FirstOrDefault();

//                if (previousFeedback != null &&
//                    previousFeedback.NextFollowUpDate.HasValue)
//                {
//                    timelinessStatus =
//                        feedback.SubmittedAt <=
//                        previousFeedback.NextFollowUpDate.Value
//                            ? "On Time"
//                            : "Late";
//                }

//                followUps.Add(new FollowUpViewModel
//                {
//                    FeedbackId = feedback.FeedbackId,
//                    AssignmentId = feedback.AssignmentId,
//                    LeadId = feedback.LeadAssignment.LeadId,

//                    LeadName = feedback.LeadAssignment.Lead.LeadName,

//                    SalesOfficerName =
//                        feedback.LeadAssignment.SalesOfficer != null
//                            ? feedback.LeadAssignment.SalesOfficer.FullName
//                            : "Unassigned",

//                    Summary = feedback.Summary,

//                    FeedbackStatus = feedback.Status.ToString(),

//                    SubmittedAt = feedback.SubmittedAt,
//                    FollowUpDate = followUpDate,

//                    IsOverdue = isOverdue,
//                    IsDueToday = isDueToday,

//                    FollowUpStatus = followUpStatus,
//                    TimelinessStatus = timelinessStatus
//                });
//            }

//            // Search
//            if (!string.IsNullOrWhiteSpace(filter.Search))
//            {
//                var search = filter.Search.Trim();

//                followUps = followUps
//                    .Where(x =>
//                        x.LeadName.Contains(
//                            search,
//                            StringComparison.OrdinalIgnoreCase)
//                        ||
//                        x.SalesOfficerName.Contains(
//                            search,
//                            StringComparison.OrdinalIgnoreCase)
//                        ||
//                        x.Summary.Contains(
//                            search,
//                            StringComparison.OrdinalIgnoreCase))
//                    .ToList();
//            }

//            // Follow-up Status Filter
//            if (!string.IsNullOrWhiteSpace(filter.FollowUpStatus))
//            {
//                followUps = followUps
//                    .Where(x =>
//                        x.FollowUpStatus.Equals(
//                            filter.FollowUpStatus,
//                            StringComparison.OrdinalIgnoreCase))
//                    .ToList();
//            }

//            // Feedback Status Filter
//            if (!string.IsNullOrWhiteSpace(filter.FeedbackStatus))
//            {
//                followUps = followUps
//                    .Where(x =>
//                        x.FeedbackStatus.Equals(
//                            filter.FeedbackStatus,
//                            StringComparison.OrdinalIgnoreCase))
//                    .ToList();
//            }

//            // From Date
//            if (filter.FromDate.HasValue)
//            {
//                var fromDate = filter.FromDate.Value.Date;

//                followUps = followUps
//                    .Where(x =>
//                        x.FollowUpDate.Date >= fromDate)
//                    .ToList();
//            }

//            // To Date
//            if (filter.ToDate.HasValue)
//            {
//                var toDate = filter.ToDate.Value.Date;

//                followUps = followUps
//                    .Where(x =>
//                        x.FollowUpDate.Date <= toDate)
//                    .ToList();
//            }

//            // Summary Counts
//            filter.TotalFollowUps = followUps.Count;

//            filter.DueTodayCount = followUps.Count(x =>
//                x.IsDueToday);

//            filter.UpcomingCount = followUps.Count(x =>
//                x.FollowUpStatus == "Upcoming");

//            filter.OverdueCount = followUps.Count(x =>
//                x.IsOverdue);

//            // Sorting
//            followUps = filter.Sort?.ToLower() switch
//            {
//                "latest" =>
//                    followUps
//                        .OrderByDescending(x => x.FollowUpDate)
//                        .ToList(),

//                "oldest" =>
//                    followUps
//                        .OrderBy(x => x.FollowUpDate)
//                        .ToList(),

//                "overdue" =>
//                    followUps
//                        .OrderByDescending(x => x.IsOverdue)
//                        .ThenBy(x => x.FollowUpDate)
//                        .ToList(),

//                "leadaz" =>
//                    followUps
//                        .OrderBy(x => x.LeadName)
//                        .ToList(),

//                _ =>
//                    followUps
//                        .OrderBy(x => x.FollowUpDate)
//                        .ToList()
//            };

//            // Pagination
//            filter.TotalItems = followUps.Count;

//            if (filter.Page < 1)
//                filter.Page = 1;

//            if (filter.PageSize <= 0)
//                filter.PageSize = 10;

//            filter.FollowUps = followUps
//                .Skip((filter.Page - 1) * filter.PageSize)
//                .Take(filter.PageSize)
//                .ToList();

//            return filter;
//        }

//        public async Task<SalesManagerFollowUpDetailsViewModel?>
//            GetFollowUpDetailsAsync(long leadId)
//        {
//            var feedbacks = await _context.Feedbacks
//                .Include(f => f.LeadAssignment)
//                    .ThenInclude(la => la.Lead)
//                .Include(f => f.LeadAssignment)
//                    .ThenInclude(la => la.SalesOfficer)
//                .Where(f =>
//                    f.LeadAssignment != null &&
//                    f.LeadAssignment.LeadId == leadId)
//                .OrderBy(f => f.SubmittedAt)
//                .ToListAsync();

//            if (!feedbacks.Any())
//                return null;

//            var history =
//                new List<SalesManagerFeedbackHistoryViewModel>();

//            for (int i = 0; i < feedbacks.Count; i++)
//            {
//                var feedback = feedbacks[i];

//                string lastFeedbackTiming = "Pending";

//                if (i > 0)
//                {
//                    var previousFeedback = feedbacks[i - 1];

//                    if (previousFeedback.NextFollowUpDate.HasValue)
//                    {
//                        lastFeedbackTiming =
//                            feedback.SubmittedAt <=
//                            previousFeedback.NextFollowUpDate.Value
//                                ? "On Time"
//                                : "Late";
//                    }
//                }

//                bool isNextFollowUpOverdue =
//                    feedback.NextFollowUpDate.HasValue &&
//                    feedback.NextFollowUpDate.Value < DateTime.UtcNow;

//                history.Add(
//                    new SalesManagerFeedbackHistoryViewModel
//                    {
//                        FeedbackId = feedback.FeedbackId,
//                        AssignmentId = feedback.AssignmentId,

//                        SubmittedAt = feedback.SubmittedAt,

//                        Summary = feedback.Summary,

//                        FeedbackStatus =
//                            feedback.Status.ToString(),

//                        NextFollowUpDate =
//                            feedback.NextFollowUpDate,

//                        ProofImage =
//                            feedback.ProofImage,

//                        VoiceRecording =
//                            feedback.VoiceRecording,

//                        Notes =
//                            feedback.Notes,

//                        IsNextFollowUpOverdue =
//                            isNextFollowUpOverdue,

//                        LastFeedbackTiming =
//                            lastFeedbackTiming
//                    });
//            }

//            var latestFeedback = feedbacks
//                .OrderByDescending(f => f.SubmittedAt)
//                .First();

//            var lead =
//                latestFeedback.LeadAssignment?.Lead;

//            if (lead == null)
//                return null;

//            return new SalesManagerFollowUpDetailsViewModel
//            {
//                LeadId = lead.LeadId,

//                LeadName = lead.LeadName,

//                SalesOfficerName =
//                    latestFeedback.LeadAssignment?.SalesOfficer != null
//                        ? latestFeedback
//                            .LeadAssignment
//                            .SalesOfficer
//                            .FullName
//                        : "Unassigned",

//                FeedbackHistory = history
//            };
//        }
//    }
//}