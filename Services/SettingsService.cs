using CRMSystem.Data;
using CRMSystem.Models.Entities;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        public SettingsService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<SystemSettings> GetSettingsAsync()
        {
            return await _context.SystemSettings.FirstAsync();
        }
        public async Task UpdateAutoAssignmentAsync(bool enabled)
        {
            var settings = await _context.SystemSettings.FirstAsync();
        
            
                settings.AutoAssignmentEnabled = enabled;
                await _context.SaveChangesAsync();
            
        }
    }
}