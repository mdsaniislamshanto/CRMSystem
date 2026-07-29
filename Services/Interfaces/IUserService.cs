using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface IUserService
    {
        //for filtering users based on search term, role, and status
        Task<List<UserViewModel>> GetAllUsersAsync(
            string? searchTerm,
            string? role,
            string? status);

        Task<CreateUserViewModel> GetCreateUserViewModelAsync();

        Task<ServiceResult> CreateUserAsync(CreateUserViewModel model);
        Task<string> GenerateEmployeeCodeAsync();



        //for edit user
        Task<EditUserViewModel?> GetEditUserAsync(long userId);
        Task<ServiceResult> UpdateUserAsync(EditUserViewModel model);

        //for view user details
        Task<UserDetailsViewModel?> GetUserDetailsAsync(long userId);


        //for Activate/Deactivate user
        Task<ServiceResult> ToggleUserStatusAsync(long userId);
    }
}