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

        public AutoAssignmentService(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task AutoAssignLeadAsync(
            long leadId,
            long? assignedBy = null)
        {
            // ============================================================
            // 1. LOAD GLOBAL AUTO ASSIGNMENT SETTING
            // ============================================================

            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                return;
            }


            // ============================================================
            // 2. LOAD LEAD
            // ============================================================

            var lead = await _context.Leads
                .FirstOrDefaultAsync(l =>
                    l.LeadId == leadId &&
                    !l.IsDeleted &&
                    !l.IsArchived);

            if (lead == null)
            {
                return;
            }


            // ============================================================
            // 3. SELECT SALES MANAGER
            // ============================================================

            User? selectedManager;

            // If a Sales Manager manually created the lead,
            // preserve that manager as the owner.
            if (assignedBy.HasValue)
            {
                selectedManager = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.UserId == assignedBy.Value &&
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey == RoleKeys.SalesManager);
            }
            else
            {
                // API / system generated lead.
                // Select manager using normalized workload.
                selectedManager =
                    await SelectSalesManagerAsync();
            }

            if (selectedManager == null)
            {
                return;
            }


            // ============================================================
            // 4. SAVE MANAGER QUEUE OWNERSHIP
            // ============================================================

            lead.SalesManagerId = selectedManager.UserId;


            // ============================================================
            // 5. GLOBAL AUTO ASSIGNMENT MASTER SWITCH
            // ============================================================

            if (!settings.AutoAssignmentEnabled)
            {
                // Manager ownership remains.
                // Officer assignment is disabled.

                lead.Status = LeadStatus.New;

                await _context.SaveChangesAsync();

                return;
            }


            // ============================================================
            // 6. MANAGER-SPECIFIC AUTO ASSIGNMENT SWITCH
            // ============================================================

            if (!selectedManager.AutoAssignmentEnabled)
            {
                // This manager has requested / received
                // Auto Assignment OFF.

                lead.Status = LeadStatus.New;

                await _context.SaveChangesAsync();

                return;
            }


            // ============================================================
            // 7. LOAD ELIGIBLE SALES OFFICERS
            // ============================================================

            var salesOfficers =
                await GetEligibleSalesOfficersAsync(
                    selectedManager.UserId);

            if (salesOfficers.Count == 0)
            {
                // Manager has no eligible Sales Officer.
                // Keep the lead in Manager queue.

                lead.Status = LeadStatus.New;

                await _context.SaveChangesAsync();

                return;
            }


            // ============================================================
            // 8. SELECT NEXT SALES OFFICER
            // ============================================================

            var nextSalesOfficer =
                await SelectNextSalesOfficerAsync(
                    salesOfficers);


            // ============================================================
            // 9. DETERMINE ASSIGNED BY USER
            // ============================================================

            var assignedByUserId =
                assignedBy ??
                SystemUsers.SystemAdminUserId;


            // ============================================================
            // 10. CREATE LEAD ASSIGNMENT
            // ============================================================

            var assignment = new LeadAssignment
            {
                LeadId = lead.LeadId,

                SalesOfficerId =
                    nextSalesOfficer.UserId,

                AssignedBy =
                    assignedByUserId,

                AssignedAt =
                    DateTime.UtcNow,

                AssignmentStatus =
                    AssignmentStatus.Pending,

                IsActive = true
            };

            _context.LeadAssignments.Add(assignment);


            // ============================================================
            // 11. UPDATE LEAD STATUS
            // ============================================================

            lead.Status = LeadStatus.Assigned;


            // ============================================================
            // 12. SAVE DATABASE CHANGES
            // ============================================================

            await _context.SaveChangesAsync();


            // ============================================================
            // 13. LOAD ASSIGNER
            // ============================================================

            var assignedByUser =
                await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == assignedByUserId);


            // ============================================================
            // 14. SEND ASSIGNMENT EMAIL
            // ============================================================

            if (assignedByUser != null &&
                !string.IsNullOrWhiteSpace(
                    nextSalesOfficer.Email))
            {
                await _emailService
                    .SendLeadAssignmentEmailAsync(
                        nextSalesOfficer.Email,
                        nextSalesOfficer.FullName,
                        lead.LeadCode,
                        lead.LeadName,
                        assignedByUser.FullName,
                        assignment.AssignedAt);
            }
        }


        // ================================================================
        // SELECT SALES MANAGER
        // ================================================================

        private async Task<User?>
            SelectSalesManagerAsync()
        {
            var managers =
                await _context.Users
                    .Include(u => u.Role)
                    .Where(u =>
                        u.IsActive &&
                        !u.IsDeleted &&
                        u.Role != null &&
                        u.Role.RoleKey ==
                            RoleKeys.SalesManager)
                    .Select(manager => new
                    {
                        Manager = manager,

                        OfficerCount =
                            _context.Users.Count(officer =>
                                officer.IsActive &&
                                !officer.IsDeleted &&
                                officer.Role != null &&
                                officer.Role.RoleKey ==
                                    RoleKeys.SalesOfficer &&
                                officer.TeamLead != null &&
                                officer.TeamLead.SalesManagerId ==
                                    manager.UserId),

                        ActiveLeadCount =
                            _context.LeadAssignments.Count(
                                assignment =>
                                    assignment.IsActive &&
                                    !assignment.IsDeleted &&

                                    assignment.SalesOfficer != null &&

                                    assignment.SalesOfficer.IsActive &&

                                    !assignment.SalesOfficer.IsDeleted &&

                                    assignment.SalesOfficer.Role != null &&

                                    assignment.SalesOfficer.Role.RoleKey ==
                                        RoleKeys.SalesOfficer &&

                                    assignment.SalesOfficer.TeamLead != null &&

                                    assignment.SalesOfficer.TeamLead
                                        .SalesManagerId ==
                                        manager.UserId &&

                                    assignment.Lead != null &&

                                    !assignment.Lead.IsDeleted &&

                                    !assignment.Lead.IsArchived &&

                                    assignment.Lead.Status !=
                                        LeadStatus.Completed)
                    })
                    .ToListAsync();

            if (managers.Count == 0)
            {
                return null;
            }


            // ============================================================
            // NORMALIZED WORKLOAD
            // ============================================================

            var selected =
                managers
                    .OrderBy(x =>
                        x.OfficerCount == 0
                            ? double.MaxValue
                            : (double)x.ActiveLeadCount /
                              x.OfficerCount)
                    .ThenBy(x => x.ActiveLeadCount)
                    .ThenBy(x => x.Manager.UserId)
                    .First();

            return selected.Manager;
        }


        // ================================================================
        // GET ELIGIBLE SALES OFFICERS
        // ================================================================

        private async Task<List<User>>
            GetEligibleSalesOfficersAsync(
                long salesManagerId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.TeamLead)
                .Where(u =>
                    u.IsActive &&
                    !u.IsDeleted &&
                    u.Role != null &&
                    u.Role.RoleKey ==
                        RoleKeys.SalesOfficer &&
                    u.TeamLead != null &&
                    u.TeamLead.SalesManagerId ==
                        salesManagerId)
                .OrderBy(u => u.UserId)
                .ToListAsync();
        }


        // ================================================================
        // SELECT NEXT SALES OFFICER - ROUND ROBIN
        // ================================================================

        private async Task<User>
            SelectNextSalesOfficerAsync(
                List<User> salesOfficers)
        {
            var officerIds =
                salesOfficers
                    .Select(o => o.UserId)
                    .ToList();

            var lastAssignment =
                await _context.LeadAssignments
                    .Where(a =>
                        !a.IsDeleted &&
                        officerIds.Contains(
                            a.SalesOfficerId))
                    .OrderByDescending(a =>
                        a.AssignmentId)
                    .FirstOrDefaultAsync();

            // No previous assignment for these officers.
            if (lastAssignment == null)
            {
                return salesOfficers.First();
            }

            var currentIndex =
                salesOfficers.FindIndex(o =>
                    o.UserId ==
                    lastAssignment.SalesOfficerId);

            // Last assigned officer is no longer
            // in the eligible list.
            if (currentIndex == -1)
            {
                return salesOfficers.First();
            }

            var nextIndex =
                currentIndex + 1;

            if (nextIndex >= salesOfficers.Count)
            {
                nextIndex = 0;
            }

            return salesOfficers[nextIndex];
        }
    }
}



