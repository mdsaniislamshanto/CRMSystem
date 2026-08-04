using CRMSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Data.Seeders
{
    public static class SystemSettingsSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.SystemSettings.AnyAsync())
            {
                return;
            }

            var settings = new SystemSettings
            {
                AutoAssignmentEnabled = false
            };

            context.SystemSettings.Add(settings);

            await context.SaveChangesAsync();
        }
    }
}