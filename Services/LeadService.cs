using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRMSystem.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAutoAssignmentService _autoAssignmentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;

        public LeadService(
            ApplicationDbContext context,
            IEmailService emailService,
            IAutoAssignmentService autoAssignmentService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _autoAssignmentService = autoAssignmentService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }


        // =========================================================
        // API LEADS for admin
        // =========================================================

        public async Task<List<ApiLeadViewModel>> GetApiLeadsAsync(
            string? search = null,
            LeadSource? source = null,
            LeadStatus? status = null)
        {
            var query = _context.Leads
                .Where(l =>
                    !l.IsDeleted &&
                    _context.LeadCaptureLogs.Any(c =>
                        c.LeadId == l.LeadId &&
                        c.CaptureStatus == CaptureStatus.Success &&
                        !c.IsDeleted))
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(l =>
                    l.LeadName.Contains(search) ||
                    (l.CompanyName != null &&
                     l.CompanyName.Contains(search)) ||
                    (l.Email != null &&
                     l.Email.Contains(search)) ||
                    l.Phone.Contains(search) ||
                    l.LeadCode.Contains(search));
            }

            // Source filter
            if (source.HasValue)
            {
                query = query.Where(l =>
                    l.Source == source.Value);
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(l =>
                    l.Status == status.Value);
            }

            return await query
                .Select(l => new ApiLeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    CompanyName = l.CompanyName,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Profession = l.Profession,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,

                    AssignedTo = _context.LeadAssignments
                        .Where(a =>
                            a.LeadId == l.LeadId &&
                            a.IsActive &&
                            !a.IsDeleted)
                        .Select(a => a.SalesOfficer!.FullName)
                        .FirstOrDefault(),

                    CreatedAt = l.CreatedAt,
                    LastContactDate = l.LastContactDate
                })
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }



        // =========================================================
        // API LEADS for Sales Manager
        // =========================================================
        public async Task<List<ApiLeadViewModel>>
                    GetApiLeadsForSalesManagerAsync(
                        long salesManagerId,
                        string? search = null,
                        LeadSource? source = null,
                        LeadStatus? status = null)
        {
            var query =
                _context.Leads
                    .Where(l =>
                        !l.IsDeleted &&

                        // -------------------------------------------------
                        // Must be an API captured Lead
                        // -------------------------------------------------
                        _context.LeadCaptureLogs.Any(c =>
                            c.LeadId == l.LeadId &&
                            c.CaptureStatus == CaptureStatus.Success &&
                            !c.IsDeleted) &&

                        // -------------------------------------------------
                        // Manager ownership
                        // -------------------------------------------------
                        (
                            // New API Lead routed to Manager queue
                            l.SalesManagerId == salesManagerId

                            ||

                            // Backward compatibility for older API Leads
                            // already assigned under this Manager
                            _context.LeadAssignments.Any(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted &&
                                a.SalesOfficer != null &&
                                a.SalesOfficer.TeamLead != null &&
                                a.SalesOfficer.TeamLead.SalesManagerId ==
                                    salesManagerId)
                        ))
                    .AsQueryable();


            // =============================================================
            // SEARCH
            // =============================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(l =>
                    l.LeadName.Contains(search) ||

                    (l.CompanyName != null &&
                     l.CompanyName.Contains(search)) ||

                    (l.Email != null &&
                     l.Email.Contains(search)) ||

                    l.Phone.Contains(search) ||

                    l.LeadCode.Contains(search));
            }


            // =============================================================
            // SOURCE FILTER
            // =============================================================

            if (source.HasValue)
            {
                query = query.Where(l =>
                    l.Source == source.Value);
            }


            // =============================================================
            // STATUS FILTER
            // =============================================================

            if (status.HasValue)
            {
                query = query.Where(l =>
                    l.Status == status.Value);
            }


            // =============================================================
            // PROJECTION
            // =============================================================

            return await query
                .Select(l => new ApiLeadViewModel
                {
                    LeadId =
                        l.LeadId,

                    LeadCode =
                        l.LeadCode,

                    LeadName =
                        l.LeadName,

                    CompanyName =
                        l.CompanyName,

                    Email =
                        l.Email,

                    Phone =
                        l.Phone,

                    Address =
                        l.Address,

                    Profession =
                        l.Profession,

                    Source =
                        l.Source,

                    Priority =
                        l.Priority,

                    Status =
                        l.Status,

                    AssignedTo =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    CreatedAt =
                        l.CreatedAt,

                    LastContactDate =
                        l.LastContactDate
                })
                .OrderByDescending(l =>
                    l.CreatedAt)
                .ToListAsync();
        }






        // =========================================================
        // GET ALL ACTIVE LEADS
        // =========================================================

        public async Task<List<LeadViewModel>> GetAllLeadsAsync()
        {
            var leads = await _context.Leads
                .Where(l =>
                    !l.IsArchived &&
                    !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    Description = l.Description,
                    FollowUpDate = l.FollowUpDate,

                    AssignedOfficerName =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    AssignedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                (DateTime?)a.AssignedAt)
                            .FirstOrDefault(),

                    AcceptedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                a.AcceptedAt)
                            .FirstOrDefault(),

                    AssignmentStatus =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                (AssignmentStatus?)
                                a.AssignmentStatus)
                            .FirstOrDefault(),

                    AcceptanceSLAMissed =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a =>
                                a.AssignedAt)
                            .Select(a =>
                                a.AcceptanceSLAMissed)
                            .FirstOrDefault()
                })
                .ToListAsync();

            // =====================================================
            // Acceptance SLA Calculation
            // SLA = 1 hour after assignment
            // =====================================================

            var now =
                DateTime.UtcNow;

            foreach (var lead in leads)
            {
                // -------------------------------------------------
                // No active assignment
                // -------------------------------------------------

                if (!lead.AssignedAt.HasValue ||
                    !lead.AssignmentStatus.HasValue)
                {
                    lead.AcceptanceSLAStatus =
                        "Not Assigned";

                    lead.AcceptanceSLAMissed =
                        false;

                    continue;
                }

                var assignedAt =
                    lead.AssignedAt.Value;

                var slaDeadline =
                    assignedAt.AddHours(1);

                // -------------------------------------------------
                // Accepted Lead
                // -------------------------------------------------

                if (lead.AcceptedAt.HasValue)
                {
                    if (lead.AcceptedAt.Value <= slaDeadline)
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

                    continue;
                }

                // -------------------------------------------------
                // Pending Assignment
                // -------------------------------------------------

                if (lead.AssignmentStatus.Value ==
                    AssignmentStatus.Pending)
                {
                    if (now > slaDeadline)
                    {
                        lead.AcceptanceSLAStatus =
                            "SLA Breached";

                        lead.AcceptanceSLAMissed =
                            true;
                    }
                    else
                    {
                        var remaining =
                            slaDeadline - now;

                        var remainingMinutes =
                            Math.Max(
                                0,
                                (int)Math.Ceiling(
                                    remaining.TotalMinutes));

                        lead.AcceptanceSLAStatus =
                            $"Pending • {remainingMinutes} min left";

                        lead.AcceptanceSLAMissed =
                            false;
                    }

                    continue;
                }

                // -------------------------------------------------
                // Other Assignment Status
                // -------------------------------------------------

                lead.AcceptanceSLAStatus =
                    lead.AssignmentStatus.Value
                        .ToString();
            }

            return leads;
        }



        // =========================================================
        // GET ACTIVE LEADS FOR SALES MANAGER
        // =========================================================
        public async Task<List<LeadViewModel>>
            GetLeadsForSalesManagerAsync(
                long salesManagerId)
        {
            var leads =
                await _context.Leads
                    .Where(l =>
                        !l.IsArchived &&
                        !l.IsDeleted &&

                        (
                            // =================================================
                            // 1. Created by current Sales Manager
                            // =================================================
                            l.CreatedBy == salesManagerId

                            ||

                            // =================================================
                            // 2. API / Queue Lead owned by current Manager
                            // =================================================
                            l.SalesManagerId == salesManagerId

                            ||

                            // =================================================
                            // 3. Assigned to Officer under current Manager
                            // =================================================
                            _context.LeadAssignments.Any(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted &&
                                a.SalesOfficer != null &&
                                a.SalesOfficer.TeamLead != null &&
                                a.SalesOfficer.TeamLead.SalesManagerId ==
                                    salesManagerId)
                        ))
                    .OrderByDescending(l =>
                        l.CreatedAt)
                    .Select(l => new LeadViewModel
                    {
                        LeadId =
                            l.LeadId,

                        LeadCode =
                            l.LeadCode,

                        CompanyName =
                            l.CompanyName,

                        LeadName =
                            l.LeadName,

                        Profession =
                            l.Profession,

                        Email =
                            l.Email,

                        Phone =
                            l.Phone,

                        Address =
                            l.Address,

                        Source =
                            l.Source,

                        Priority =
                            l.Priority,

                        Status =
                            l.Status,

                        Description =
                            l.Description,

                        FollowUpDate =
                            l.FollowUpDate,

                        AssignedOfficerName =
                            _context.LeadAssignments
                                .Where(a =>
                                    a.LeadId == l.LeadId &&
                                    a.IsActive &&
                                    !a.IsDeleted)
                                .OrderByDescending(a =>
                                    a.AssignedAt)
                                .Select(a =>
                                    a.SalesOfficer != null
                                        ? a.SalesOfficer.FullName
                                        : null)
                                .FirstOrDefault(),

                        AssignedAt =
                            _context.LeadAssignments
                                .Where(a =>
                                    a.LeadId == l.LeadId &&
                                    a.IsActive &&
                                    !a.IsDeleted)
                                .OrderByDescending(a =>
                                    a.AssignedAt)
                                .Select(a =>
                                    (DateTime?)a.AssignedAt)
                                .FirstOrDefault(),

                        AcceptedAt =
                            _context.LeadAssignments
                                .Where(a =>
                                    a.LeadId == l.LeadId &&
                                    a.IsActive &&
                                    !a.IsDeleted)
                                .OrderByDescending(a =>
                                    a.AssignedAt)
                                .Select(a =>
                                    a.AcceptedAt)
                                .FirstOrDefault(),

                        AssignmentStatus =
                            _context.LeadAssignments
                                .Where(a =>
                                    a.LeadId == l.LeadId &&
                                    a.IsActive &&
                                    !a.IsDeleted)
                                .OrderByDescending(a =>
                                    a.AssignedAt)
                                .Select(a =>
                                    (AssignmentStatus?)a.AssignmentStatus)
                                .FirstOrDefault(),

                        AcceptanceSLAMissed =
                            _context.LeadAssignments
                                .Where(a =>
                                    a.LeadId == l.LeadId &&
                                    a.IsActive &&
                                    !a.IsDeleted)
                                .OrderByDescending(a =>
                                    a.AssignedAt)
                                .Select(a =>
                                    a.AcceptanceSLAMissed)
                                .FirstOrDefault()
                    })
                    .ToListAsync();


            foreach (var lead in leads)
            {
                CalculateAcceptanceSLA(lead);
            }


            return leads;
        }



        // =========================================================
        // CREATE MANUAL LEAD
        // =========================================================
        public async Task CreateLeadAsync(
    CreateLeadViewModel model)
        {
            // 1. Current user
            var httpContext =
                _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException(
                    "HTTP context is not available.");
            }

            var userIdClaim =
                httpContext.User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!long.TryParse(
                    userIdClaim,
                    out long userId))
            {
                throw new InvalidOperationException(
                    "Current logged-in user could not be identified.");
            }

            // 2. Create Lead
            var lead =
                CreateLeadEntity(model);

            // 3. Set actual creator
            lead.CreatedBy =
                userId;

            // 4. Save
            _context.Leads.Add(lead);

            await _context.SaveChangesAsync();

            // 5. Generate Lead Code
            lead.LeadCode =
                $"L{lead.LeadId:D6}";

            await _context.SaveChangesAsync();

            // 6. Auto Assignment
            await _autoAssignmentService
                .AutoAssignLeadAsync(
                    lead.LeadId,
                    userId);
        }


        //// =========================================================
        //// CREATE MANUAL LEAD
        //// =========================================================

        //public async Task CreateLeadAsync(CreateLeadViewModel model)
        //{
        //    var lead = CreateLeadEntity(model);

        //    _context.Leads.Add(lead);

        //    await _context.SaveChangesAsync();

        //    lead.LeadCode =
        //        $"L{lead.LeadId:D6}";

        //    await _context.SaveChangesAsync();

        //    // =====================================================
        //    // Current Logged-in User
        //    // =====================================================

        //    var httpContext =
        //        _httpContextAccessor.HttpContext;

        //    if (httpContext == null)
        //    {
        //        throw new InvalidOperationException(
        //            "HTTP context is not available.");
        //    }

        //    var userIdClaim =
        //        httpContext.User.FindFirstValue(
        //            ClaimTypes.NameIdentifier);

        //    if (!long.TryParse(
        //            userIdClaim,
        //            out long userId))
        //    {
        //        throw new InvalidOperationException(
        //            "Current logged-in user could not be identified.");
        //    }



        // =========================================================
        // AUTO LEAD CAPTURE
        // =========================================================

        public async Task<long> CreateLeadFromCaptureAsync(
      AutoLeadCreateViewModel model)
        {
            var receivedAt =
                DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(
                    model.SourceReferenceId))
            {
                throw new InvalidOperationException(
                    "SourceReferenceId is required for automatic lead capture.");
            }

            // =========================================================
            // LEVEL 1 — SourceReferenceId Duplicate Check
            // =========================================================

            var existingLead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.Source == model.Source &&
                        l.SourceReferenceId ==
                        model.SourceReferenceId &&
                        !l.IsDeleted);

            if (existingLead != null)
            {
                var duplicateLog =
                    new LeadCaptureLog
                    {
                        LeadId =
                            existingLead.LeadId,

                        CaptureSource =
                            LeadCaptureSource.GoogleForm,

                        CaptureStatus =
                            CaptureStatus.Duplicate,

                        ExternalLeadId =
                            model.SourceReferenceId,

                        PayloadJson =
                            model.PayloadJson,

                        ReceivedAt =
                            receivedAt,

                        ProcessedAt =
                            DateTime.UtcNow,

                        IsActive = true
                    };

                await _context.LeadCaptureLogs
                    .AddAsync(duplicateLog);

                await _context.SaveChangesAsync();

                return existingLead.LeadId;
            }

            // =========================================================
            // LEVEL 2 — Phone Number Duplicate Check
            // =========================================================

            var normalizedIncomingPhone =
                NormalizePhoneNumber(model.Phone);

            if (string.IsNullOrWhiteSpace(
                    normalizedIncomingPhone))
            {
                throw new InvalidOperationException(
                    "Phone number is required for automatic lead capture.");
            }

            // ---------------------------------------------------------
            // Get active leads only.
            //
            // Completed and Rejected leads are intentionally included
            // here because their phone numbers are allowed to create
            // a new lead.
            // ---------------------------------------------------------

            var activePhoneCandidates =
                await _context.Leads
                    .Where(l =>
                        !l.IsDeleted &&
                        l.Phone != null &&
                        l.Status != LeadStatus.Completed &&
                        l.Status != LeadStatus.Rejected)
                    .Select(l => new
                    {
                        l.LeadId,
                        l.Phone
                    })
                    .ToListAsync();

            var phoneDuplicate =
                activePhoneCandidates
                    .FirstOrDefault(l =>
                        NormalizePhoneNumber(l.Phone) ==
                        normalizedIncomingPhone);

            if (phoneDuplicate != null)
            {
                var duplicateLog =
                    new LeadCaptureLog
                    {
                        LeadId =
                            phoneDuplicate.LeadId,

                        CaptureSource =
                            LeadCaptureSource.GoogleForm,

                        CaptureStatus =
                            CaptureStatus.Duplicate,

                        ExternalLeadId =
                            model.SourceReferenceId,

                        PayloadJson =
                            model.PayloadJson,

                        ReceivedAt =
                            receivedAt,

                        ProcessedAt =
                            DateTime.UtcNow,

                        IsActive = true
                    };

                await _context.LeadCaptureLogs
                    .AddAsync(duplicateLog);

                await _context.SaveChangesAsync();

                return phoneDuplicate.LeadId;
            }

            try
            {
                // =====================================================
                // CREATE LEAD
                // =====================================================

                var lead =
                    CreateLeadEntity(model);

                _context.Leads.Add(lead);

                await _context.SaveChangesAsync();

                lead.LeadCode =
                    $"L{lead.LeadId:D6}";

                await _context.SaveChangesAsync();

                // =====================================================
                // SUCCESS CAPTURE LOG
                // =====================================================

                var captureLog =
                    new LeadCaptureLog
                    {
                        LeadId =
                            lead.LeadId,

                        CaptureSource =
                            LeadCaptureSource.GoogleForm,

                        CaptureStatus =
                            CaptureStatus.Success,

                        ExternalLeadId =
                            model.SourceReferenceId,

                        PayloadJson =
                            model.PayloadJson,

                        ReceivedAt =
                            receivedAt,

                        ProcessedAt =
                            DateTime.UtcNow,

                        IsActive = true
                    };

                await _context.LeadCaptureLogs
                    .AddAsync(captureLog);

                await _context.SaveChangesAsync();

                // =====================================================
                // AUTO ASSIGNMENT
                //
                // AutoAssignmentService itself checks
                // SystemSettings.AutoAssignmentEnabled.
                // =====================================================

                await _autoAssignmentService
                    .AutoAssignLeadAsync(
                        lead.LeadId);

                return lead.LeadId;
            }
            catch (Exception ex)
            {
                // =====================================================
                // FAILED CAPTURE LOG
                // =====================================================

                var captureLog =
                    new LeadCaptureLog
                    {
                        LeadId = null,

                        CaptureSource =
                            LeadCaptureSource.GoogleForm,

                        CaptureStatus =
                            CaptureStatus.Failed,

                        ExternalLeadId =
                            model.SourceReferenceId,

                        PayloadJson =
                            model.PayloadJson,

                        ErrorMessage =
                            ex.Message,

                        ReceivedAt =
                            receivedAt,

                        ProcessedAt =
                            DateTime.UtcNow,

                        IsActive = true
                    };

                await _context.LeadCaptureLogs
                    .AddAsync(captureLog);

                await _context.SaveChangesAsync();

                throw;
            }
        }

        // =========================================================
        // GET LEAD DETAILS
        // =========================================================

        public async Task<LeadViewModel?> GetLeadByIdAsync(long id)
        {
            var lead = await _context.Leads
                .Where(l =>
                    l.LeadId == id &&
                    !l.IsArchived &&
                    !l.IsDeleted)
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    Description = l.Description,
                    FollowUpDate = l.FollowUpDate,

                    AssignedOfficerName =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    AssignedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (DateTime?)a.AssignedAt)
                            .FirstOrDefault(),

                    AcceptedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.AcceptedAt)
                            .FirstOrDefault(),

                    AssignmentStatus =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (AssignmentStatus?)
                                a.AssignmentStatus)
                            .FirstOrDefault(),

                    AcceptanceSLAMissed =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.AcceptanceSLAMissed)
                            .FirstOrDefault()
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
        // GET LEAD FOR SALES MANAGER Only
        // =========================================================
        public async Task<LeadViewModel?>
     GetLeadForSalesManagerAsync(
         long leadId,
         long salesManagerId)
        {
            var lead = await _context.Leads
                .Where(l =>
                    l.LeadId == leadId &&
                    !l.IsArchived &&
                    !l.IsDeleted &&
                    (
                        // 1. Created by current Sales Manager
                        l.CreatedBy == salesManagerId

                        ||

                        // 2. API / Queue Lead owned by current Sales Manager
                        l.SalesManagerId == salesManagerId

                        ||

                        // 3. Assigned to Officer under current Sales Manager
                        _context.LeadAssignments.Any(a =>
                            a.LeadId == l.LeadId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.SalesOfficer != null &&
                            a.SalesOfficer.TeamLead != null &&
                            a.SalesOfficer.TeamLead.SalesManagerId ==
                                salesManagerId)
                    ))
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    Description = l.Description,
                    FollowUpDate = l.FollowUpDate,

                    AssignedOfficerName =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    AssignedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (DateTime?)a.AssignedAt)
                            .FirstOrDefault(),

                    AcceptedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a => a.AcceptedAt)
                            .FirstOrDefault(),

                    AssignmentStatus =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (AssignmentStatus?)a.AssignmentStatus)
                            .FirstOrDefault(),

                    AcceptanceSLAMissed =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a => a.AcceptanceSLAMissed)
                            .FirstOrDefault()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (lead == null)
            {
                return null;
            }

            CalculateAcceptanceSLA(lead);

            return lead;
        }


        // =========================================================
        // GET ARCHIVED LEAD DETAILS For admin
        // =========================================================

        public async Task<LeadViewModel?> GetArchivedLeadByIdAsync(long id)
        {
            return await _context.Leads
                .Where(l =>
                    l.LeadId == id &&
                    l.IsArchived &&
                    !l.IsDeleted)
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,

                    LeadCode = l.LeadCode,

                    CompanyName = l.CompanyName,

                    LeadName = l.LeadName,

                    Profession = l.Profession,

                    Email = l.Email,

                    Phone = l.Phone,

                    Address = l.Address,

                    Source = l.Source,

                    Priority = l.Priority,

                    Status = l.Status,

                    Description = l.Description,

                    FollowUpDate = l.FollowUpDate,

                    // ==========================================
                    // Archive Information
                    // ==========================================

                    IsArchived = l.IsArchived,

                    ArchivedAt = l.ArchivedAt,

                    ArchivedByName = _context.Users
                        .Where(u => u.UserId == l.ArchivedBy)
                        .Select(u => u.FullName)
                        .FirstOrDefault(),

                    // ==========================================
                    // Assignment Information
                    // ==========================================

                    AssignedOfficerName =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    AssignedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (DateTime?)a.AssignedAt)
                            .FirstOrDefault(),

                    AcceptedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.AcceptedAt)
                            .FirstOrDefault(),

                    AssignmentStatus =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (AssignmentStatus?)
                                a.AssignmentStatus)
                            .FirstOrDefault(),

                    AcceptanceSLAMissed =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                a.IsActive &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.AcceptanceSLAMissed)
                            .FirstOrDefault()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }






        // =========================================================
        // GET ARCHIVED LEAD DETAILS For sales manager
        // =========================================================
        public async Task<LeadViewModel?>
    GetArchivedLeadForSalesManagerAsync(
        long leadId,
        long salesManagerId)
        {
            var lead = await _context.Leads
                .Where(l =>
                    l.LeadId == leadId &&
                    l.IsArchived &&
                    !l.IsDeleted &&
                    (
                        // 1. Lead manually created by this Sales Manager
                        l.CreatedBy == salesManagerId

                        ||

                        // 2. Lead belongs to this Sales Manager's queue
                        l.SalesManagerId == salesManagerId

                        ||

                        // 3. Lead was historically assigned
                        //    to a Sales Officer under this Manager
                        _context.LeadAssignments.Any(a =>
                            a.LeadId == l.LeadId &&
                            !a.IsDeleted &&
                            a.SalesOfficer != null &&
                            a.SalesOfficer.TeamLead != null &&
                            a.SalesOfficer.TeamLead.SalesManagerId ==
                                salesManagerId
                        )
                    ))
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,

                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,

                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,

                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,

                    Description = l.Description,

                    FollowUpDate = l.FollowUpDate,
                    LastContactDate = l.LastContactDate,

                    IsArchived = l.IsArchived,
                    ArchivedAt = l.ArchivedAt,

                    AssignedOfficerName =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                a.SalesOfficer != null
                                    ? a.SalesOfficer.FullName
                                    : null)
                            .FirstOrDefault(),

                    AssignedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (DateTime?)a.AssignedAt)
                            .FirstOrDefault(),

                    AcceptedAt =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a => a.AcceptedAt)
                            .FirstOrDefault(),

                    AssignmentStatus =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a =>
                                (AssignmentStatus?)a.AssignmentStatus)
                            .FirstOrDefault(),

                    AcceptanceSLAMissed =
                        _context.LeadAssignments
                            .Where(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted)
                            .OrderByDescending(a => a.AssignedAt)
                            .Select(a => a.AcceptanceSLAMissed)
                            .FirstOrDefault()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (lead == null)
            {
                return null;
            }

            CalculateAcceptanceSLA(lead);

            return lead;
        }









        // =========================================================
        // EDIT General
        // =========================================================

        public async Task<EditLeadViewModel?> GetLeadForEditAsync(long id)
        {
            return await _context.Leads
                .Where(l =>
                    l.LeadId == id &&
                    !l.IsArchived)
                .Select(l => new EditLeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    Description = l.Description,
                    FollowUpDate = l.FollowUpDate
                })
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // GET LEAD FOR SALES MANAGER EDIT
        // =========================================================
        public async Task<EditLeadViewModel?>
            GetLeadForEditForSalesManagerAsync(
                long leadId,
                long salesManagerId)
        {
            return await _context.Leads
                .Where(l =>
                    l.LeadId == leadId &&
                    !l.IsArchived &&
                    !l.IsDeleted &&
                    (
                        // 1. Created by current Sales Manager
                        l.CreatedBy == salesManagerId

                        ||

                        // 2. Owned by current Sales Manager queue
                        l.SalesManagerId == salesManagerId

                        ||

                        // 3. Assigned under current Sales Manager
                        _context.LeadAssignments.Any(a =>
                            a.LeadId == l.LeadId &&
                            a.IsActive &&
                            !a.IsDeleted &&
                            a.SalesOfficer != null &&
                            a.SalesOfficer.TeamLead != null &&
                            a.SalesOfficer.TeamLead.SalesManagerId ==
                                salesManagerId)
                    ))
                .Select(l => new EditLeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    Description = l.Description,
                    FollowUpDate = l.FollowUpDate
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // UPDATE Genaral
        // =========================================================

        public async Task UpdateLeadAsync(
            EditLeadViewModel model)
        {
            var lead =
                await _context.Leads
                    .FindAsync(model.LeadId);

            if (lead == null ||
                lead.IsArchived)
            {
                return;
            }

            lead.CompanyName =
                model.CompanyName;

            lead.LeadName =
                model.LeadName;

            lead.Profession =
                model.Profession;

            lead.Email =
                model.Email;

            lead.Phone =
                model.Phone;

            lead.Address =
                model.Address;

            lead.Source =
                model.Source;

            lead.Priority =
                model.Priority;

            lead.Status =
                model.Status;

            lead.Description =
                model.Description;

            lead.FollowUpDate =
                model.FollowUpDate;

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // UPDATE LEAD FOR SALES MANAGER
        // =========================================================

        public async Task<bool> UpdateLeadForSalesManagerAsync(
            EditLeadViewModel model,
            long salesManagerId)
        {
            var lead = await _context.Leads
    .FirstOrDefaultAsync(l =>
        l.LeadId == model.LeadId &&
        !l.IsDeleted &&
        !l.IsArchived &&
        (
            l.CreatedBy == salesManagerId ||

            l.SalesManagerId == salesManagerId ||

            _context.LeadAssignments.Any(a =>
                a.LeadId == l.LeadId &&
                a.IsActive &&
                !a.IsDeleted &&
                a.SalesOfficer != null &&
                a.SalesOfficer.TeamLead != null &&
                a.SalesOfficer.TeamLead.SalesManagerId ==
                    salesManagerId)
        ));

            if (lead == null)
            {
                return false;
            }

            lead.CompanyName = model.CompanyName;
            lead.LeadName = model.LeadName;
            lead.Profession = model.Profession;
            lead.Email = model.Email;
            lead.Phone = model.Phone;
            lead.Address = model.Address;
            lead.Source = model.Source;
            lead.Priority = model.Priority;
            lead.Status = model.Status;
            lead.Description = model.Description;
            lead.FollowUpDate = model.FollowUpDate;

            await _context.SaveChangesAsync();

            return true;
        }


        // =========================================================
        // ARCHIVE For Admin
        // =========================================================

        public async Task ArchiveLeadAsync(
            long id,
            long archivedBy)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == id &&
                        !l.IsDeleted);

            if (lead == null ||
                lead.IsArchived)
            {
                return;
            }

            // -----------------------------------------------------
            // Archive Lead
            // -----------------------------------------------------

            lead.IsArchived = true;

            // -----------------------------------------------------
            // Archive Metadata
            // -----------------------------------------------------

            lead.ArchivedAt =
                DateTime.UtcNow;

            lead.ArchivedBy =
                archivedBy;

            await _context.SaveChangesAsync();
        }




        // =========================================================
        // ARCHIVE LEAD FOR SALES MANAGER
        // =========================================================

        public async Task<bool>
            ArchiveLeadForSalesManagerAsync(
                long leadId,
                long salesManagerId)
        {
            // =====================================================
            // 1. Find Active Lead
            // =====================================================

            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == leadId &&
                        !l.IsDeleted &&
                        !l.IsArchived);

            if (lead == null)
            {
                return false;
            }

            // =====================================================
            // 2. Check Sales Manager Access
            //
            // Manager can archive:
            // A. Lead created by himself/herself
            // OR
            // B. Lead assigned to an Officer under himself/herself
            // =====================================================

            var hasAccess =
                lead.CreatedBy == salesManagerId
                ||
                await _context.LeadAssignments
                    .AnyAsync(a =>
                        a.LeadId == leadId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.SalesOfficer != null &&
                        a.SalesOfficer.TeamLead != null &&
                        a.SalesOfficer.TeamLead.SalesManagerId ==
                            salesManagerId);

            if (!hasAccess)
            {
                return false;
            }

            // =====================================================
            // 3. Archive Lead
            // =====================================================

            lead.IsArchived =
                true;

            // =====================================================
            // 4. Archive Metadata
            // =====================================================

            lead.ArchivedAt =
                DateTime.UtcNow;

            lead.ArchivedBy =
                salesManagerId;

            // =====================================================
            // 5. Save Changes
            // =====================================================

            await _context.SaveChangesAsync();

            return true;
        }





        // =========================================================
        // ARCHIVED LEADS For admin
        // =========================================================

        public async Task<List<LeadViewModel>> GetArchivedLeadsAsync()
        {
            return await _context.Leads
                .Where(l =>
                    l.IsArchived &&
                    !l.IsDeleted)
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,

                    LeadCode = l.LeadCode,

                    CompanyName = l.CompanyName,

                    LeadName = l.LeadName,

                    Profession = l.Profession,

                    Email = l.Email,

                    Phone = l.Phone,

                    Address = l.Address,

                    Source = l.Source,

                    Priority = l.Priority,

                    Status = l.Status,

                    Description = l.Description,

                    FollowUpDate = l.FollowUpDate,

                    // ==========================================
                    // Archive Information
                    // ==========================================

                    IsArchived = l.IsArchived,

                    ArchivedAt = l.ArchivedAt,

                    ArchivedByName = _context.Users
                        .Where(u => u.UserId == l.ArchivedBy)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                })
                .OrderByDescending(l => l.ArchivedAt)
                .ToListAsync();
        }



        // =========================================================
        // GET ARCHIVED LEADS FOR SALES MANAGER
        // =========================================================

        public async Task<List<LeadViewModel>>
            GetArchivedLeadsForSalesManagerAsync(
                long salesManagerId)
        {
            var leads =
                await _context.Leads
                    .AsNoTracking()
                    .Where(l =>
                        l.IsArchived &&
                        !l.IsDeleted &&
                        (
                            // -----------------------------------------
                            // Lead created by current Sales Manager
                            // -----------------------------------------
                            l.CreatedBy == salesManagerId

                            ||

                            // -----------------------------------------
                            // Lead belongs to Sales Manager hierarchy
                            // -----------------------------------------
                            _context.LeadAssignments.Any(a =>
                                a.LeadId == l.LeadId &&
                                !a.IsDeleted &&
                                a.SalesOfficer != null &&
                                a.SalesOfficer.TeamLead != null &&
                                a.SalesOfficer.TeamLead.SalesManagerId ==
                                    salesManagerId)
                        ))
                    .OrderByDescending(l => l.ArchivedAt)
                    .Select(l => new LeadViewModel
                    {
                        LeadId =
                            l.LeadId,

                        LeadCode =
                            l.LeadCode,

                        CompanyName =
                            l.CompanyName,

                        LeadName =
                            l.LeadName,

                        Profession =
                            l.Profession,

                        Email =
                            l.Email,

                        Phone =
                            l.Phone,

                        Address =
                            l.Address,

                        Source =
                            l.Source,

                        Priority =
                            l.Priority,

                        Status =
                            l.Status,

                        Description =
                            l.Description,

                        FollowUpDate =
                            l.FollowUpDate,


                        IsArchived =
                            l.IsArchived,

                        ArchivedAt =
                            l.ArchivedAt
                    })
                    .ToListAsync();

            return leads;
        }





        // =========================================================
        // RESTORE Leads for Admin
        // =========================================================

        public async Task RestoreLeadAsync(long id)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == id &&
                        !l.IsDeleted);

            if (lead == null ||
                !lead.IsArchived)
            {
                return;
            }

            // -----------------------------------------------------
            // Restore Lead
            // -----------------------------------------------------

            lead.IsArchived = false;

            // -----------------------------------------------------
            // Clear Current Archive Metadata
            // -----------------------------------------------------

            lead.ArchivedAt = null;

            lead.ArchivedBy = null;

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // RESTORE Leads for Sales manager
        // =========================================================
        public async Task<bool>
    RestoreLeadForSalesManagerAsync(
        long leadId,
        long salesManagerId)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == leadId &&
                        l.IsArchived &&
                        !l.IsDeleted);

            if (lead == null)
            {
                return false;
            }

            var hasAccess =
                lead.CreatedBy == salesManagerId
                ||
                await _context.LeadAssignments.AnyAsync(a =>
                    a.LeadId == leadId &&
                    !a.IsDeleted &&
                    a.SalesOfficer != null &&
                    a.SalesOfficer.TeamLead != null &&
                    a.SalesOfficer.TeamLead.SalesManagerId ==
                        salesManagerId);

            if (!hasAccess)
            {
                return false;
            }

            lead.IsArchived = false;
            lead.ArchivedAt = null;
            lead.ArchivedBy = null;

            await _context.SaveChangesAsync();

            return true;
        }






        // =========================================================
        // ASSIGN VIEW MODEL For Admin
        // =========================================================

        public async Task<AssignLeadViewModel?>
            GetAssignLeadViewModelAsync(long leadId)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(
                        l => l.LeadId == leadId);

            if (lead == null)
            {
                return null;
            }

            var salesOfficers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.Role != null &&
                        u.Role.RoleName ==
                        "Sales Officer")
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FirstName +
                                " " +
                                (u.LastName ?? "")
                        })
                    .ToListAsync();

            return new AssignLeadViewModel
            {
                LeadId =
                    lead.LeadId,

                LeadCode =
                    lead.LeadCode,

                LeadName =
                    lead.LeadName,

                SalesOfficers =
                    salesOfficers
            };
        }


        // =========================================================
        // ASSIGN LEAD For Admin
        // =========================================================

        public async Task AssignLeadAsync(
            AssignLeadViewModel model,
            long adminId)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(
                        l => l.LeadId == model.LeadId);

            if (lead == null)
            {
                throw new Exception(
                    "Lead not found.");
            }

            // =====================================================
            // Deactivate Existing Assignment
            // =====================================================

            var activeAssignment =
                await _context.LeadAssignments
                    .FirstOrDefaultAsync(a =>
                        a.LeadId == model.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted);

            if (activeAssignment != null)
            {
                activeAssignment.IsActive = false;

                activeAssignment.AssignmentStatus =
                    AssignmentStatus.Reassigned;
            }

            // =====================================================
            // New Assignment
            // =====================================================

            var assignment =
                new LeadAssignment
                {
                    LeadId =
                        model.LeadId,

                    SalesOfficerId =
                        model.SalesOfficerId,

                    AssignedBy =
                        adminId,

                    AssignedAt =
                        DateTime.UtcNow,

                    AssignmentStatus =
                        AssignmentStatus.Pending,

                    IsActive = true
                };

            _context.LeadAssignments
                .Add(assignment);

            lead.Status =
                LeadStatus.Assigned;

            await _context.SaveChangesAsync();

            // =====================================================
            // Sales Officer
            // =====================================================

            var salesOfficer =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId ==
                             model.SalesOfficerId);

            // =====================================================
            // Admin
            // =====================================================

            var admin =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserId ==
                             adminId);

            if (salesOfficer == null)
            {
                throw new Exception(
                    "Sales Officer not found.");
            }

            if (admin == null)
            {
                throw new Exception(
                    "Admin not found.");
            }

            // =====================================================
            // Email
            // =====================================================

            await _emailService
                .SendLeadAssignmentEmailAsync(
                    salesOfficer.Email,
                    $"{salesOfficer.FirstName} {salesOfficer.LastName}",
                    lead.LeadCode,
                    lead.LeadName,
                    $"{admin.FirstName} {admin.LastName}",
                    assignment.AssignedAt);

            // =====================================================
            // Notification
            // =====================================================

            await _notificationService
                .CreateNotificationAsync(
                    salesOfficer.UserId,
                    NotificationType.LeadAssigned,
                    "New Lead Assigned",
                    $"A new lead ({lead.LeadCode}) has been assigned to you.",
                    lead.LeadId,
                    assignment.AssignmentId);
        }




        // =========================================================
        // SALES MANAGER ASSIGN VIEW MODEL
        // =========================================================

        public async Task<AssignLeadViewModel?>
            GetAssignLeadViewModelForSalesManagerAsync(
                long leadId,
                long salesManagerId)
        {
            // =====================================================
            // Find Lead
            // =====================================================

            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == leadId &&
                        !l.IsDeleted &&
                        !l.IsArchived);

            if (lead == null)
            {
                return null;
            }

            // =====================================================
            // Check Lead Access
            //
            // Lead must either:
            // 1. Be created by this Sales Manager
            // OR
            // 2. Be assigned to a Sales Officer under this Manager
            // =====================================================

            var hasAccess =
                lead.CreatedBy == salesManagerId
                ||
                await _context.LeadAssignments
                    .AnyAsync(a =>
                        a.LeadId == leadId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.SalesOfficer != null &&
                        a.SalesOfficer.TeamLead != null &&
                        a.SalesOfficer.TeamLead.SalesManagerId
                            == salesManagerId);

            if (!hasAccess)
            {
                return null;
            }

            // =====================================================
            // Get Only Sales Officers Under This Manager
            // =====================================================

            var salesOfficers =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.TeamLead)
                    .Where(u =>
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey == RoleKeys.SalesOfficer &&
                        u.TeamLead != null &&
                        u.TeamLead.SalesManagerId ==
                            salesManagerId)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName
                        })
                    .ToListAsync();

            // =====================================================
            // Return View Model
            // =====================================================

            return new AssignLeadViewModel
            {
                LeadId =
                    lead.LeadId,

                LeadCode =
                    lead.LeadCode,

                LeadName =
                    lead.LeadName,

                SalesOfficers =
                    salesOfficers
            };
        }





        // =========================================================
        // SALES MANAGER ASSIGN LEAD
        // =========================================================

        public async Task<bool>
            AssignLeadForSalesManagerAsync(
                AssignLeadViewModel model,
                long salesManagerId)
        {
            // =====================================================
            // 1. Find Lead
            // =====================================================

            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(l =>
                        l.LeadId == model.LeadId &&
                        !l.IsDeleted &&
                        !l.IsArchived);

            if (lead == null)
            {
                return false;
            }

            // =====================================================
            // 2. Check Sales Manager Access to Lead
            //
            // Lead must either:
            // - be created by this Sales Manager
            // OR
            // - be assigned to an Officer under this Manager
            // =====================================================

            var hasLeadAccess =
                lead.CreatedBy == salesManagerId
                ||
                await _context.LeadAssignments
                    .AnyAsync(a =>
                        a.LeadId == model.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.SalesOfficer != null &&
                        a.SalesOfficer.TeamLead != null &&
                        a.SalesOfficer.TeamLead.SalesManagerId
                            == salesManagerId);

            if (!hasLeadAccess)
            {
                return false;
            }

            // =====================================================
            // 3. Validate Selected Sales Officer
            // =====================================================

            var salesOfficer =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.TeamLead)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == model.SalesOfficerId &&
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey == RoleKeys.SalesOfficer);

            if (salesOfficer == null)
            {
                return false;
            }

            // =====================================================
            // 4. IMPORTANT:
            //    Officer must belong to current Sales Manager
            // =====================================================

            if (salesOfficer.TeamLead == null ||
                salesOfficer.TeamLead.SalesManagerId !=
                    salesManagerId)
            {
                return false;
            }

            // =====================================================
            // 5. Check Existing Active Assignment
            // =====================================================

            var existingAssignment =
                await _context.LeadAssignments
                    .FirstOrDefaultAsync(a =>
                        a.LeadId == model.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted);

            if (existingAssignment != null)
            {
                return false;
            }

            // =====================================================
            // 6. Create Assignment
            // =====================================================

            var assignment =
                new LeadAssignment
                {
                    LeadId =
                        model.LeadId,

                    SalesOfficerId =
                        model.SalesOfficerId,

                    AssignedBy =
                        salesManagerId,

                    AssignedAt =
                        DateTime.UtcNow,

                    AssignmentStatus =
                        AssignmentStatus.Pending,

                    IsActive =
                        true
                };

            _context.LeadAssignments.Add(
                assignment);

            // =====================================================
            // 7. Update Lead Status
            // =====================================================

            lead.Status =
                LeadStatus.Assigned;

            // =====================================================
            // 8. Save
            // =====================================================

            await _context.SaveChangesAsync();

            return true;
        }






        // =========================================================
        // SALES OFFICER ASSIGNED LEADS
        // =========================================================

        public async Task<List<MyAssignedLeadViewModel>>
            GetAssignedLeadsAsync(long salesOfficerId)
        {
            return await _context.LeadAssignments
                .Include(a => a.Lead)
                .Where(a =>
                    a.SalesOfficerId ==
                        salesOfficerId &&
                    a.IsActive &&
                    !a.IsDeleted)
                .OrderByDescending(a =>
                    a.AssignedAt)
                .Select(a =>
                    new MyAssignedLeadViewModel
                    {
                        AssignmentId =
                            a.AssignmentId,

                        LeadId =
                            a.LeadId,

                        CompanyName =
                            a.Lead!.CompanyName ??
                            string.Empty,

                        LeadName =
                            a.Lead.LeadName,

                        Email =
                            a.Lead.Email ??
                            string.Empty,

                        Phone =
                            a.Lead.Phone,

                        AssignedAt =
                            a.AssignedAt,

                        AcceptedAt =
                            a.AcceptedAt,

                        AssignmentStatus =
                            a.AssignmentStatus
                    })
                .ToListAsync();
        }


        // =========================================================
        // ACCEPT LEAD
        // =========================================================

        public async Task AcceptLeadAsync(
            long assignmentId,
            long salesOfficerId)
        {
            var assignment =
                await _context.LeadAssignments
                    .Include(a => a.Lead)
                    .FirstOrDefaultAsync(a =>
                        a.AssignmentId ==
                            assignmentId &&
                        a.SalesOfficerId ==
                            salesOfficerId &&
                        a.IsActive &&
                        !a.IsDeleted);

            if (assignment == null)
            {
                return;
            }

            if (assignment.AssignmentStatus !=
                AssignmentStatus.Pending)
            {
                return;
            }

            assignment.AssignmentStatus =
                AssignmentStatus.Accepted;

            assignment.AcceptedAt =
                DateTime.UtcNow;

            // =====================================================
            // Acceptance SLA
            // =====================================================

            var acceptanceDeadline =
                assignment.AssignedAt.AddHours(1);

            assignment.AcceptanceSLAMissed =
                assignment.AcceptedAt.Value >
                acceptanceDeadline;

            if (assignment.Lead != null)
            {
                assignment.Lead.Status =
                    LeadStatus.Accepted;
            }

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // CREATE MANUAL LEAD ENTITY
        // =========================================================

        private Lead CreateLeadEntity(
            CreateLeadViewModel model)
        {
            return new Lead
            {
                CompanyName =
                    model.CompanyName,

                LeadName =
                    model.LeadName,

                Profession =
                    model.Profession,

                Email =
                    model.Email,

                Phone =
                    model.Phone,

                Address =
                    model.Address,

                Source =
                    model.Source,

                Priority =
                    model.Priority,

                Status =
                    LeadStatus.New,

                Description =
                    model.Description,

                FollowUpDate =
                    model.FollowUpDate,

                //CreatedBy = 1
            };
        }


        // =========================================================
        // CREATE AUTO LEAD ENTITY
        // =========================================================

        private Lead CreateLeadEntity(
            AutoLeadCreateViewModel model)
        {
            return new Lead
            {
                LeadCode =
                    string.Empty,

                CompanyName =
                    model.CompanyName,

                LeadName =
                    model.LeadName,

                Profession =
                    model.Profession,

                Email =
                    model.Email,

                Phone =
                    model.Phone,

                Address =
                    model.Address,

                Source =
                    model.Source,

                SourceReferenceId =
                    model.SourceReferenceId,

                Priority =
                    model.Priority,

                Status =
                    LeadStatus.New,

                Description =
                    model.Description,

                CreatedBy = 1
            };
        }


        // =========================================================
        // REASSIGN VIEW MODEL For Admin
        // =========================================================

        public async Task<ReassignLeadViewModel?>
            GetReassignLeadViewModelAsync(long leadId)
        {
            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(
                        l => l.LeadId == leadId);

            if (lead == null)
            {
                return null;
            }

            var assignment =
                await _context.LeadAssignments
                    .Include(a =>
                        a.SalesOfficer)
                    .FirstOrDefaultAsync(a =>
                        a.LeadId == leadId &&
                        (a.AssignmentStatus ==
                            AssignmentStatus.Pending ||
                         a.AssignmentStatus ==
                            AssignmentStatus.Accepted ||
                         a.AssignmentStatus ==
                            AssignmentStatus.Declined));

            if (assignment == null)
            {
                return null;
            }

            var salesOfficers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role!.RoleKey ==
                            RoleKeys.SalesOfficer &&
                        u.UserId !=
                            assignment.SalesOfficerId)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName
                        })
                    .ToListAsync();

            return new ReassignLeadViewModel
            {
                LeadId =
                    lead.LeadId,

                AssignmentId =
                    assignment.AssignmentId,

                LeadCode =
                    lead.LeadCode,

                LeadName =
                    lead.LeadName,

                CurrentSalesOfficer =
                    assignment.SalesOfficer != null
                        ? assignment.SalesOfficer.FullName
                        : string.Empty,

                SalesOfficers =
                    salesOfficers
            };
        }


        // =========================================================
        // REASSIGN VIEW MODEL FOR SALES MANAGER
        // =========================================================

        public async Task<ReassignLeadViewModel?>
            GetReassignLeadViewModelForSalesManagerAsync(
                long leadId,
                long salesManagerId)
        {
            var assignment =
                await _context.LeadAssignments
                    .Include(a => a.Lead)
                    .Include(a => a.SalesOfficer)
                    .FirstOrDefaultAsync(a =>
                        a.LeadId == leadId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.Lead != null &&
                        !a.Lead.IsDeleted &&
                        !a.Lead.IsArchived &&
                        a.SalesOfficer != null &&
                        a.SalesOfficer.TeamLead != null &&
                        a.SalesOfficer.TeamLead.SalesManagerId ==
                            salesManagerId);

            if (assignment == null)
            {
                return null;
            }

            var salesOfficers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey == RoleKeys.SalesOfficer &&
                        u.UserId != assignment.SalesOfficerId &&
                        u.TeamLead != null &&
                        u.TeamLead.SalesManagerId ==
                            salesManagerId)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value = u.UserId.ToString(),
                            Text = u.FullName
                        })
                    .ToListAsync();

            return new ReassignLeadViewModel
            {
                LeadId = assignment.Lead!.LeadId,
                AssignmentId = assignment.AssignmentId,
                LeadCode = assignment.Lead.LeadCode,
                LeadName = assignment.Lead.LeadName,
                CurrentSalesOfficer =
                    assignment.SalesOfficer != null
                        ? assignment.SalesOfficer.FullName
                        : string.Empty,
                SalesOfficers = salesOfficers
            };
        }


        // =========================================================
        // REASSIGN LEAD
        // =========================================================

        public async Task ReassignLeadAsync(
            ReassignLeadViewModel model,
            long salesManagerId)
        {
            //var currentAssignment =
            //    await _context.LeadAssignments
            //        .FirstOrDefaultAsync(
            //            a =>
            //                a.AssignmentId ==
            //                model.AssignmentId);
            var currentAssignment =
                   await _context.LeadAssignments
                    .Include(a => a.Lead)
                    .Include(a => a.SalesOfficer)
                    .ThenInclude(o => o!.TeamLead)
                    .FirstOrDefaultAsync(a =>
                        a.AssignmentId == model.AssignmentId &&
                        a.LeadId == model.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted &&
                        a.Lead != null &&
                        !a.Lead.IsDeleted &&
                        !a.Lead.IsArchived);

            if (currentAssignment == null)
            {
                return;
            }

            // =========================================================
            // SALES MANAGER HIERARCHY VALIDATION
            // =========================================================
            if (currentAssignment.SalesOfficer?.TeamLead?.SalesManagerId
                != salesManagerId)
            {
                return;
            }

            // =========================================================
            // NEW SALES OFFICER VALIDATION
            // =========================================================

            var newSalesOfficer =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.TeamLead)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == model.NewSalesOfficerId &&
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey == RoleKeys.SalesOfficer);

            if (newSalesOfficer == null)
            {
                return;
            }

            if (newSalesOfficer.TeamLead == null ||
                newSalesOfficer.TeamLead.SalesManagerId != salesManagerId)
            {
                return;
            }



            currentAssignment.AssignmentStatus =
                AssignmentStatus.Reassigned;

            currentAssignment.IsActive = false;

            var newAssignment =
                new LeadAssignment
                {
                    LeadId =
                        model.LeadId,

                    SalesOfficerId =
                        model.NewSalesOfficerId,

                    AssignedBy =
                        salesManagerId,

                    AssignedAt =
                        DateTime.UtcNow,

                    AcceptedAt = null,

                    AcceptanceSLAMissed = false,

                    AssignmentStatus =
                        AssignmentStatus.Pending,

                    IsActive = true
                };

            _context.LeadAssignments
                .Add(newAssignment);

            var lead =
                await _context.Leads
                    .FirstOrDefaultAsync(
                        l =>
                            l.LeadId ==
                            model.LeadId);

            if (lead != null)
            {
                lead.Status =
                    model.Status;
            }

            await _context.SaveChangesAsync();

            var salesOfficer =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            model.NewSalesOfficerId);

            var salesManager =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            salesManagerId);

            if (salesOfficer != null &&
                salesManager != null &&
                lead != null)
            {
                await _emailService
                    .SendLeadAssignmentEmailAsync(
                        salesOfficer.Email,
                        salesOfficer.FullName,
                        lead.LeadCode,
                        lead.LeadName,
                        salesManager.FullName,
                        newAssignment.AssignedAt);
            }
        }


        // =========================================================
        // UNASSIGNED LEADS For admin
        // =========================================================

        public async Task<List<UnassignedLeadViewModel>>
    GetUnassignedLeadsAsync(
        string? search = null,
        LeadSource? source = null,
        LeadPriority? priority = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
        {
            var query = _context.Leads
                .Where(l =>
                    l.Status == LeadStatus.New &&
                    !l.IsDeleted &&
                    l.IsActive)
                .AsQueryable();

            // =====================================================
            // Search
            // =====================================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(l =>
                    l.LeadCode.Contains(search) ||
                    l.LeadName.Contains(search) ||
                    (l.CompanyName != null &&
                     l.CompanyName.Contains(search)) ||
                    l.Phone.Contains(search));
            }

            // =====================================================
            // Source Filter
            // =====================================================

            if (source.HasValue)
            {
                query = query.Where(l =>
                    l.Source == source.Value);
            }

            // =====================================================
            // Priority Filter
            // =====================================================

            if (priority.HasValue)
            {
                query = query.Where(l =>
                    l.Priority == priority.Value);
            }

            // =====================================================
            // From Date
            // =====================================================

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                query = query.Where(l =>
                    l.CreatedAt >= startDate);
            }

            // =====================================================
            // To Date
            // =====================================================

            if (toDate.HasValue)
            {
                var endDate =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(l =>
                    l.CreatedAt < endDate);
            }

            // =====================================================
            // Projection
            // =====================================================

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l =>
                    new UnassignedLeadViewModel
                    {
                        LeadId =
                            l.LeadId,

                        LeadCode =
                            l.LeadCode,

                        LeadName =
                            l.LeadName,

                        CompanyName =
                            l.CompanyName,

                        Phone =
                            l.Phone,

                        Priority =
                            l.Priority,

                        Source =
                            l.Source,

                        CreatedAt =
                            l.CreatedAt
                    })
                .AsNoTracking()
                .ToListAsync();
        }



        // =========================================================
        // UNASSIGNED LEADS For Sales Manager
        // =========================================================
        public async Task<List<UnassignedLeadViewModel>>
    GetUnassignedLeadsForSalesManagerAsync(
        long salesManagerId,
        string? search = null,
        LeadSource? source = null,
        LeadPriority? priority = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
        {
            var query = _context.Leads
                .Where(l =>
                    l.Status == LeadStatus.New &&
                    !l.IsDeleted &&
                    !l.IsArchived &&

                    // No active assignment
                    !_context.LeadAssignments.Any(a =>
                        a.LeadId == l.LeadId &&
                        a.IsActive &&
                        !a.IsDeleted) &&

                    // Sales Manager ownership
                    (
                        l.CreatedBy == salesManagerId ||
                        l.SalesManagerId == salesManagerId
                    ));

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(l =>
                    l.LeadCode.Contains(search) ||
                    l.LeadName.Contains(search) ||
                    (l.CompanyName != null &&
                     l.CompanyName.Contains(search)) ||
                    l.Phone.Contains(search) ||
                    (l.Email != null &&
                     l.Email.Contains(search)));
            }

            // Source filter
            if (source.HasValue)
            {
                query = query.Where(l =>
                    l.Source == source.Value);
            }

            // Priority filter
            if (priority.HasValue)
            {
                query = query.Where(l =>
                    l.Priority == priority.Value);
            }

            // From date
            if (fromDate.HasValue)
            {
                query = query.Where(l =>
                    l.CreatedAt >= fromDate.Value);
            }

            // To date
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(l =>
                    l.CreatedAt < endDate);
            }

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new UnassignedLeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Address = l.Address,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt
                })
                .AsNoTracking()
                .ToListAsync();
        }




        // =========================================================
        // ACCEPTANCE SLA CALCULATOR
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
                lead.AssignedAt.Value.AddHours(1);

            var now =
                DateTime.UtcNow;

            // -----------------------------------------------------
            // Accepted
            // -----------------------------------------------------

            if (lead.AcceptedAt.HasValue)
            {
                if (lead.AcceptedAt.Value <= deadline)
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

            // -----------------------------------------------------
            // Pending
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Other status
            // -----------------------------------------------------

            lead.AcceptanceSLAStatus =
                lead.AssignmentStatus.Value
                    .ToString();
        }





        //========================================================
        //Helper Method to Normalize Phone Number
        //========================================================
        private static string NormalizePhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            var digits =
                new string(
                    phone.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("00880"))
            {
                digits =
                    digits.Substring(2);
            }

            if (digits.StartsWith("880"))
            {
                digits =
                    digits.Substring(3);
            }

            if (digits.StartsWith("0"))
            {
                digits =
                    digits.Substring(1);
            }

            return digits;
        }
    }
}