//using CRMSystem.Constants;
//using CRMSystem.Data;
//using CRMSystem.Enums;
//using CRMSystem.Models.Entities;
//using CRMSystem.Services.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace CRMSystem.Services
//{
//    public class AutoAssignmentService : IAutoAssignmentService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IEmailService _emailService;

//        public AutoAssignmentService(
//            ApplicationDbContext context,
//            IEmailService emailService)
//        {
//            _context = context;
//            _emailService = emailService;
//        }

//        public async Task AutoAssignLeadAsync(
//            long leadId,
//            long? assignedBy = null)
//        {
//            // =========================================================
//            // 1. LOAD GLOBAL SETTINGS
//            // =========================================================

//            var settings = await _context.SystemSettings
//                .FirstOrDefaultAsync();

//            if (settings == null)
//            {
//                return;
//            }


//            // =========================================================
//            // 2. LOAD LEAD
//            // =========================================================

//            var lead = await _context.Leads
//                .FirstOrDefaultAsync(l =>
//                    l.LeadId == leadId &&
//                    !l.IsDeleted);

//            if (lead == null)
//            {
//                return;
//            }


//            // =========================================================
//            // 3. CHECK WHETHER THIS LEAD ALREADY HAS
//            //    AN ACTIVE ASSIGNMENT
//            // =========================================================

