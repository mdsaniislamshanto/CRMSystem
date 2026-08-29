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


        // For getting automatically captured API leads
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


        //for getting all the leads from the database
        public async Task<List<LeadViewModel>> GetAllLeadsAsync()
        {
            return await _context.Leads
                .Where(l => !l.IsArchived)
                .Select(l => new LeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    CompanyName = l.CompanyName,
                    LeadName = l.LeadName,
                    Profession = l.Profession,
                    Email = l.Email,
                    Phone = l.Phone,
                    Source = l.Source,
                    Priority = l.Priority,
                    Status = l.Status,
                    FollowUpDate = l.FollowUpDate
                })
                .ToListAsync();
        }

        //for creating a new lead in the database
        public async Task CreateLeadAsync(CreateLeadViewModel model)
        {
            var lead = CreateLeadEntity(model);

            // First Save
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            // Generate Lead Code using LeadId
            lead.LeadCode = $"L{lead.LeadId:D6}";

            // Save Again
            await _context.SaveChangesAsync();

            // =====================================================
            // Get Current Logged-in User ID from Authentication Claim
            // =====================================================

            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException(
                    "HTTP context is not available.");
            }

            var userIdClaim =
                httpContext.User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdClaim, out long userId))
            {
                throw new InvalidOperationException(
                    "Current logged-in user could not be identified.");
            }

            // =====================================================
            // Auto Assign Lead if enabled
            // =====================================================

            await _autoAssignmentService.AutoAssignLeadAsync(
                lead.LeadId,
                userId);
        }


        // For Auto Lead Capture
        public async Task<long> CreateLeadFromCaptureAsync(
            AutoLeadCreateViewModel model)
        {
            var receivedAt = DateTime.UtcNow;

            // Source reference is required for automatic capture
            if (string.IsNullOrWhiteSpace(model.SourceReferenceId))
            {
                throw new InvalidOperationException(
                    "SourceReferenceId is required for automatic lead capture.");
            }

            // Check whether this external lead has already been captured
            var existingLead = await _context.Leads
                .FirstOrDefaultAsync(l =>
                    l.Source == model.Source &&
                    l.SourceReferenceId == model.SourceReferenceId &&
                    !l.IsDeleted);

            // Duplicate found
            if (existingLead != null)
            {
                var duplicateLog = new LeadCaptureLog
                {
                    LeadId = existingLead.LeadId,

                    CaptureSource = LeadCaptureSource.GoogleForm,

                    CaptureStatus = CaptureStatus.Duplicate,

                    ExternalLeadId = model.SourceReferenceId,

                    PayloadJson = model.PayloadJson,

                    ReceivedAt = receivedAt,

                    ProcessedAt = DateTime.UtcNow,

                    IsActive = true
                };

                await _context.LeadCaptureLogs.AddAsync(duplicateLog);

                await _context.SaveChangesAsync();

                return existingLead.LeadId;
            }

            try
            {
                // Create new Lead entity
                var lead = CreateLeadEntity(model);

                _context.Leads.Add(lead);

                // First save generates LeadId
                await _context.SaveChangesAsync();

                // Generate LeadCode
                lead.LeadCode = $"L{lead.LeadId:D6}";

                await _context.SaveChangesAsync();

                // Create successful capture log
                var captureLog = new LeadCaptureLog
                {
                    LeadId = lead.LeadId,

                    CaptureSource = LeadCaptureSource.GoogleForm,

                    CaptureStatus = CaptureStatus.Success,

                    ExternalLeadId = model.SourceReferenceId,

                    PayloadJson = model.PayloadJson,

                    ReceivedAt = receivedAt,

                    ProcessedAt = DateTime.UtcNow,

                    IsActive = true
                };

                await _context.LeadCaptureLogs.AddAsync(captureLog);

                await _context.SaveChangesAsync();

                // Automatically assign the new Lead
                await _autoAssignmentService
                    .AutoAssignLeadAsync(lead.LeadId);

                return lead.LeadId;
            }
            catch (Exception ex)
            {
                var captureLog = new LeadCaptureLog
                {
                    // Lead creation failed,
                    // therefore there is no LeadId.
                    LeadId = null,

                    CaptureSource = LeadCaptureSource.GoogleForm,

                    CaptureStatus = CaptureStatus.Failed,

                    ExternalLeadId = model.SourceReferenceId,

                    PayloadJson = model.PayloadJson,

                    ErrorMessage = ex.Message,

                    ReceivedAt = receivedAt,

                    ProcessedAt = DateTime.UtcNow,

                    IsActive = true
                };

                await _context.LeadCaptureLogs.AddAsync(captureLog);

                await _context.SaveChangesAsync();

                throw;
            }
        }

        //for viewing the lead details in the view lead page
        public async Task<LeadViewModel?> GetLeadByIdAsync(long id)
        {
            return await _context.Leads
                .Where(l => l.LeadId == id && !l.IsArchived)
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
                .FirstOrDefaultAsync();
        }


        //for editing the lead details in the edit lead page
        public async Task<EditLeadViewModel?> GetLeadForEditAsync(long id)
        {
            return await _context.Leads
                .Where(l => l.LeadId == id && !l.IsArchived)
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


        //for updating the lead details in the database
        public async Task UpdateLeadAsync(EditLeadViewModel model)
        {
            var lead = await _context.Leads.FindAsync(model.LeadId);

            if (lead == null || lead.IsArchived)
            {
                return;
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
        }


        //for archiving the lead in the database
        public async Task ArchiveLeadAsync(long id)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null || lead.IsArchived)
            {
                return;
            }
            lead.IsArchived = true;
            await _context.SaveChangesAsync();
        }

        //for getting all archived leads
        public async Task<List<LeadViewModel>> GetArchivedLeadsAsync()
        {
            return await _context.Leads
                .Where(l => l.IsArchived)
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

        //for restoring the archived lead in the database
        public async Task RestoreLeadAsync(long id)
        {
            var lead = await _context.Leads.FindAsync(id);

            if (lead == null || !lead.IsArchived)
            {
                return;
            }

            lead.IsArchived = false;

            await _context.SaveChangesAsync();
        }


        //for getting the assign lead view model
        public async Task<AssignLeadViewModel?> GetAssignLeadViewModelAsync(long leadId)
        {
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == leadId);

            if (lead == null)
            {
                return null;
            }

            var salesOfficers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName == "Sales Officer")
                .Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FirstName + " " + (u.LastName ?? "")
                })
                .ToListAsync();

            var model = new AssignLeadViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                SalesOfficers = salesOfficers
            };

            return model;


        }
        //for assigning a lead to a sales officer
        public async Task AssignLeadAsync(AssignLeadViewModel model, long adminId)
        {
           


            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == model.LeadId);

            if (lead == null)
            {
                throw new Exception("Lead not found.");
            }

            var activeAssignment = await _context.LeadAssignments
                .FirstOrDefaultAsync(a => a.LeadId == model.LeadId
                                       && a.IsActive);

            if (activeAssignment != null)
            {
                activeAssignment.IsActive = false;
                activeAssignment.AssignmentStatus = AssignmentStatus.Reassigned;
            }

            var assignment = new LeadAssignment
            {
                LeadId = model.LeadId,
                SalesOfficerId = model.SalesOfficerId,
                AssignedBy = adminId,
                AssignedAt = DateTime.UtcNow,
                AssignmentStatus = AssignmentStatus.Pending,
                IsActive = true
            };

            _context.LeadAssignments.Add(assignment);

            lead.Status = LeadStatus.Assigned;

            await _context.SaveChangesAsync();

            // Get Sales Officer Information
            var salesOfficer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == model.SalesOfficerId);

            // Get Admin Information
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == adminId);

        
            if (salesOfficer == null)
            {
                throw new Exception("Sales Officer not found.");
            }

            if (admin == null)
            {
                throw new Exception("Admin not found.");
            }


            // Email Notification for Sales Officer
            await _emailService.SendLeadAssignmentEmailAsync(
                salesOfficer.Email,
                $"{salesOfficer.FirstName} {salesOfficer.LastName}",
                lead.LeadCode,
                lead.LeadName,
                $"{admin.FirstName} {admin.LastName}",
                assignment.AssignedAt);


            // Create In-App Notification for Sales Officer
            await _notificationService.CreateNotificationAsync(
                salesOfficer.UserId,
                NotificationType.LeadAssigned,
                "New Lead Assigned",
                $"A new lead ({lead.LeadCode}) has been assigned to you.",
                lead.LeadId,
                assignment.AssignmentId);
        }

        //for Sales Officer Login করার পরে শুধুমাত্র তার নিজের Assigned Leads দেখতে পারবে
        public async Task<List<MyAssignedLeadViewModel>> GetAssignedLeadsAsync(long salesOfficerId)
        {
            return await _context.LeadAssignments
                .Include(a => a.Lead)
              .Where(a => a.SalesOfficerId == salesOfficerId && a.IsActive && !a.IsDeleted)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new MyAssignedLeadViewModel
                {
                    AssignmentId = a.AssignmentId,
                    LeadId = a.LeadId,
                    CompanyName = a.Lead!.CompanyName ?? string.Empty,
                    LeadName = a.Lead.LeadName,
                    Email = a.Lead.Email ?? string.Empty,
                    Phone = a.Lead.Phone,
                    AssignedAt = a.AssignedAt,
                    AcceptedAt = a.AcceptedAt,
                    AssignmentStatus = a.AssignmentStatus
                })
                .ToListAsync();
        }


        // For sales officer Accept leads
        public async Task AcceptLeadAsync(long assignmentId, long salesOfficerId)
        {
            var assignment = await _context.LeadAssignments
                .Include(a => a.Lead)
                .FirstOrDefaultAsync(a =>
                    a.AssignmentId == assignmentId &&
                    a.SalesOfficerId == salesOfficerId &&
                    a.IsActive &&
                    !a.IsDeleted);

            if (assignment == null)
            {
                return;
            }

            if (assignment.AssignmentStatus != AssignmentStatus.Pending)
            {
                return;
            }

            assignment.AssignmentStatus = AssignmentStatus.Accepted;
            assignment.AcceptedAt = DateTime.UtcNow;

            if (assignment.Lead != null)
            {
                assignment.Lead.Status = LeadStatus.Accepted;
            }

            await _context.SaveChangesAsync();
        }


        // Create Lead Entity from Manual Lead Form
        private Lead CreateLeadEntity(CreateLeadViewModel model)
        {
            return new Lead
            {
                CompanyName = model.CompanyName,
                LeadName = model.LeadName,
                Profession = model.Profession,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Source = model.Source,
               
                Priority = model.Priority,
                Status = LeadStatus.New,
                Description = model.Description,
                FollowUpDate = model.FollowUpDate,
                CreatedBy = 1
            };
        }

        // Create Lead Entity from Auto Lead Capture
        private Lead CreateLeadEntity(
            AutoLeadCreateViewModel model)
        {
            return new Lead
            {
                LeadCode = string.Empty,

                CompanyName = model.CompanyName,

                LeadName = model.LeadName,

                Profession = model.Profession,

                Email = model.Email,

                Phone = model.Phone,

                Address = model.Address,

                Source = model.Source,

                // Google Form Response ID
                SourceReferenceId = model.SourceReferenceId,

                Priority = model.Priority,

                Status = LeadStatus.New,

                Description = model.Description,

                // Temporary
                // Later replace with actual system user
                CreatedBy = 1
            };
        }

        // ==========================
        // Get Reassign Lead ViewModel
        // ==========================
        public async Task<ReassignLeadViewModel?> GetReassignLeadViewModelAsync(long leadId)
        {
            // Get Lead
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == leadId);

            if (lead == null)
            {
                return null;
            }

            // Get Current Assignment
            var assignment = await _context.LeadAssignments
                .Include(a => a.SalesOfficer)
                   .FirstOrDefaultAsync(a =>
            a.LeadId == leadId &&
            (a.AssignmentStatus == AssignmentStatus.Pending ||
            a.AssignmentStatus == AssignmentStatus.Accepted ||
            a.AssignmentStatus == AssignmentStatus.Declined));

            if (assignment == null)
            {
                return null;
            }

            // Get Sales Officers
            var salesOfficers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.IsActive &&
                    !u.IsDeleted &&
                    u.Role!.RoleKey == RoleKeys.SalesOfficer &&
                    u.UserId != assignment.SalesOfficerId)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FullName
                })
                .ToListAsync();

            return new ReassignLeadViewModel
            {
                LeadId = lead.LeadId,
                AssignmentId = assignment.AssignmentId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                CurrentSalesOfficer = assignment.SalesOfficer.FullName,
                SalesOfficers = salesOfficers
            };
        }

        // ==========================
        // Reassign Lead
        // ==========================
        public async Task ReassignLeadAsync(
            ReassignLeadViewModel model,
            long salesManagerId)
        {
            // Get Current Assignment
            var currentAssignment = await _context.LeadAssignments
                .FirstOrDefaultAsync(a => a.AssignmentId == model.AssignmentId);

            if (currentAssignment == null)
            {
                return;
            }

            // Mark Current Assignment as Reassigned
            currentAssignment.AssignmentStatus = AssignmentStatus.Reassigned;

            // Create New Assignment
            var newAssignment = new LeadAssignment
            {
                LeadId = model.LeadId,
                SalesOfficerId = model.NewSalesOfficerId,
                AssignedBy = salesManagerId,
                AssignedAt = DateTime.UtcNow,
                AcceptedAt = null,
                AssignmentStatus = AssignmentStatus.Pending
            };

            _context.LeadAssignments.Add(newAssignment);

            // Update Lead Status
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == model.LeadId);

            if (lead != null)
            {
                lead.Status = model.Status;
            }

            await _context.SaveChangesAsync();

            // Get New Sales Officer
            var salesOfficer = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == model.NewSalesOfficerId);

            // Get Sales Manager
            var salesManager = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == salesManagerId);

            if (salesOfficer != null &&
                salesManager != null &&
                lead != null)
            {
                await _emailService.SendLeadAssignmentEmailAsync(
                    salesOfficer.Email,
                    salesOfficer.FullName,
                    lead.LeadCode,
                    lead.LeadName,
                    salesManager.FullName,
                    newAssignment.AssignedAt);
            }
        }

        // For getting all unassigned leads
        public async Task<List<UnassignedLeadViewModel>> GetUnassignedLeadsAsync()
        {
            return await _context.Leads
                .Where(l =>
                    l.Status == LeadStatus.New &&
                    !l.IsDeleted &&
                    l.IsActive)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new UnassignedLeadViewModel
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    CompanyName = l.CompanyName,
                    Phone = l.Phone,
                    Priority = l.Priority,
                    Source = l.Source,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();
        }

      

    }


}