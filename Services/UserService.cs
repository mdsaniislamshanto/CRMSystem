//using CRMSystem.Constants;
//using CRMSystem.Data;
//using CRMSystem.Models.DTOs;
//using CRMSystem.Models.Entities;
//using CRMSystem.Models.ViewModels;
//using CRMSystem.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;

//namespace CRMSystem.Services
//{
//    public class UserService : IUserService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IAuthService _authService;

//        public UserService(
//            ApplicationDbContext context,
//            IAuthService authService)
//        {
//            _context = context;
//            _authService = authService;
//        }

//        // =========================================================
//        // Get All Users
//        // =========================================================

//        public async Task<List<UserViewModel>> GetAllUsersAsync(
//            string? searchTerm,
//            string? role,
//            string? status)
//        {
//            var query = _context.Users
//                .Include(u => u.Role)
//                .AsQueryable();

//            // Filter by name / employee code / email
//            if (!string.IsNullOrWhiteSpace(searchTerm))
//            {
//                searchTerm = searchTerm.Trim();

//                query = query.Where(u =>
//                    u.EmployeeCode.Contains(searchTerm) ||
//                    u.FirstName.Contains(searchTerm) ||
//                    (u.LastName != null &&
//                     u.LastName.Contains(searchTerm)) ||
//                    ((u.FirstName + " " +
//                      (u.LastName ?? ""))
//                        .Contains(searchTerm)) ||
//                    u.Email.Contains(searchTerm));
//            }

//            // Filter by role
//            if (!string.IsNullOrWhiteSpace(role))
//            {
//                query = query.Where(u =>
//                    u.Role != null &&
//                    u.Role.RoleName == role);
//            }

//            // Filter by status
//            if (!string.IsNullOrWhiteSpace(status))
//            {
//                bool isActive =
//                    status == "Active";

//                query = query.Where(u =>
//                    u.IsActive == isActive);
//            }

//            return await query
//                .OrderBy(u => u.FirstName)
//                .Select(u => new UserViewModel
//                {
//                    UserId = u.UserId,
//                    EmployeeCode = u.EmployeeCode,
//                    FullName = u.FullName,
//                    Email = u.Email,
//                    PhoneNumber = u.PhoneNumber,
//                    RoleName =
//                        u.Role != null
//                            ? u.Role.RoleName
//                            : "",
//                    IsEmailVerified =
//                        u.IsEmailVerified,
//                    IsActive =
//                        u.IsActive,
//                    LastLoginAt =
//                        u.LastLoginAt
//                })
//                .ToListAsync();
//        }

//        // =========================================================
//        // Get Create User ViewModel
//        // =========================================================

//        public async Task<CreateUserViewModel>
//            GetCreateUserViewModelAsync()
//        {
//            var model =
//                new CreateUserViewModel();

//            model.EmployeeCode =
//                await GenerateEmployeeCodeAsync();

//            // -----------------------------------------------------
//            // Load Roles
//            // -----------------------------------------------------

//            model.Roles =
//                await _context.Roles
//                    .Where(r =>
//                        r.RoleKey != RoleKeys.Admin)
//                    .OrderBy(r =>
//                        r.DisplayOrder)
//                    .Select(r =>
//                        new SelectListItem
//                        {
//                            Value =
//                                r.RoleId.ToString(),

//                            Text =
//                                r.RoleName
//                        })
//                    .ToListAsync();

//            // -----------------------------------------------------
//            // Load Active Sales Managers
//            // -----------------------------------------------------

//            model.SalesManagers =
//                await _context.Users
//                    .Where(u =>
//                        u.Role != null &&
//                        u.Role.RoleKey ==
//                        RoleKeys.SalesManager &&
//                        u.IsActive)
//                    .OrderBy(u =>
//                        u.FirstName)
//                    .ThenBy(u =>
//                        u.LastName)
//                    .Select(u =>
//                        new SelectListItem
//                        {
//                            Value =
//                                u.UserId.ToString(),

//                            Text =
//                                u.FullName
//                        })
//                    .ToListAsync();

//            return model;
//        }

//        // =========================================================
//        // Create User
//        // =========================================================

//        public async Task<ServiceResult>
//            CreateUserAsync(
//                CreateUserViewModel model)
//        {
//            // -----------------------------------------------------
//            // Check duplicate Employee Code
//            // -----------------------------------------------------