//            var existingAssignment =
//     await _context.LeadAssignments
//         .AnyAsync(a =>
//             a.LeadId == leadId &&
//             a.IsActive &&
//             !a.IsDeleted);

//            if (existingAssignment)
//            {
//                return;
//            }


//            // =========================================================
//            // 4. DETERMINE SALES MANAGER
//            // =========================================================

//            User? selectedManager = null;

//            // ---------------------------------------------------------
//            // If a Sales Manager created the lead manually,
//            // preserve that manager as the queue owner.
//            // ---------------------------------------------------------

//            if (assignedBy.HasValue)
//            {
//                selectedManager = await _context.Users
//                    .Include(u => u.Role)
//                    .FirstOrDefaultAsync(u =>
//                        u.UserId == assignedBy.Value &&
//                        u.IsActive &&
//                        !u.IsDeleted &&
//                        u.Role != null &&
//                        u.Role.RoleKey == RoleKeys.SalesManager);
//            }


//            // ---------------------------------------------------------
//            // If no specific Sales Manager was identified,
//            // select one using normalized workload.
//            // ---------------------------------------------------------

//            if (selectedManager == null)
//            {
//                selectedManager =
//                    await SelectSalesManagerAsync();
//            }


//            // ---------------------------------------------------------
//            // No active Sales Manager available.
//            // ---------------------------------------------------------

//            if (selectedManager == null)
//            {
//                return;
//            }


//            // =========================================================
//            // 5. SAVE MANAGER QUEUE OWNERSHIP
//            // =========================================================

//            lead.SalesManagerId = selectedManager.UserId;


//            // =========================================================
//            // 6. GLOBAL AUTO ASSIGNMENT CHECK
//            // =========================================================

//            // Global OFF:
//            // Lead remains in selected Manager's queue.
//            // No Sales Officer assignment happens.

//            if (!settings.AutoAssignmentEnabled)
//            {
//                lead.Status = LeadStatus.New;

//                await _context.SaveChangesAsync();

//                return;
//            }


//            // =========================================================
//            // 7. MANAGER-SPECIFIC AUTO ASSIGNMENT CHECK
//            // =========================================================

//            // Manager OFF:
//            // Lead remains in that Manager's queue.
//            // Manager can manually assign it.

//            if (!selectedManager.AutoAssignmentEnabled)
//            {
//                lead.Status = LeadStatus.New;

//                await _context.SaveChangesAsync();

