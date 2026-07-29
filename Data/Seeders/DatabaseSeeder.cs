using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;


namespace CRMSystem.Data.Seeders
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await SeedRolesAsync(context);
            await SeedAdminUserAsync(context);
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            if (await context.Roles.AnyAsync())
                return;

            var roles = new List<Role>
            {
                new Role
                {
                    RoleId = 1,
                    RoleKey = "ADMIN",
                    RoleName = "Administrator",
                    Description = "System Administrator",
                    DisplayOrder = 1
                },

                new Role
                {
                    RoleId = 2,
                    RoleKey = "SALES_OFFICER",
                    RoleName = "Sales Officer",
                    Description = "Sales Officer",
                    DisplayOrder = 2
                },

                new Role
                {
                    RoleId = 3,
                    RoleKey = "ACCOUNT",
                    RoleName = "Account",
                    Description = "Account Department",
                    DisplayOrder = 3
                }
            };

            await context.Roles.AddRangeAsync(roles);
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