//            if (await _context.Users.AnyAsync(
//                    u =>
//                        u.EmployeeCode ==
//                        model.EmployeeCode))
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "Employee Code already exists."
//                };
//            }

//            // -----------------------------------------------------
//            // Check duplicate Email
//            // -----------------------------------------------------

//            if (await _context.Users.AnyAsync(
//                    u =>
//                        u.Email == model.Email))
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "Email already exists."
//                };
//            }

//            // -----------------------------------------------------
//            // Validate selected Role
//            // -----------------------------------------------------

//            var selectedRole =
//                await _context.Roles
//                    .FirstOrDefaultAsync(r =>
//                        r.RoleId == model.RoleId);

//            if (selectedRole == null)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "Selected role was not found."
//                };
//            }

//            // -----------------------------------------------------
//            // Sales Officer / Team Lead must have Sales Manager
//            // -----------------------------------------------------

//            bool requiresSalesManager =
//                selectedRole.RoleKey ==
//                    RoleKeys.SalesOfficer ||

//                selectedRole.RoleKey ==
//                    RoleKeys.TeamLead;

//            if (requiresSalesManager)
//            {
//                if (!model.SalesManagerId.HasValue)
//                {
//                    return new ServiceResult
//                    {
//                        IsSuccess = false,
//                        Message =
//                            $"Please select a Sales Manager for the {selectedRole.RoleName}."
//                    };
//                }

//                var managerExists =
//                    await _context.Users.AnyAsync(u =>
//                        u.UserId ==
//                        model.SalesManagerId.Value &&

//                        u.Role != null &&

//                        u.Role.RoleKey ==
//                        RoleKeys.SalesManager &&

//                        u.IsActive);

//                if (!managerExists)
//                {
//                    return new ServiceResult
//                    {
//                        IsSuccess = false,
//                        Message =
//                            "Selected Sales Manager is invalid or inactive."
//                    };
//                }
//            }
//            else
//            {
//                // -------------------------------------------------
//                // Other roles do not need a Sales Manager
//                // -------------------------------------------------

//                model.SalesManagerId = null;
//            }

//            // -----------------------------------------------------
//            // Create User
//            // -----------------------------------------------------

//            var user = new User
//            {
//                RoleId =
//                    model.RoleId,

//                EmployeeCode =
//                    await GenerateEmployeeCodeAsync(),

//                FirstName =
//                    model.FirstName,

//                LastName =
//                    model.LastName,

//                Email =
//                    model.Email,

//                PhoneNumber =
//                    model.PhoneNumber,

//                PasswordHash =
//                    BCrypt.Net.BCrypt
//                        .HashPassword(
//                            model.Password),

//                IsEmailVerified =
//                    false,

//                IsActive =
//                    true,

//                LastPasswordChangedAt =
//                    DateTime.UtcNow,

//                SalesManagerId =
//                    model.SalesManagerId
//            };

//            _context.Users.Add(user);

//            await _context.SaveChangesAsync();

//            return new ServiceResult
//            {
//                IsSuccess = true,
//                Message =
//                    "User created successfully."
//            };
//        }

//        // =========================================================
//        // Generate Employee Code
//        // =========================================================

//        public async Task<string>
//            GenerateEmployeeCodeAsync()
//        {
//            var lastEmployee =
//                await _context.Users
//                    .OrderByDescending(u =>
//                        u.UserId)
//                    .FirstOrDefaultAsync();

//            if (lastEmployee == null)
//            {
//                return "EMP000001";
//            }

//            var lastNumber =
//                int.Parse(
//                    lastEmployee.EmployeeCode
//                        .Substring(3));

//            return
//                $"EMP{(lastNumber + 1):D6}";
//        }

//        // =========================================================
//        // Get Edit User
//        // =========================================================

//        public async Task<EditUserViewModel?>
//            GetEditUserAsync(
//                long userId)
//        {
//            var user =
//                await _context.Users
//                    .Include(u => u.Role)
//                    .FirstOrDefaultAsync(
//                        u =>
//                            u.UserId == userId &&
//                            u.IsActive);

//            if (user == null)
//            {
//                return null;
//            }

//            var model =
//                new EditUserViewModel
//                {
//                    UserId =
//                        user.UserId,

