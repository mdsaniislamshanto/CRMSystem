using CRMSystem.Models.Entities;

namespace CRMSystem.Services.Interfaces
{
    public interface ISettingsService
    {
        Task<SystemSettings> GetSettingsAsync();

        Task UpdateAutoAssignmentAsync(bool enabled);
    }
}