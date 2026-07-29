using CRMSystem.Data;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace CRMSystem.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public UserService(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }


        //for get all users and search users by employee code, full name, or email
        public async Task<List<UserViewModel>> GetAllUsersAsync(
                string? searchTerm,
                string? role,
                string? status)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();


            // Filter by name if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(u =>
                    u.EmployeeCode.Contains(searchTerm) ||

                    u.FirstName.Contains(searchTerm) ||

                    (u.LastName != null &&
                     u.LastName.Contains(searchTerm)) ||

                    ((u.FirstName + " " + (u.LastName ?? ""))
                        .Contains(searchTerm)) ||

                    u.Email.Contains(searchTerm));
            }


            // Filter by role if provided
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role != null && u.Role.RoleName == role);
            }


            // Filter by Status
            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isActive = status == "Active";

                query = query.Where(u => u.IsActive == isActive);
            }

            return await query
                .OrderBy(u => u.FirstName)
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,
                    EmployeeCode = u.EmployeeCode,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    RoleName = u.Role != null ? u.Role.RoleName : "",
                    IsEmailVerified = u.IsEmailVerified,
                    IsActive = u.IsActive,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
        }


        // This method is a placeholder for creating a new user. In a real implementation, you would add logic to create the user in the database.
        public async Task<CreateUserViewModel> GetCreateUserViewModelAsync()
        {
            var model = new CreateUserViewModel();
            model.EmployeeCode = await GenerateEmployeeCodeAsync();

            model.Roles = await _context.Roles
                .Where(r => r.RoleKey != "ADMIN")
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new SelectListItem
                {
                    Value = r.RoleId.ToString(),
                    Text = r.RoleName
                })
                .ToListAsync();

            return model;
        }



        //  for creat a new user
        public async Task<ServiceResult> CreateUserAsync(CreateUserViewModel model)
        {
            // Check duplicate Employee Code
            if (await _context.Users.AnyAsync(u => u.EmployeeCode == model.EmployeeCode))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Employee Code already exists."
                };
            }

            // Check duplicate Email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Email already exists."
                };
            }

            var user = new User
            {
                RoleId = model.RoleId,
                EmployeeCode = await GenerateEmployeeCodeAsync(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsEmailVerified = false,
                IsActive = true,
                LastPasswordChangedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "User created successfully."
            };
        }

        //for generating employee code
        public async Task<string> GenerateEmployeeCodeAsync()
        {
            var lastEmployee = await _context.Users
                .OrderByDescending(u => u.UserId)
                .FirstOrDefaultAsync();

            if (lastEmployee == null)
            {
                return "EMP000001";
            }

            var lastNumber = int.Parse(lastEmployee.EmployeeCode.Substring(3));

            return $"EMP{(lastNumber + 1):D6}";
        }


        //for edit user
        public async Task<EditUserViewModel?> GetEditUserAsync(long userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                return null;
            }

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                EmployeeCode = user.EmployeeCode,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.RoleId
            };

            model.Roles = await _context.Roles
                .Where(r => r.RoleKey != "ADMIN")
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new SelectListItem
                {
                    Value = r.RoleId.ToString(),
                    Text = r.RoleName
                })
                .ToListAsync();

            return model;
        }

        //for updating user
        public async Task<ServiceResult> UpdateUserAsync(EditUserViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == model.UserId && u.IsActive);

            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            var phoneExists = await _context.Users
                .AnyAsync(u => u.PhoneNumber == model.PhoneNumber
                            && u.UserId != model.UserId);

            if (phoneExists)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Phone number already exists."
                };
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.RoleId = model.RoleId;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "User updated successfully."
            };
        }

        //for view user details
        public async Task<UserDetailsViewModel?> GetUserDetailsAsync(long userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                return null;
            }

            return new UserDetailsViewModel
            {
                UserId = user.UserId,
                EmployeeCode = user.EmployeeCode,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role.RoleName,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                LastPasswordChangedAt = user.LastPasswordChangedAt
            };
        }

        //for Activate/Deactivate user
        public async Task<ServiceResult> ToggleUserStatusAsync(long userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);






            if (user == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "User not found."
                };
            }


            // Get current logged-in user id from session
            var currentUserId = _authService.GetCurrentUserId();

            if (currentUserId == user.UserId)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "You cannot deactivate your own account."
                };
            }

            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = user.IsActive
                    ? "User activated successfully."
                    : "User deactivated successfully."
            };
        }
    }
}