//                    EmployeeCode =
//                        user.EmployeeCode,

//                    Email =
//                        user.Email,

//                    FirstName =
//                        user.FirstName,

//                    LastName =
//                        user.LastName,

//                    PhoneNumber =
//                        user.PhoneNumber,

//                    RoleId =
//                        user.RoleId,

//                    SalesManagerId =
//                        user.SalesManagerId
//                };

//            // -----------------------------------------------------
//            // Load Roles
//            // -----------------------------------------------------

//            model.Roles =
//                await _context.Roles
//                    .Where(r =>
//                        r.RoleKey != RoleKeys.Admin)
//                    .OrderBy(r =>
//                        r.DisplayOrder)
//                    .Select(r =>
//                        new SelectListItem
//                        {
//                            Value =
//                                r.RoleId.ToString(),

//                            Text =
//                                r.RoleName
//                        })
//                    .ToListAsync();

//            // -----------------------------------------------------
//            // Load Active Sales Managers
//            // -----------------------------------------------------

//            model.SalesManagers =
//                await _context.Users
//                    .Where(u =>
//                        u.Role != null &&
//                        u.Role.RoleKey ==
//                        RoleKeys.SalesManager &&
//                        u.IsActive)
//                    .OrderBy(u =>
//                        u.FirstName)
//                    .ThenBy(u =>
//                        u.LastName)
//                    .Select(u =>
//                        new SelectListItem
//                        {
//                            Value =
//                                u.UserId.ToString(),

//                            Text =
//                                u.FullName
//                        })
//                    .ToListAsync();

//            return model;
//        }

//        // =========================================================
//        // Update User
//        // =========================================================

//        public async Task<ServiceResult>
//            UpdateUserAsync(
//                EditUserViewModel model)
//        {
//            var user =
//                await _context.Users
//                    .FirstOrDefaultAsync(
//                        u =>
//                            u.UserId ==
//                            model.UserId &&
//                            u.IsActive);

//            if (user == null)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "User not found."
//                };
//            }

//            // -----------------------------------------------------
//            // Check duplicate Phone Number
//            // -----------------------------------------------------

//            var phoneExists =
//                await _context.Users.AnyAsync(
//                    u =>
//                        u.PhoneNumber ==
//                        model.PhoneNumber &&

//                        u.UserId !=
//                        model.UserId);

//            if (phoneExists)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "Phone number already exists."
//                };
//            }

//            // -----------------------------------------------------
//            // Validate selected Role
//            // -----------------------------------------------------

//            var selectedRole =
//                await _context.Roles
//                    .FirstOrDefaultAsync(
//                        r =>
//                            r.RoleId ==
//                            model.RoleId);

//            if (selectedRole == null)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "Selected role was not found."
//                };
//            }

//            // -----------------------------------------------------
//            // Sales Officer / Team Lead must have Sales Manager
//            // -----------------------------------------------------

//            bool requiresSalesManager =
//                selectedRole.RoleKey ==
//                    RoleKeys.SalesOfficer ||

//                selectedRole.RoleKey ==
//                    RoleKeys.TeamLead;

//            if (requiresSalesManager)
//            {
//                if (!model.SalesManagerId.HasValue)
//                {
//                    return new ServiceResult
//                    {
//                        IsSuccess = false,
//                        Message =
//                            $"Please select a Sales Manager for the {selectedRole.RoleName}."
//                    };
//                }

//                var managerExists =
//                    await _context.Users.AnyAsync(
//                        u =>
//                            u.UserId ==
//                            model.SalesManagerId.Value &&

//                            u.Role != null &&

//                            u.Role.RoleKey ==
//                            RoleKeys.SalesManager &&

//                            u.IsActive);

//                if (!managerExists)
//                {
//                    return new ServiceResult
//                    {
//                        IsSuccess = false,
//                        Message =
//                            "Selected Sales Manager is invalid or inactive."
//                    };
//                }
//            }
//            else
//            {
//                model.SalesManagerId = null;
//            }

//            // -----------------------------------------------------
//            // Update User
//            // -----------------------------------------------------

//            user.FirstName =
//                model.FirstName;

//            user.LastName =
//                model.LastName;

//            user.PhoneNumber =
//                model.PhoneNumber;

