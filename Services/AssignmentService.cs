using CRMSystem.Data;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using CRMSystem.Constants;
using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CRMSystem.Models.DTOs;
using CRMSystem.Enums;

namespace CRMSystem.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AssignmentService(
                ApplicationDbContext context,
                IAuthService authService,
                IEmailService emailService)
        {
            _context = context;
            _authService = authService;
            _emailService = emailService;
        }
        public async Task<ServiceResult> AssignLeadAsync(AssignmentViewModel model)
        {
            // ==========================
            // Check Lead Exists
            // ==========================
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == model.LeadId);

            if (lead == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Lead not found."
                };
            }

            // ==========================
            // Check Sales Officer Exists
            // ==========================
            var salesOfficer = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == model.SalesOfficerId);

            if (salesOfficer == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Sales Officer not found."
                };
            }

            // ==========================
            // Check Role
            // ==========================
            if (salesOfficer.Role?.RoleKey != RoleKeys.SalesOfficer)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Selected user is not a Sales Officer."
                };
            }


            // ==========================
            // Check Existing Active Assignment
            // ==========================
            var hasActiveAssignment = await _context.LeadAssignments
                .AnyAsync(a =>
                    a.LeadId == lead.LeadId &&
                    (a.AssignmentStatus == AssignmentStatus.Pending ||
                     a.AssignmentStatus == AssignmentStatus.Accepted));

            if (hasActiveAssignment)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "This lead already has an active assignment."
                };
            }


            // ==========================
            // Get Current Logged-in User
            // ==========================
            var currentUserId = _authService.GetCurrentUserId();

            if (currentUserId == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "User session expired."
                };
            }

            // ==========================
            // Create Assignment
            // ==========================
            var assignment = new LeadAssignment
            {
                LeadId = lead.LeadId,
                SalesOfficerId = salesOfficer.UserId,
                AssignedBy = currentUserId.Value,
                AssignedAt = DateTime.UtcNow,
                AssignmentStatus = AssignmentStatus.Pending
            };

            // ==========================
            // Add Assignment
            // ==========================
            await _context.LeadAssignments.AddAsync(assignment);

            // ==========================
            // Update Lead Status
            // ==========================
            lead.Status = LeadStatus.Assigned;

            // ==========================
            // Save Changes
            // ==========================
            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message = "Lead assigned successfully."
            };
        }
    }
}