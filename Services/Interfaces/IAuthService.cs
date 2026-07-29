using CRMSystem.Models.DTOs;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginViewModel model);

        void Logout();

        long? GetCurrentUserId();

        string? GetCurrentUserRole();
    }
}