//                return;
//            }


//            // =========================================================
//            // 8. FIND ELIGIBLE SALES OFFICERS
//            // =========================================================

//            var salesOfficers =
//                await GetEligibleSalesOfficersAsync(
//                    selectedManager.UserId);


//            // ---------------------------------------------------------
//            // No eligible officer.
//            // Keep lead in Manager queue.
//            // ---------------------------------------------------------

//            if (!salesOfficers.Any())
//            {
//                lead.Status = LeadStatus.New;

//                await _context.SaveChangesAsync();

//                return;
//            }


//            // =========================================================
//            // 9. SELECT NEXT SALES OFFICER
//            //    ROUND ROBIN
//            // =========================================================

//            var nextSalesOfficer =
//                await SelectNextSalesOfficerAsync(
//                    salesOfficers,
//                    selectedManager.UserId);


//            if (nextSalesOfficer == null)
//            {
//                lead.Status = LeadStatus.New;

//                await _context.SaveChangesAsync();

//                return;
//            }


//            // =========================================================
//            // 10. RESOLVE ASSIGNED BY USER
//            // =========================================================

//            long assignedByUserId =
//                assignedBy ?? SystemUsers.SystemAdminUserId;


//            // =========================================================
//            // 11. CREATE LEAD ASSIGNMENT
//            // =========================================================

//            var assignment = new LeadAssignment
//            {
//                LeadId = lead.LeadId,

//                SalesOfficerId =
//                    nextSalesOfficer.UserId,

//                AssignedBy =
//                    assignedByUserId,

//                AssignedAt =
//                    DateTime.UtcNow,

//                AssignmentStatus =
//                    AssignmentStatus.Pending,

//                IsActive = true
//            };


//            _context.LeadAssignments.Add(assignment);


//            // =========================================================
//            // 12. UPDATE LEAD STATUS
//            // =========================================================

//            lead.Status = LeadStatus.Assigned;


//            // =========================================================
//            // 13. SAVE EVERYTHING
//            // =========================================================

//            await _context.SaveChangesAsync();


//            // =========================================================
//            // 14. LOAD ASSIGNER
//            // =========================================================

//            var assignedByUser =
//                await _context.Users
//                    .FirstOrDefaultAsync(u =>
//                        u.UserId == assignedByUserId);


//            // =========================================================
//            // 15. SEND EMAIL
//            // =========================================================

//            if (assignedByUser != null)
//            {
//                await _emailService.SendLeadAssignmentEmailAsync(
//                    nextSalesOfficer.Email,
//                    nextSalesOfficer.FullName,
//                    lead.LeadCode,
//                    lead.LeadName,
//                    assignedByUser.FullName,
//                    assignment.AssignedAt);
//            }
//        }


//        // =============================================================
//        // SELECT SALES MANAGER
//        // =============================================================

//        private async Task<User?> SelectSalesManagerAsync()
//        {
//            var managers = await _context.Users
//                .Include(u => u.Role)
//                .Where(u =>
//                    u.IsActive &&
//                    !u.IsDeleted &&
//                    u.Role != null &&
//                    u.Role.RoleKey == RoleKeys.SalesManager)
//                .ToListAsync();

//            if (!managers.Any())
//            {
//                return null;
//            }


//            // =========================================================
//            // CALCULATE MANAGER WORKLOAD
//            // =========================================================

//            var managerWorkloads =
//                await _context.Users
//                    .Where(manager =>
//                        manager.IsActive &&
//                        !manager.IsDeleted &&
//                        manager.Role != null &&
//                        manager.Role.RoleKey ==
//                            RoleKeys.SalesManager)

//                    .Select(manager => new
//                    {
//                        ManagerId = manager.UserId,

//                        // -------------------------------------------------
//                        // Eligible active Sales Officers under this manager
//                        // -------------------------------------------------

//                        OfficerCount =
//                            _context.Users.Count(officer =>
//                                officer.IsActive &&
//                                !officer.IsDeleted &&
//                                officer.Role != null &&
//                                officer.Role.RoleKey ==
//                                    RoleKeys.SalesOfficer &&