//            user.RoleId =
//                model.RoleId;

//            user.SalesManagerId =
//                model.SalesManagerId;

//            await _context.SaveChangesAsync();

//            return new ServiceResult
//            {
//                IsSuccess = true,
//                Message =
//                    "User updated successfully."
//            };
//        }

//        // =========================================================
//        // Get User Details
//        // =========================================================

//        public async Task<UserDetailsViewModel?>
//            GetUserDetailsAsync(
//                long userId)
//        {
//            var user =
//                await _context.Users
//                    .Include(u => u.Role)
//                    .Include(u => u.SalesManager)
//                    .FirstOrDefaultAsync(
//                        u =>
//                            u.UserId ==
//                            userId &&
//                            u.IsActive);

//            if (user == null)
//            {
//                return null;
//            }

//            return new UserDetailsViewModel
//            {
//                UserId =
//                    user.UserId,

//                EmployeeCode =
//                    user.EmployeeCode,

//                FullName =
//                    user.FullName,

//                Email =
//                    user.Email,

//                PhoneNumber =
//                    user.PhoneNumber,

//                RoleName =
//                    user.Role!.RoleName,

//                IsEmailVerified =
//                    user.IsEmailVerified,

//                IsActive =
//                    user.IsActive,

//                LastLoginAt =
//                    user.LastLoginAt,

//                LastPasswordChangedAt =
//                    user.LastPasswordChangedAt,

//                // =====================================================
//                // Sales Hierarchy
//                // =====================================================

//                SalesManagerId =
//                    user.SalesManagerId,

//                SalesManagerName =
//                    user.SalesManager != null
//                        ? user.SalesManager.FullName
//                        : null
//            };
//        }

//        // =========================================================
//        // Activate / Deactivate User
//        // =========================================================

//        public async Task<ServiceResult>
//            ToggleUserStatusAsync(
//                long userId)
//        {
//            var user =
//                await _context.Users
//                    .FirstOrDefaultAsync(
//                        u =>
//                            u.UserId ==
//                            userId);

//            if (user == null)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "User not found."
//                };
//            }

//            var currentUserId =
//                _authService
//                    .GetCurrentUserId();

//            if (currentUserId ==
//                user.UserId)
//            {
//                return new ServiceResult
//                {
//                    IsSuccess = false,
//                    Message =
//                        "You cannot deactivate your own account."
//                };
//            }

//            user.IsActive =
//                !user.IsActive;

//            await _context.SaveChangesAsync();

//            return new ServiceResult
//            {
//                IsSuccess = true,
//                Message =
//                    user.IsActive
//                        ? "User activated successfully."
//                        : "User deactivated successfully."
//            };
//        }
//    }
//}












