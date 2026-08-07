using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class AutoAssignmentService : IAutoAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; 

        public AutoAssignmentService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task AutoAssignLeadAsync(long leadId, long? assignedBy = null)
        {
            // Check System Settings
            var settings = await _context.SystemSettings.FirstAsync();

            // Auto Assignment OFF
            if (!settings.AutoAssignmentEnabled)
            {
                return;
            }

            /////////////////////////////////
            ///Round Robin Assignment Logic//
            ///////////////////////////////// 


            // Get Active Sales Officers
            var salesOfficers = await _context.Users
                .Include(u => u.Role)
                .Where(u =>
                    u.IsActive &&
                    !u.IsDeleted &&
                    u.Role!.RoleKey == RoleKeys.SalesOfficer)
                .OrderBy(u => u.UserId)
                .ToListAsync();

            if (!salesOfficers.Any())
            {
                return;
            }

            // Get Last Assignment
            var lastAssignment = await _context.LeadAssignments
                .OrderByDescending(a => a.AssignmentId)
                .FirstOrDefaultAsync();

            User nextSalesOfficer;

            // First Assignment
            if (lastAssignment == null)
            {
                nextSalesOfficer = salesOfficers.First();
            }
            else
            {
                var currentIndex = salesOfficers.FindIndex(
                    s => s.UserId == lastAssignment.SalesOfficerId);

                if (currentIndex == -1)
                {
                    nextSalesOfficer = salesOfficers.First();
                }
                else
                {
                    currentIndex++;

                    if (currentIndex >= salesOfficers.Count)
                    {
                        currentIndex = 0;
                    }

                    nextSalesOfficer = salesOfficers[currentIndex];
                }
            }

            // Create Assignment
            var assignment = new LeadAssignment
            {
                LeadId = leadId,
                SalesOfficerId = nextSalesOfficer.UserId,
                AssignedBy = assignedBy ?? SystemUsers.SystemAdminUserId,
                AssignedAt = DateTime.UtcNow,
                AssignmentStatus = AssignmentStatus.Pending,
                IsActive = true
            };

            _context.LeadAssignments.Add(assignment);

            // Update Lead Status
            var lead = await _context.Leads
                .FirstOrDefaultAsync(l => l.LeadId == leadId);

            if (lead != null)
            {
                lead.Status = LeadStatus.Assigned;
            }

            await _context.SaveChangesAsync();


            // Get Assigned By User
            var assignedByUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == (assignedBy ?? SystemUsers.SystemAdminUserId));

            if (assignedByUser != null)
            {
                await _emailService.SendLeadAssignmentEmailAsync(
                    nextSalesOfficer.Email,
                    nextSalesOfficer.FullName,
                    lead!.LeadCode,
                    lead.LeadName,
                    assignedByUser.FullName,
                    assignment.AssignedAt);
            }
        }


    }
    
}