using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;

        public LeadService(ApplicationDbContext context)
        {
            _context = context;
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
            var lead = new Lead
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

            // First Save
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            // Generate Lead Code using LeadId
            lead.LeadCode = $"L{lead.LeadId:D6}";

            // Save Again
            await _context.SaveChangesAsync();
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
        }
    }
}