using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Models.DTOs;
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

        public UserService(
            ApplicationDbContext context,
            IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }


        // =========================================================
        // Get All Users
        // =========================================================

        public async Task<List<UserViewModel>> GetAllUsersAsync(
            string? searchTerm,
            string? role,
            string? status)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();


            // Filter by name / employee code / email
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(u =>
                    u.EmployeeCode.Contains(searchTerm) ||

                    u.FirstName.Contains(searchTerm) ||

                    (u.LastName != null &&
                     u.LastName.Contains(searchTerm)) ||

                    ((u.FirstName + " " +
                      (u.LastName ?? ""))
                        .Contains(searchTerm)) ||

                    u.Email.Contains(searchTerm));
            }


            // Filter by role
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u =>
                    u.Role != null &&
                    u.Role.RoleName == role);
            }


            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                bool isActive =
                    status == "Active";

                query = query.Where(u =>
                    u.IsActive == isActive);
            }


            return await query
                .OrderBy(u => u.FirstName)
                .Select(u => new UserViewModel
                {
                    UserId = u.UserId,

                    EmployeeCode =
                        u.EmployeeCode,

                    FullName =
                        u.FullName,

                    Email =
                        u.Email,

                    PhoneNumber =
                        u.PhoneNumber,

                    RoleName =
                        u.Role != null
                            ? u.Role.RoleName
                            : "",

                    IsEmailVerified =
                        u.IsEmailVerified,

                    IsActive =
                        u.IsActive,

                    LastLoginAt =
                        u.LastLoginAt
                })
                .ToListAsync();
        }


        // =========================================================
        // Get Create User ViewModel
        // =========================================================

        public async Task<CreateUserViewModel>
            GetCreateUserViewModelAsync()
        {
            var model =
                new CreateUserViewModel();


            model.EmployeeCode =
                await GenerateEmployeeCodeAsync();


            // -----------------------------------------------------
            // Load Roles
            // -----------------------------------------------------

            model.Roles =
                await _context.Roles
                    .Where(r =>
                        r.RoleKey !=
                        RoleKeys.Admin)
                    .OrderBy(r =>
                        r.DisplayOrder)
                    .Select(r =>
                        new SelectListItem
                        {
                            Value =
                                r.RoleId.ToString(),

                            Text =
                                r.RoleName
                        })
                    .ToListAsync();


            // -----------------------------------------------------
            // Load Active Sales Managers
            // -----------------------------------------------------

            model.SalesManagers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.SalesManager &&

                        u.IsActive)
                    .OrderBy(u =>
                        u.FirstName)
                    .ThenBy(u =>
                        u.LastName)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName
                        })
                    .ToListAsync();


            // -----------------------------------------------------
            // Load Active Team Leads
            // -----------------------------------------------------

            model.TeamLeads =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead &&

                        u.IsActive)
                    .OrderBy(u =>
                        u.FirstName)
                    .ThenBy(u =>
                        u.LastName)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName
                        })
                    .ToListAsync();


            return model;
        }


        // =========================================================
        // Create User
        // =========================================================

        public async Task<ServiceResult>
            CreateUserAsync(
                CreateUserViewModel model)
        {
            // -----------------------------------------------------
            // Check duplicate Employee Code
            // -----------------------------------------------------

            if (await _context.Users.AnyAsync(
                    u =>
                        u.EmployeeCode ==
                        model.EmployeeCode))
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "Employee Code already exists."
                };
            }


            // -----------------------------------------------------
            // Check duplicate Email
            // -----------------------------------------------------

            if (await _context.Users.AnyAsync(
                    u =>
                        u.Email ==
                        model.Email))
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "Email already exists."
                };
            }


            // -----------------------------------------------------
            // Validate selected Role
            // -----------------------------------------------------

            var selectedRole =
                await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.RoleId ==
                        model.RoleId);

            if (selectedRole == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "Selected role was not found."
                };
            }


            // =====================================================
            // Team Lead → Sales Manager
            // =====================================================

            if (selectedRole.RoleKey ==
                RoleKeys.TeamLead)
            {
                if (!model.SalesManagerId.HasValue)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Please select a Sales Manager for the Team Lead."
                    };
                }


                var managerExists =
                    await _context.Users.AnyAsync(u =>
                        u.UserId ==
                        model.SalesManagerId.Value &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.SalesManager &&

                        u.IsActive);

                if (!managerExists)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Selected Sales Manager is invalid or inactive."
                    };
                }
            }


            // =====================================================
            // Sales Officer → Team Lead
            // =====================================================

            else if (selectedRole.RoleKey ==
                     RoleKeys.SalesOfficer)
            {
                if (!model.TeamLeadId.HasValue)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Please select a Team Lead for the Sales Officer."
                    };
                }


                var teamLeadExists =
                    await _context.Users.AnyAsync(u =>
                        u.UserId ==
                        model.TeamLeadId.Value &&

                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead &&

                        u.IsActive);

                if (!teamLeadExists)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Selected Team Lead is invalid or inactive."
                    };
                }


                // Sales Officer no longer directly belongs
                // to Sales Manager in the new hierarchy.
                model.SalesManagerId = null;
            }


            // =====================================================
            // Other Roles
            // =====================================================

            else
            {
                model.SalesManagerId = null;
                model.TeamLeadId = null;
            }


            // -----------------------------------------------------
            // Create User
            // -----------------------------------------------------

            var user = new User
            {
                RoleId =
                    model.RoleId,

                EmployeeCode =
                    await GenerateEmployeeCodeAsync(),

                FirstName =
                    model.FirstName,

                LastName =
                    model.LastName,

                Email =
                    model.Email,

                PhoneNumber =
                    model.PhoneNumber,

                PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            model.Password),

                IsEmailVerified =
                    false,

                IsActive =
                    true,

                LastPasswordChangedAt =
                    DateTime.UtcNow,

                SalesManagerId =
                    model.SalesManagerId,

                TeamLeadId =
                    model.TeamLeadId
            };


            _context.Users.Add(user);

            await _context.SaveChangesAsync();


            return new ServiceResult
            {
                IsSuccess = true,

                Message =
                    "User created successfully."
            };
        }


        // =========================================================
        // Generate Employee Code
        // =========================================================

        public async Task<string>
            GenerateEmployeeCodeAsync()
        {
            var lastEmployee =
                await _context.Users
                    .OrderByDescending(u =>
                        u.UserId)
                    .FirstOrDefaultAsync();

            if (lastEmployee == null)
            {
                return "EMP000001";
            }


            var lastNumber =
                int.Parse(
                    lastEmployee.EmployeeCode
                        .Substring(3));


            return
                $"EMP{(lastNumber + 1):D6}";
        }


        // =========================================================
        // Get Edit User
        // =========================================================

        public async Task<EditUserViewModel?>
            GetEditUserAsync(
                long userId)
        {
            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            userId &&
                            u.IsActive);

            if (user == null)
            {
                return null;
            }


            var model =
                new EditUserViewModel
                {
                    UserId =
                        user.UserId,

                    EmployeeCode =
                        user.EmployeeCode,

                    Email =
                        user.Email,

                    FirstName =
                        user.FirstName,

                    LastName =
                        user.LastName,

                    PhoneNumber =
                        user.PhoneNumber,

                    RoleId =
                        user.RoleId,

                    SalesManagerId =
                        user.SalesManagerId,

                    TeamLeadId =
                        user.TeamLeadId
                };


            // -----------------------------------------------------
            // Load Roles
            // -----------------------------------------------------

            model.Roles =
                await _context.Roles
                    .Where(r =>
                        r.RoleKey !=
                        RoleKeys.Admin)
                    .OrderBy(r =>
                        r.DisplayOrder)
                    .Select(r =>
                        new SelectListItem
                        {
                            Value =
                                r.RoleId.ToString(),

                            Text =
                                r.RoleName
                        })
                    .ToListAsync();


            // -----------------------------------------------------
            // Load Active Sales Managers
            // -----------------------------------------------------

            model.SalesManagers =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.SalesManager &&

                        u.IsActive)
                    .OrderBy(u =>
                        u.FirstName)
                    .ThenBy(u =>
                        u.LastName)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName
                        })
                    .ToListAsync();


            // -----------------------------------------------------
            // Load Active Team Leads
            // -----------------------------------------------------

            model.TeamLeads =
                await _context.Users
                    .Where(u =>
                        u.Role != null &&

                        u.Role.RoleKey ==
                        RoleKeys.TeamLead &&

                        u.IsActive)
                    .OrderBy(u =>
                        u.FirstName)
                    .ThenBy(u =>
                        u.LastName)
                    .Select(u =>
                        new SelectListItem
                        {
                            Value =
                                u.UserId.ToString(),

                            Text =
                                u.FullName,

                            Selected =
                                u.UserId ==
                                user.TeamLeadId
                        })
                    .ToListAsync();


            return model;
        }


        // =========================================================
        // Update User
        // =========================================================

        public async Task<ServiceResult>
            UpdateUserAsync(
                EditUserViewModel model)
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            model.UserId &&
                            u.IsActive);

            if (user == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "User not found."
                };
            }


            // -----------------------------------------------------
            // Check duplicate Phone Number
            // -----------------------------------------------------

            var phoneExists =
                await _context.Users.AnyAsync(
                    u =>
                        u.PhoneNumber ==
                        model.PhoneNumber &&

                        u.UserId !=
                        model.UserId);

            if (phoneExists)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "Phone number already exists."
                };
            }


            // -----------------------------------------------------
            // Validate selected Role
            // -----------------------------------------------------

            var selectedRole =
                await _context.Roles
                    .FirstOrDefaultAsync(
                        r =>
                            r.RoleId ==
                            model.RoleId);

            if (selectedRole == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "Selected role was not found."
                };
            }


            // =====================================================
            // Team Lead → Sales Manager
            // =====================================================

            if (selectedRole.RoleKey ==
                RoleKeys.TeamLead)
            {
                if (!model.SalesManagerId.HasValue)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Please select a Sales Manager for the Team Lead."
                    };
                }


                var managerExists =
                    await _context.Users.AnyAsync(
                        u =>
                            u.UserId ==
                            model.SalesManagerId.Value &&

                            u.Role != null &&

                            u.Role.RoleKey ==
                            RoleKeys.SalesManager &&

                            u.IsActive);

                if (!managerExists)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Selected Sales Manager is invalid or inactive."
                    };
                }


                model.TeamLeadId = null;
            }


            // =====================================================
            // Sales Officer → Team Lead
            // =====================================================

            else if (selectedRole.RoleKey ==
                     RoleKeys.SalesOfficer)
            {
                if (!model.TeamLeadId.HasValue)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Please select a Team Lead for the Sales Officer."
                    };
                }


                var teamLeadExists =
                    await _context.Users.AnyAsync(
                        u =>
                            u.UserId ==
                            model.TeamLeadId.Value &&

                            u.Role != null &&

                            u.Role.RoleKey ==
                            RoleKeys.TeamLead &&

                            u.IsActive);

                if (!teamLeadExists)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,

                        Message =
                            "Selected Team Lead is invalid or inactive."
                    };
                }


                // Sales Officer belongs to Team Lead,
                // not directly to Sales Manager.
                model.SalesManagerId = null;
            }


            // =====================================================
            // Other Roles
            // =====================================================

            else
            {
                model.SalesManagerId = null;
                model.TeamLeadId = null;
            }


            // -----------------------------------------------------
            // Update User
            // -----------------------------------------------------

            user.FirstName =
                model.FirstName;

            user.LastName =
                model.LastName;

            user.PhoneNumber =
                model.PhoneNumber;

            user.RoleId =
                model.RoleId;

            user.SalesManagerId =
                model.SalesManagerId;

            user.TeamLeadId =
                model.TeamLeadId;


            await _context.SaveChangesAsync();


            return new ServiceResult
            {
                IsSuccess = true,

                Message =
                    "User updated successfully."
            };
        }


        // =========================================================
        // Get User Details
        // =========================================================

        public async Task<UserDetailsViewModel?>
            GetUserDetailsAsync(
                long userId)
        {
            var user =
                await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.SalesManager)
                    .Include(u => u.TeamLead)
                        .ThenInclude(t => t.SalesManager)
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            userId &&
                            u.IsActive);

            if (user == null)
            {
                return null;
            }


            // =====================================================
            // Determine Sales Manager
            // =====================================================

            var salesManager =
                user.SalesManager ??
                user.TeamLead?.SalesManager;


            return new UserDetailsViewModel
            {
                UserId =
                    user.UserId,

                EmployeeCode =
                    user.EmployeeCode,

                FullName =
                    user.FullName,

                Email =
                    user.Email,

                PhoneNumber =
                    user.PhoneNumber,

                RoleName =
                    user.Role!.RoleName,

                IsEmailVerified =
                    user.IsEmailVerified,

                IsActive =
                    user.IsActive,

                LastLoginAt =
                    user.LastLoginAt,

                LastPasswordChangedAt =
                    user.LastPasswordChangedAt,

                // =====================================================
                // Hierarchy
                // =====================================================

                SalesManagerId =
                    salesManager?.UserId,

                SalesManagerName =
                    salesManager?.FullName,

                TeamLeadId =
                    user.TeamLeadId,

                TeamLeadName =
                    user.TeamLead?.FullName
            };
        }


        // =========================================================
        // Activate / Deactivate User
        // =========================================================

        public async Task<ServiceResult>
            ToggleUserStatusAsync(
                long userId)
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        u =>
                            u.UserId ==
                            userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "User not found."
                };
            }


            var currentUserId =
                _authService
                    .GetCurrentUserId();

            if (currentUserId ==
                user.UserId)
            {
                return new ServiceResult
                {
                    IsSuccess = false,

                    Message =
                        "You cannot deactivate your own account."
                };
            }


            user.IsActive =
                !user.IsActive;


            await _context.SaveChangesAsync();


            return new ServiceResult
            {
                IsSuccess = true,

                Message =
                    user.IsActive
                        ? "User activated successfully."
                        : "User deactivated successfully."
            };
        }
    }
}