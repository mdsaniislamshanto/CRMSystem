using CRMSystem.Constants;
using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;


namespace CRMSystem.Data.Seeders
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await SeedRolesAsync(context);
            await SeedAdminUserAsync(context);

            // Seed default system settings
            await SystemSettingsSeeder.SeedAsync(context);
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            var roles = new List<Role>
    {
        new Role
        {
            RoleId = 1,
            RoleKey = RoleKeys.Admin,
            RoleName = "Administrator",
            Description = "System Administrator",
            DisplayOrder = 1
        },

        new Role
        {
            RoleId = 2,
            RoleKey = RoleKeys.SalesOfficer,
            RoleName = "Sales Officer",
            Description = "Sales Officer",
            DisplayOrder = 2
        },

        new Role
        {
            RoleId = 3,
            RoleKey = RoleKeys.Account,
            RoleName = "Account Department",
            Description = "Account Department",
            DisplayOrder = 3
        },

        new Role
        {
            RoleId = 4,
            RoleKey = RoleKeys.SalesManager,
            RoleName = "Sales Manager",
            Description = "Sales Manager",
            DisplayOrder = 4
        },

        new Role
        {
            RoleId = 5,
            RoleKey = RoleKeys.HR,
            RoleName = "HR Department",
            Description = "Human Resource Department",
            DisplayOrder = 5
        }
    };

            foreach (var role in roles)
            {
                bool exists = await context.Roles
                    .AnyAsync(r => r.RoleKey == role.RoleKey);

                if (!exists)
                {
                    await context.Roles.AddAsync(role);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(ApplicationDbContext context)
        {
            // Admin user already exists?
            if (await context.Users.AnyAsync(u => u.Email == "admin@crm.com"))
                return;

            // Get Admin Role
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleKey == "ADMIN");

            if (adminRole == null)
                return;

            var adminUser = new User
            {
                RoleId = adminRole.RoleId,
                EmployeeCode = "EMP0001",
                FirstName = "System",
                LastName = "Admin",
                Email = "admin@crm.com",
                PhoneNumber = "01700000000",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                ProfileImage = null,
                IsEmailVerified = true,
                LastLoginAt = null,
                LastPasswordChangedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
    }
}