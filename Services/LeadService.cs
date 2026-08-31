using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
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
        // API LEADS
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
        // CREATE MANUAL LEAD
        // =========================================================

        public async Task CreateLeadAsync(CreateLeadViewModel model)
        {
            var lead = CreateLeadEntity(model);

            _context.Leads.Add(lead);

            await _context.SaveChangesAsync();

            lead.LeadCode =
                $"L{lead.LeadId:D6}";

            await _context.SaveChangesAsync();

            // =====================================================
            // Current Logged-in User
            // =====================================================

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

            // =====================================================
            // Auto Assignment
            // =====================================================

            await _autoAssignmentService.AutoAssignLeadAsync(
                lead.LeadId,
                userId);
        }


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

            // =====================================================
            // Duplicate Check
            // =====================================================

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

            try
            {
                // =================================================
                // Create Lead
                // =================================================

                var lead =
                    CreateLeadEntity(model);

                _context.Leads.Add(lead);

                await _context.SaveChangesAsync();

                lead.LeadCode =
                    $"L{lead.LeadId:D6}";

                await _context.SaveChangesAsync();

                // =================================================
                // Capture Log
                // =================================================

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

                // =================================================
                // Auto Assign
                // =================================================

                await _autoAssignmentService
                    .AutoAssignLeadAsync(
                        lead.LeadId);

                return lead.LeadId;
            }
            catch (Exception ex)
            {
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
        // EDIT
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
        // UPDATE
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
        // ARCHIVE
        // =========================================================

        public async Task ArchiveLeadAsync(long id)
        {
            var lead =
                await _context.Leads.FindAsync(id);

            if (lead == null ||
                lead.IsArchived)
            {
                return;
            }

            lead.IsArchived = true;

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // ARCHIVED LEADS
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
                    FollowUpDate = l.FollowUpDate
                })
                .ToListAsync();
        }


        // =========================================================
        // RESTORE
        // =========================================================

        public async Task RestoreLeadAsync(long id)
        {
            var lead =
                await _context.Leads.FindAsync(id);

            if (lead == null ||
                !lead.IsArchived)
            {
                return;
            }

            lead.IsArchived = false;

            await _context.SaveChangesAsync();
        }


        // =========================================================
        // ASSIGN VIEW MODEL
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
        // ASSIGN LEAD
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

                CreatedBy = 1
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
        // REASSIGN VIEW MODEL
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
        // REASSIGN LEAD
        // =========================================================

        public async Task ReassignLeadAsync(
            ReassignLeadViewModel model,
            long salesManagerId)
        {
            var currentAssignment =
                await _context.LeadAssignments
                    .FirstOrDefaultAsync(
                        a =>
                            a.AssignmentId ==
                            model.AssignmentId);

            if (currentAssignment == null)
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
        // UNASSIGNED LEADS
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
    }
}