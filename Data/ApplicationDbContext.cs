using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CRMSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadAssignment> LeadAssignments { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<LeadCaptureLog> LeadCaptureLogs { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }



        //OnModelCreating method to apply Fluent API configurations
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.LeadCode)
                .IsUnique();
            //modelBuilder.Entity<LeadAssignment>()
            //    .HasOne(la => la.Lead)
            //    .WithMany()
            //    .HasForeignKey(la => la.LeadId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<LeadAssignment>()
            //    .HasOne(la => la.SalesOfficer)
            //    .WithMany()
            //    .HasForeignKey(la => la.SalesOfficerId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<LeadAssignment>()
            //    .HasOne(la => la.AssignedByUser)
            //    .WithMany()
            //    .HasForeignKey(la => la.AssignedBy)
            //    .OnDelete(DeleteBehavior.Restrict);
        }


        //for setting CreatedAt and UpdatedAt timestamps
        private void ApplyAuditInformation() //for setting CreatedAt and UpdatedAt timestamps
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            foreach (EntityEntry<BaseEntity> entry in entries)
            {
                if (entry.State == EntityState.Added) // Set CreatedAt for new entities
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified) // Set UpdatedAt for modified entities
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return await base.SaveChangesAsync(cancellationToken);

        }

        //internal object Include(Func<object, object> value)
        //{
        //    throw new NotImplementedException();
        //}
    }
}