//                                officer.TeamLead != null &&

//                                officer.TeamLead.SalesManagerId ==
//                                    manager.UserId),


//                        // -------------------------------------------------
//                        // Active assigned leads under this manager
//                        // -------------------------------------------------

//                        ActiveLeadCount =
//                            _context.LeadAssignments.Count(assignment =>
//                                assignment.IsActive &&
//                                !assignment.IsDeleted &&


//                                assignment.SalesOfficer != null &&

//                                assignment.SalesOfficer.IsActive &&

//                                assignment.SalesOfficer.TeamLead != null &&

//                                assignment.SalesOfficer.TeamLead
//                                    .SalesManagerId ==
//                                    manager.UserId &&

//                                assignment.Lead != null &&

//                                !assignment.Lead.IsDeleted &&

//                                !assignment.Lead.IsArchived &&

//                                assignment.Lead.Status !=
//                                    LeadStatus.Completed)
//                    })
//                    .ToListAsync();


//            // =========================================================
//            // NORMALIZED WORKLOAD
//            // =========================================================

//            var selectedManagerId =
//                managerWorkloads
//                    .OrderBy(m =>
//                        m.OfficerCount > 0
//                            ? (double)m.ActiveLeadCount /
//                              m.OfficerCount
//                            : double.MaxValue)

//                    .ThenBy(m => m.ActiveLeadCount)

//                    .ThenBy(m => m.ManagerId)

//                    .Select(m => (long?)m.ManagerId)

//                    .FirstOrDefault();


//            if (!selectedManagerId.HasValue)
//            {
//                return null;
//            }


//            return managers.FirstOrDefault(
//                m => m.UserId == selectedManagerId.Value);
//        }


//        // =============================================================
//        // GET ELIGIBLE SALES OFFICERS
//        // =============================================================

//        private async Task<List<User>>
//            GetEligibleSalesOfficersAsync(
//                long salesManagerId)
//        {
//            return await _context.Users
//                .Include(u => u.Role)
//                .Include(u => u.TeamLead)

//                .Where(u =>
//                    u.IsActive &&
//                    !u.IsDeleted &&

//                    u.Role != null &&
//                    u.Role.RoleKey ==
//                        RoleKeys.SalesOfficer &&

//                    u.TeamLead != null &&

//                    u.TeamLead.IsActive &&

//                    !u.TeamLead.IsDeleted &&

//                    u.TeamLead.SalesManagerId ==
//                        salesManagerId)

//                .OrderBy(u => u.UserId)

//                .ToListAsync();
//        }


//        // =============================================================
//        // SELECT NEXT SALES OFFICER
//        // ROUND ROBIN WITHIN SELECTED MANAGER
//        // =============================================================

//        private async Task<User?>
//            SelectNextSalesOfficerAsync(
//                List<User> salesOfficers,
//                long salesManagerId)
//        {
//            if (!salesOfficers.Any())
//            {
//                return null;
//            }


//            var officerIds =
//                salesOfficers
//                    .Select(o => o.UserId)
//                    .ToList();


//            // ---------------------------------------------------------
//            // Find latest assignment among officers of this Manager.
//            // ---------------------------------------------------------

//            var lastAssignment =
//                await _context.LeadAssignments
//                    .Where(a =>
//                        a.IsActive &&
//                        !a.IsDeleted &&

//                        officerIds.Contains(
//                            a.SalesOfficerId))
//                    .OrderByDescending(
//                        a => a.AssignmentId)
//                    .FirstOrDefaultAsync();


//            // ---------------------------------------------------------
//            // No previous assignment for this Manager.
//            // Start from first officer.
//            // ---------------------------------------------------------

//            if (lastAssignment == null)
//            {
//                return salesOfficers.First();
//            }


//            var currentIndex =
//                salesOfficers.FindIndex(
//                    officer =>
//                        officer.UserId ==
//                        lastAssignment.SalesOfficerId);


//            // ---------------------------------------------------------
//            // Last assigned officer is no longer eligible.
//            // Start from first eligible officer.
//            // ---------------------------------------------------------

