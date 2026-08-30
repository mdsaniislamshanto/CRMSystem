using CRMSystem.Data;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SalesOfficerServiceForSalesManager
        : ISalesOfficerServiceForSalesManager
    {
        private readonly ApplicationDbContext _context;

        public SalesOfficerServiceForSalesManager(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // Get All Sales Officers
        // =========================================================

        public async Task<List<SalesOfficerListViewModel>>
            GetSalesOfficersAsync()
        {
            var salesOfficers = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Role != null &&
                    u.Role.RoleKey == "SALES_OFFICER")
                .Select(u => new
                {
                    u.UserId,
                    u.EmployeeCode,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.ProfileImage,
                    u.IsActive,

                    AssignedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId),

                    AcceptedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.AcceptedAt != null),

                    PendingAcceptance = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.AcceptedAt == null),

                    CompletedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.Lead != null &&
                            a.Lead.Status.ToString() == "Completed"),

                    TotalFeedbacks = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId == u.UserId),

                    OverdueFollowUps = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId == u.UserId &&
                            f.NextFollowUpDate.HasValue &&
                            f.NextFollowUpDate.Value < DateTime.UtcNow &&
                            !f.NextFeedbackSLAMissed)
                })
                .ToListAsync();


            // =====================================================
            // Build ViewModel in memory
            // =====================================================

            return salesOfficers
                .Select(u => new SalesOfficerListViewModel
                {
                    UserId = u.UserId,

                    EmployeeCode = u.EmployeeCode,

                    FullName =
                        $"{u.FirstName} {u.LastName}".Trim(),

                    Email = u.Email,

                    PhoneNumber = u.PhoneNumber,

                    ProfileImage = u.ProfileImage,

                    IsActive = u.IsActive,

                    AssignedLeads = u.AssignedLeads,

                    AcceptedLeads = u.AcceptedLeads,

                    PendingAcceptance = u.PendingAcceptance,

                    CompletedLeads = u.CompletedLeads,

                    TotalFeedbacks = u.TotalFeedbacks,

                    OverdueFollowUps = u.OverdueFollowUps
                })
                .OrderBy(u => u.FullName)
                .ToList();
        }


        // =========================================================
        // Get Sales Officer Details
        // =========================================================

        public async Task<SalesOfficerDetailsViewModel?>
            GetSalesOfficerDetailsAsync(long userId)
        {
            var officer = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.UserId == userId &&
                    u.Role != null &&
                    u.Role.RoleKey == "SALES_OFFICER")
                .Select(u => new SalesOfficerDetailsViewModel
                {
                    // =================================================
                    // Basic Information
                    // =================================================

                    UserId = u.UserId,

                    EmployeeCode = u.EmployeeCode,

                    FullName =
                        $"{u.FirstName} {u.LastName}".Trim(),

                    Email = u.Email,

                    PhoneNumber = u.PhoneNumber,

                    ProfileImage = u.ProfileImage,

                    IsActive = u.IsActive,


                    // =================================================
                    // Lead Performance
                    // =================================================

                    AssignedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId),

                    AcceptedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.AcceptedAt != null),

                    PendingAcceptance = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.AcceptedAt == null),

                    CompletedLeads = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.Lead != null &&
                            a.Lead.Status.ToString() == "Completed"),

                    TotalFeedbacks = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId ==
                            u.UserId),

                    OverdueFollowUps = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId ==
                            u.UserId &&
                            f.NextFollowUpDate.HasValue &&
                            f.NextFollowUpDate.Value < DateTime.UtcNow &&
                            !f.NextFeedbackSLAMissed),


                    // =================================================
                    // SLA Performance
                    // =================================================

                    AcceptanceSLAMissed = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.AcceptanceSLAMissed),

                    FirstFeedbackSLAMissed = _context.LeadAssignments
                        .Count(a =>
                            a.SalesOfficerId == u.UserId &&
                            a.FirstFeedbackSLAMissed),

                    NextFeedbackSLAMissed = _context.Feedbacks
                        .Count(f =>
                            f.LeadAssignment != null &&
                            f.LeadAssignment.SalesOfficerId ==
                            u.UserId &&
                            f.NextFeedbackSLAMissed)
                })
                .FirstOrDefaultAsync();


            if (officer == null)
            {
                return null;
            }


            // =========================================================
            // Acceptance Rate
            // =========================================================

            officer.AcceptanceRate =
                officer.AssignedLeads > 0
                    ? (double)officer.AcceptedLeads /
                      officer.AssignedLeads * 100
                    : 0;


            // =========================================================
            // Recent Lead Activity
            // =========================================================

            officer.RecentLeads =
                await _context.LeadAssignments
                    .AsNoTracking()
                    .Where(a =>
                        a.SalesOfficerId == userId &&
                        a.Lead != null)
                    .OrderByDescending(a => a.AssignedAt)
                    .Take(10)
                    .Select(a => new SalesOfficerLeadActivityViewModel
                    {
                        LeadId = a.Lead!.LeadId,

                        LeadCode = a.Lead.LeadCode,

                        LeadName = a.Lead.LeadName,

                        AssignedAt = a.AssignedAt,

                        AcceptedAt = a.AcceptedAt,

                        Status = a.Lead.Status.ToString(),

                        NextFollowUpDate =
                            _context.Feedbacks
                                .Where(f =>
                                    f.AssignmentId ==
                                    a.AssignmentId &&
                                    f.NextFollowUpDate.HasValue)
                                .OrderByDescending(f =>
                                    f.SubmittedAt)
                                .Select(f =>
                                    f.NextFollowUpDate)
                                .FirstOrDefault()
                    })
                    .ToListAsync();


            // =========================================================
            // Recent Feedback Activity
            // =========================================================

            officer.RecentFeedbacks =
                await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        f.LeadAssignment != null &&
                        f.LeadAssignment.SalesOfficerId ==
                        userId)
                    .OrderByDescending(f =>
                        f.SubmittedAt)
                    .Take(10)
                    .Select(f =>
                        new SalesOfficerFeedbackActivityViewModel
                        {
                            FeedbackId = f.FeedbackId,

                            LeadId =
                                f.LeadAssignment!.LeadId,

                            LeadName =
                                f.LeadAssignment.Lead != null
                                    ? f.LeadAssignment.Lead.LeadName
                                    : "Unknown Lead",

                            SubmittedAt = f.SubmittedAt,

                            Status = f.Status.ToString(),

                            NextFollowUpDate =
                                f.NextFollowUpDate,

                            NextFeedbackSLAMissed =
                                f.NextFeedbackSLAMissed
                        })
                    .ToListAsync();


            return officer;
        }
    }
}