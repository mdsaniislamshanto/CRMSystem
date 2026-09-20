//using CRMSystem.Models.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.ChangeTracking;

//namespace CRMSystem.Data
//{
//    public class ApplicationDbContext : DbContext
//    {
//        public ApplicationDbContext(
//            DbContextOptions<ApplicationDbContext> options)
//            : base(options)
//        {
//        }

//        // =========================================================
//        // DbSets
//        // =========================================================

//        public DbSet<Role> Roles { get; set; }

//        public DbSet<User> Users { get; set; }

//        public DbSet<Lead> Leads { get; set; }

//        public DbSet<Notification> Notifications { get; set; }

//        public DbSet<LeadAssignment> LeadAssignments { get; set; }

//        public DbSet<Feedback> Feedbacks { get; set; }

//        public DbSet<LeadCaptureLog> LeadCaptureLogs { get; set; }

//        public DbSet<SystemSettings> SystemSettings { get; set; }

//        public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }

//        public DbSet<AutoAssignmentRequest> AutoAssignmentRequests { get; set; }

//        public DbSet<GoogleOAuthCredential> GoogleOAuthCredentials { get; set; }

//        public DbSet<SalesTarget> SalesTargets { get; set; }


//        // =========================================================
//        // OnModelCreating
//        // =========================================================

//        protected override void OnModelCreating(
//            ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            // =====================================================
//            // Lead Code Unique Index
//            // =====================================================

//            modelBuilder.Entity<Lead>()
//                .HasIndex(l => l.LeadCode)
//                .IsUnique();

//            // =====================================================
//            // Lead Source + SourceReferenceId Unique Index
//            // =====================================================

//            modelBuilder.Entity<Lead>()
//                .HasIndex(l => new
//                {
//                    l.Source,
//                    l.SourceReferenceId
//                })
//                .IsUnique();


//            // =====================================================
//            // Sales Target → Target User
//            // =====================================================

//            modelBuilder.Entity<SalesTarget>()
//                .HasOne(t => t.User)
//                .WithMany()
//                .HasForeignKey(t => t.UserId)
//                .OnDelete(DeleteBehavior.Restrict);


//            // =====================================================
//            // Sales Target → Created By User
//            // =====================================================

//            modelBuilder.Entity<SalesTarget>()
//                .HasOne(t => t.CreatedByUser)
//                .WithMany()
//                .HasForeignKey(t => t.CreatedBy)
//                .OnDelete(DeleteBehavior.Restrict);


//            // =====================================================
//            // Sales Target Index
//            // =====================================================

//            modelBuilder.Entity<SalesTarget>()
//                .HasIndex(t => new
//                {
//                    t.UserId,
//                    t.PeriodType,
//                    t.StartDate,
//                    t.EndDate
//                });
//        }


//        // =========================================================
//        // Apply Audit Information
//        // =========================================================

//        private void ApplyAuditInformation()
//        {
//            var entries =
//                ChangeTracker.Entries<BaseEntity>();

//            foreach (EntityEntry<BaseEntity> entry in entries)
//            {
//                if (entry.State == EntityState.Added)
//                {
//                    entry.Entity.CreatedAt =
//                        DateTime.UtcNow;
//                }
//                else if (entry.State == EntityState.Modified)
//                {
//                    entry.Entity.UpdatedAt =
//                        DateTime.UtcNow;
//                }
//            }
//        }


//        // =========================================================
//        // SaveChanges
//        // =========================================================

//        public override int SaveChanges()
//        {
//            ApplyAuditInformation();

//            return base.SaveChanges();
//        }


//        // =========================================================
//        // SaveChangesAsync
//        // =========================================================

//        public override async Task<int> SaveChangesAsync(
//            CancellationToken cancellationToken = default)
//        {
//            ApplyAuditInformation();

//            return await base.SaveChangesAsync(
//                cancellationToken);
//        }
//    }
//}














using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CRMSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // DbSets
        // =========================================================

        public DbSet<Role> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Lead> Leads { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<LeadAssignment> LeadAssignments { get; set; }

        public DbSet<Feedback> Feedbacks { get; set; }

        public DbSet<LeadCaptureLog> LeadCaptureLogs { get; set; }

        public DbSet<SystemSettings> SystemSettings { get; set; }

        public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }

        public DbSet<AutoAssignmentRequest> AutoAssignmentRequests { get; set; }

        public DbSet<GoogleOAuthCredential> GoogleOAuthCredentials { get; set; }

        public DbSet<SalesTarget> SalesTargets { get; set; }


        // =========================================================
        // OnModelCreating
        // =========================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // Lead Code Unique Index
            // =====================================================

            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.LeadCode)
                .IsUnique();

            // =====================================================
            // Lead Source + SourceReferenceId Unique Index
            // =====================================================

            modelBuilder.Entity<Lead>()
                .HasIndex(l => new
                {
                    l.Source,
                    l.SourceReferenceId
                })
                .IsUnique();


            // =====================================================
            // User → Sales Manager Relationship
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.SalesManager)
                .WithMany(u => u.SalesOfficers)
                .HasForeignKey(u => u.SalesManagerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // User → Team Lead Relationship
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.TeamLead)
                .WithMany(u => u.TeamLeadOfficers)
                .HasForeignKey(u => u.TeamLeadId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);



            // =====================================================
            // Sales Target → Target User
            // =====================================================

            modelBuilder.Entity<SalesTarget>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // Sales Target → Created By User
            // =====================================================

            modelBuilder.Entity<SalesTarget>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // Sales Target Index
            // =====================================================

            modelBuilder.Entity<SalesTarget>()
                .HasIndex(t => new
                {
                    t.UserId,
                    t.PeriodType,
                    t.StartDate,
                    t.EndDate
                });
        }


        // =========================================================
        // Apply Audit Information
        // =========================================================

        private void ApplyAuditInformation()
        {
            var entries =
                ChangeTracker.Entries<BaseEntity>();

            foreach (EntityEntry<BaseEntity> entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt =
                        DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt =
                        DateTime.UtcNow;
                }
            }
        }


        // =========================================================
        // SaveChanges
        // =========================================================

        public override int SaveChanges()
        {
            ApplyAuditInformation();

            return base.SaveChanges();
        }


        // =========================================================
        // SaveChangesAsync
        // =========================================================

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();

            return await base.SaveChangesAsync(
                cancellationToken);
        }
    }
}