//            if (currentIndex == -1)
//            {
//                return salesOfficers.First();
//            }


//            // ---------------------------------------------------------
//            // Move to next officer.
//            // ---------------------------------------------------------

//            var nextIndex =
//                currentIndex + 1;


//            if (nextIndex >= salesOfficers.Count)
//            {
//                nextIndex = 0;
//            }


//            return salesOfficers[nextIndex];
//        }
//    }
//}


//using CRMSystem.Constants;
//using CRMSystem.Data;
//using CRMSystem.Enums;
//using CRMSystem.Models.Entities;
//using CRMSystem.Services.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace CRMSystem.Services
//{
//    public class AutoAssignmentService : IAutoAssignmentService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IEmailService _emailService;

//        public AutoAssignmentService(
//            ApplicationDbContext context,
//            IEmailService emailService)
//        {
//            _context = context;
//            _emailService = emailService;
//        }

//        public async Task AutoAssignLeadAsync(
//            long leadId,
//            long? assignedBy = null)
//        {
//            // Check System Settings
//            var settings = await _context.SystemSettings.FirstAsync();

//            // Auto Assignment OFF
//            if (!settings.AutoAssignmentEnabled)
//            {
//                return;
//            }

//            /////////////////////////////////
//            /// Round Robin Assignment Logic
//            /////////////////////////////////

//            // Get Active Sales Officers
//            var salesOfficers = await _context.Users
//                .Include(u => u.Role)
//                .Where(u =>
//                    u.IsActive &&
//                    !u.IsDeleted &&
//                    u.Role!.RoleKey == RoleKeys.SalesOfficer)
//                .OrderBy(u => u.UserId)
//                .ToListAsync();

//            if (!salesOfficers.Any())
//            {
//                return;
//            }

//            // Get Last Assignment
//            var lastAssignment = await _context.LeadAssignments
//                .OrderByDescending(a => a.AssignmentId)
//                .FirstOrDefaultAsync();

//            User nextSalesOfficer;

//            // First Assignment
//            if (lastAssignment == null)
//            {
//                nextSalesOfficer = salesOfficers.First();
//            }
//            else
//            {
//                var currentIndex = salesOfficers.FindIndex(
//                    s => s.UserId == lastAssignment.SalesOfficerId);

//                if (currentIndex == -1)
//                {
//                    nextSalesOfficer = salesOfficers.First();
//                }
//                else
//                {
//                    currentIndex++;

//                    if (currentIndex >= salesOfficers.Count)
//                    {
//                        currentIndex = 0;
//                    }

//                    nextSalesOfficer = salesOfficers[currentIndex];
//                }
//            }

//            // Resolve Assigned By User ID
//            long assignedByUserId =
//                assignedBy ?? SystemUsers.SystemAdminUserId;

//            // Create Assignment
//            var assignment = new LeadAssignment
//            {
//                LeadId = leadId,
//                SalesOfficerId = nextSalesOfficer.UserId,
//                AssignedBy = assignedByUserId,
//                AssignedAt = DateTime.UtcNow,
//                AssignmentStatus = AssignmentStatus.Pending,
//                IsActive = true
//            };

//            _context.LeadAssignments.Add(assignment);

//            // Update Lead Status
//            var lead = await _context.Leads
//                .FirstOrDefaultAsync(l => l.LeadId == leadId);

//            if (lead != null)
//            {
//                lead.Status = LeadStatus.Assigned;
//            }

//            await _context.SaveChangesAsync();

//            // Get Assigned By User
//            var assignedByUser = await _context.Users
//                .FirstOrDefaultAsync(u =>
//                    u.UserId == assignedByUserId);

//            if (assignedByUser != null && lead != null)
//            {
//                await _emailService.SendLeadAssignmentEmailAsync(
//                    nextSalesOfficer.Email,
//                    nextSalesOfficer.FullName,
//                    lead.LeadCode,
//                    lead.LeadName,
//                    assignedByUser.FullName,
//                    assignment.AssignedAt);
//            }
//        }
//    }
//}