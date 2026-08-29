using CRMSystem.Constants;
using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.DTOs;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;

        private readonly IWebHostEnvironment _environment;

        private readonly INotificationService _notificationService;

        public SettingsService(ApplicationDbContext context, IWebHostEnvironment environment, INotificationService notificationService)
        {
            _context = context;
            _environment = environment;
            _notificationService = notificationService;
        }


        // =====================================================
        // System Settings
        // =====================================================

        public async Task<SystemSettings> GetSettingsAsync()
        {
            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "System settings record was not found.");
            }

            return settings;
        }

        //for admin only- Auto Assignment setting update without request
        public async Task UpdateAutoAssignmentAsync(bool enabled)
        {
            var settings = await GetSettingsAsync();

            settings.AutoAssignmentEnabled = enabled;

            await _context.SaveChangesAsync();
        }


        // =====================================================
        // Notification Settings
        // =====================================================

        // Admin only - update own notification preference
        public async Task UpdateUserNotificationPreferenceAsync(
            long userId,
            bool enabled)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "User not found.");
            }


            // =====================================================
            // Admin-only Personal Notification Preference
            // =====================================================

            if (user.Role?.RoleKey != RoleKeys.Admin)
            {
                throw new InvalidOperationException(
                    "Only Admin can change personal notification settings.");
            }


            user.NotificationsEnabled = enabled;

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // Notification Settings
        // =====================================================

        // Admin only - update global notification settings
        public async Task UpdateGlobalNotificationSettingsAsync(
            bool globalNotificationsEnabled,
            bool leadAssignmentNotificationEnabled,
            bool followUpNotificationEnabled,
            bool feedbackNotificationEnabled,
            bool overdueNotificationEnabled)
        {
            var settings = await GetSettingsAsync();

            settings.GlobalNotificationsEnabled =
                globalNotificationsEnabled;

            settings.LeadAssignmentNotificationEnabled =
                leadAssignmentNotificationEnabled;

            settings.FollowUpNotificationEnabled =
                followUpNotificationEnabled;

            settings.FeedbackNotificationEnabled =
                feedbackNotificationEnabled;

            settings.OverdueNotificationEnabled =
                overdueNotificationEnabled;

            await _context.SaveChangesAsync();
        }




        // =====================================================
        // Profile
        // =====================================================

        //for user only- get profile
        public async Task<ProfileViewModel?> GetProfileAsync(long userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new ProfileViewModel
                {
                    UserId = u.UserId,

                    FirstName = u.FirstName,

                    LastName = u.LastName,

                    Email = u.Email,

                    PhoneNumber = u.PhoneNumber,

                    ProfileImage = u.ProfileImage,

                    RoleName = u.Role != null
                        ? u.Role.RoleName
                        : string.Empty,

                    EmployeeCode = u.EmployeeCode,

                    NotificationsEnabled = u.NotificationsEnabled,
                })
                .FirstOrDefaultAsync();
        }



        //for admin only- update profile without request
        public async Task UpdateProfileAsync(
         long userId,
         ProfileViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "User not found.");
            }


            // =====================================================
            // Update Basic Profile Information
            // =====================================================

            user.FirstName = model.FirstName.Trim();

            user.LastName =
                string.IsNullOrWhiteSpace(model.LastName)
                    ? null
                    : model.LastName.Trim();

            user.PhoneNumber =
                string.IsNullOrWhiteSpace(model.PhoneNumber)
                    ? null
                    : model.PhoneNumber.Trim();



            // =====================================================
            // Email
            // =====================================================

            var newEmail = model.Email.Trim();

            if (!string.Equals(
                    user.Email,
                    newEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Users
                    .AnyAsync(u =>
                        u.UserId != userId &&
                        u.Email == newEmail);

                if (emailExists)
                {
                    throw new InvalidOperationException(
                        "This email address is already in use.");
                }

                user.Email = newEmail;

                user.IsEmailVerified = false;
            }


            // =====================================================
            // Profile Image Upload
            // =====================================================

            if (model.ProfileImageFile != null &&
                model.ProfileImageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };


                var extension =
                    Path.GetExtension(
                        model.ProfileImageFile.FileName)
                        .ToLowerInvariant();


                // -------------------------------------------------
                // Validate Extension
                // -------------------------------------------------

                if (!allowedExtensions.Contains(extension))
                {
                    throw new InvalidOperationException(
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }


                // -------------------------------------------------
                // Validate File Size
                // -------------------------------------------------

                const long maxFileSize = 2 * 1024 * 1024;

                if (model.ProfileImageFile.Length > maxFileSize)
                {
                    throw new InvalidOperationException(
                        "Profile image must not exceed 2 MB.");
                }


                // -------------------------------------------------
                // Upload Folder
                // -------------------------------------------------

                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profile-images");


                Directory.CreateDirectory(uploadsFolder);


                // -------------------------------------------------
                // Generate Unique File Name
                // -------------------------------------------------

                var fileName =
                    $"{Guid.NewGuid():N}{extension}";


                var filePath = Path.Combine(
                    uploadsFolder,
                    fileName);


                // -------------------------------------------------
                // Save New Image
                // -------------------------------------------------

                await using (var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    await model.ProfileImageFile
                        .CopyToAsync(stream);
                }


                // -------------------------------------------------
                // Delete Old Image
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(user.ProfileImage))
                {
                    var oldFileName =
                        Path.GetFileName(user.ProfileImage);

                    var oldFilePath =
                        Path.Combine(
                            uploadsFolder,
                            oldFileName);

                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }


                // -------------------------------------------------
                // Store New Image Path
                // -------------------------------------------------

                user.ProfileImage =
                    $"/uploads/profile-images/{fileName}";
            }


            await _context.SaveChangesAsync();
        }


        // =====================================================
        // Direct Profile Image Update for sales officer
        // =====================================================

        public async Task<ServiceResult>
            UpdateProfileImageAsync(
                long userId,
                IFormFile profileImage)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "User not found."
                };
            }


            // =====================================================
            // Validate File
            // =====================================================

            if (profileImage == null ||
                profileImage.Length == 0)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Please select a profile image."
                };
            }


            // =====================================================
            // Allowed Extensions
            // =====================================================

            var allowedExtensions = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };


            var extension =
                Path.GetExtension(
                    profileImage.FileName)
                    .ToLowerInvariant();


            if (!allowedExtensions.Contains(extension))
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Only JPG, JPEG, PNG and WEBP images are allowed."
                };
            }


            // =====================================================
            // File Size Validation
            // =====================================================

            const long maxFileSize =
                2 * 1024 * 1024;


            if (profileImage.Length > maxFileSize)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Profile image must not exceed 2 MB."
                };
            }


            // =====================================================
            // Upload Folder
            // =====================================================

            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profile-images");


            Directory.CreateDirectory(
                uploadsFolder);


            // =====================================================
            // Generate Unique File Name
            // =====================================================

            var fileName =
                $"{Guid.NewGuid():N}{extension}";


            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName);


            // =====================================================
            // Save New Image
            // =====================================================

            await using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await profileImage
                    .CopyToAsync(stream);
            }


            // =====================================================
            // Delete Old Image
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    user.ProfileImage))
            {
                var oldFileName =
                    Path.GetFileName(
                        user.ProfileImage);


                var oldFilePath =
                    Path.Combine(
                        uploadsFolder,
                        oldFileName);


                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }


            // =====================================================
            // Store New Image Path
            // =====================================================

            user.ProfileImage =
                $"/uploads/profile-images/{fileName}";


            await _context.SaveChangesAsync();


            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Profile picture updated successfully."
            };
        }



        // =====================================================
        // Password
        // =====================================================

        public async Task<ServiceResult> ChangePasswordAsync(
            long userId,
            ChangePasswordViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "User not found."
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(
                    model.CurrentPassword,
                    user.PasswordHash))
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Current password is incorrect."
                };
            }

            if (BCrypt.Net.BCrypt.Verify(
                    model.NewPassword,
                    user.PasswordHash))
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "New password must be different from the current password."
                };
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            user.LastPasswordChangedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message = "Password changed successfully."
            };
        }


        // =====================================================
        // Profile Change Request
        // =====================================================

        public async Task<ServiceResult>
            SubmitProfileChangeRequestAsync(
                long userId,
                ProfileViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "User not found."
                };
            }


            // -------------------------------------------------
            // First Name
            // -------------------------------------------------

            if (!string.Equals(
                    user.FirstName,
                    model.FirstName?.Trim(),
                    StringComparison.Ordinal))
            {
                var result =
                    await CreateProfileChangeRequestAsync(
                        userId,
                        "FirstName",
                        user.FirstName,
                        model.FirstName!.Trim());

                if (!result.IsSuccess)
                {
                    return result;
                }
            }


            // -------------------------------------------------
            // Last Name
            // -------------------------------------------------

            var oldLastName = user.LastName ?? string.Empty;

            var newLastName =
                model.LastName?.Trim() ?? string.Empty;

            if (!string.Equals(
                    oldLastName,
                    newLastName,
                    StringComparison.Ordinal))
            {
                var result =
                    await CreateProfileChangeRequestAsync(
                        userId,
                        "LastName",
                        oldLastName,
                        newLastName);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }


            // -------------------------------------------------
            // Phone Number
            // -------------------------------------------------

            var oldPhone = user.PhoneNumber ?? string.Empty;

            var newPhone =
                model.PhoneNumber?.Trim() ?? string.Empty;

            if (!string.Equals(
                    oldPhone,
                    newPhone,
                    StringComparison.Ordinal))
            {
                var result =
                    await CreateProfileChangeRequestAsync(
                        userId,
                        "PhoneNumber",
                        oldPhone,
                        newPhone);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }


            // -------------------------------------------------
            // Email
            // -------------------------------------------------

            if (!string.Equals(
                    user.Email,
                    model.Email?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                var newEmail = model.Email.Trim();

                var emailExists = await _context.Users
                    .AnyAsync(u =>
                        u.UserId != userId &&
                        u.Email == newEmail);

                if (emailExists)
                {
                    return new ServiceResult
                    {
                        IsSuccess = false,
                        Message = "This email address is already in use."
                    };
                }

                var result =
                    await CreateProfileChangeRequestAsync(
                        userId,
                        "Email",
                        user.Email,
                        newEmail);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }


            return new ServiceResult
            {
                IsSuccess = true,
                Message = "Profile change request submitted for Admin approval."
            };
        }


        private async Task<ServiceResult>
            CreateProfileChangeRequestAsync(
                long userId,
                string fieldName,
                string oldValue,
                string newValue)
        {
            var existingPendingRequest =
                await _context.ProfileChangeRequests
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.FieldName == fieldName &&
                        r.Status == ApprovalStatus.Pending);

            if (existingPendingRequest)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        $"A pending {fieldName} change request already exists."
                };
            }

            var request = new ProfileChangeRequest
            {
                UserId = userId,

                FieldName = fieldName,

                OldValue = oldValue,

                NewValue = newValue,

                Status = ApprovalStatus.Pending,

                RequestedAt = DateTime.UtcNow
            };

            _context.ProfileChangeRequests.Add(request);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message = $"{fieldName} change request submitted."
            };
        }


        // =====================================================
        // Pending Profile Requests
        // =====================================================

        public async Task<List<ProfileChangeRequestViewModel>>
            GetPendingProfileChangeRequestsAsync()
        {
            return await _context.ProfileChangeRequests
                .Where(r =>
                    r.Status == ApprovalStatus.Pending)
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new ProfileChangeRequestViewModel
                {
                    RequestId = r.RequestId,

                    UserId = r.UserId,

                    EmployeeCode =
                        r.User!.EmployeeCode,

                    UserName =
                        r.User.FullName,

                    FieldName =
                        r.FieldName,

                    OldValue =
                        r.OldValue,

                    NewValue =
                        r.NewValue,

                    Status =
                        r.Status,

                    RequestedAt =
                        r.RequestedAt,

                    ReviewedBy =
                        r.ReviewedBy,

                    ReviewedAt =
                        r.ReviewedAt,

                    AdminComment =
                        r.AdminComment
                })
                .ToListAsync();
        }


        // =====================================================
        // Approve Profile Request
        // =====================================================

        public async Task<ServiceResult>
            ApproveProfileChangeRequestAsync(
                long requestId,
                long adminId,
                string? adminComment)
        {
            var request =
                await _context.ProfileChangeRequests
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == requestId);

            if (request == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Profile change request not found."
                };
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "This request has already been reviewed."
                };
            }

            if (request.User == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Requested user was not found."
                };
            }


            switch (request.FieldName)
            {
                case "FirstName":

                    request.User.FirstName =
                        request.NewValue.Trim();

                    break;


                case "LastName":

                    request.User.LastName =
                        string.IsNullOrWhiteSpace(request.NewValue)
                            ? null
                            : request.NewValue.Trim();

                    break;


                case "PhoneNumber":

                    request.User.PhoneNumber =
                        string.IsNullOrWhiteSpace(request.NewValue)
                            ? null
                            : request.NewValue.Trim();

                    break;


                case "Email":

                    var emailExists =
                        await _context.Users.AnyAsync(u =>
                            u.UserId != request.UserId &&
                            u.Email == request.NewValue);

                    if (emailExists)
                    {
                        return new ServiceResult
                        {
                            IsSuccess = false,
                            Message =
                                "The requested email address is already in use."
                        };
                    }

                    request.User.Email =
                        request.NewValue.Trim();

                    request.User.IsEmailVerified = false;

                    break;


                default:

                    return new ServiceResult
                    {
                        IsSuccess = false,
                        Message = "Invalid profile change field."
                    };
            }


            request.Status = ApprovalStatus.Approved;

            request.ReviewedBy = adminId;

            request.ReviewedAt = DateTime.UtcNow;

            request.AdminComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            await _context.SaveChangesAsync();

            //for in app notification to user about approval of profile change request
            await _notificationService.CreateNotificationAsync(
                request.UserId,
                NotificationType.ProfileChangeApproved,
                "Profile Change Approved",
                $"Your {request.FieldName} change request has been approved by Admin.",
                null,
                null);

            return new ServiceResult
            {
                IsSuccess = true,
                Message = "Profile change request approved successfully."
            };
        }


        // =====================================================
        // Reject Profile Request
        // =====================================================

        public async Task<ServiceResult>
            RejectProfileChangeRequestAsync(
                long requestId,
                long adminId,
                string? adminComment)
        {
            var request =
                await _context.ProfileChangeRequests
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == requestId);

            if (request == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "Profile change request not found."
                };
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message = "This request has already been reviewed."
                };
            }

            request.Status = ApprovalStatus.Rejected;

            request.ReviewedBy = adminId;

            request.ReviewedAt = DateTime.UtcNow;

            request.AdminComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            await _context.SaveChangesAsync();

            //for in app notification to user about rejection of profile change request
            await _notificationService.CreateNotificationAsync(
                request.UserId,
                NotificationType.ProfileChangeRejected,
                "Profile Change Rejected",
                $"Your {request.FieldName} change request has been rejected by Admin.",
                null,
                null);

            return new ServiceResult
            {
                IsSuccess = true,
                Message = "Profile change request rejected successfully."
            };
        }


        // =====================================================
        // Auto Assignment Request
        // =====================================================

        public async Task<ServiceResult>
            SubmitAutoAssignmentRequestAsync(
                long salesManagerId,
                bool enabled)
        {
            var existingPendingRequest =
                await _context.AutoAssignmentRequests
                    .AnyAsync(r =>
                        r.RequestedBy == salesManagerId &&
                        r.Status == ApprovalStatus.Pending);

            if (existingPendingRequest)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "You already have a pending Auto Assignment request."
                };
            }

            var currentSettings =
                await GetSettingsAsync();

            if (currentSettings.AutoAssignmentEnabled == enabled)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Auto Assignment is already in the requested state."
                };
            }

            var request = new AutoAssignmentRequest
            {
                RequestedBy = salesManagerId,

                RequestedStatus = enabled,

                Status = ApprovalStatus.Pending,

                RequestedAt = DateTime.UtcNow
            };

            _context.AutoAssignmentRequests.Add(request);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Auto Assignment request submitted for Admin approval."
            };
        }


        // =====================================================
        // Pending Auto Assignment Requests
        // =====================================================

        public async Task<List<AutoAssignmentRequestViewModel>>
            GetPendingAutoAssignmentRequestsAsync()
        {
            return await _context.AutoAssignmentRequests
                .Where(r =>
                    r.Status == ApprovalStatus.Pending)
                .Include(r => r.Requester)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new AutoAssignmentRequestViewModel
                {
                    RequestId =
                        r.RequestId,

                    RequestedBy =
                        r.RequestedBy,

                    EmployeeCode =
                        r.Requester!.EmployeeCode,

                    SalesManagerName =
                        r.Requester.FullName,

                    RequestedStatus =
                        r.RequestedStatus,

                    Status =
                        r.Status,

                    RequestedAt =
                        r.RequestedAt,

                    ReviewedBy =
                        r.ReviewedBy,

                    ReviewedAt =
                        r.ReviewedAt,

                    AdminComment =
                        r.AdminComment
                })
                .ToListAsync();
        }


        // =====================================================
        // Approve Auto Assignment Request
        // =====================================================

        public async Task<ServiceResult>
            ApproveAutoAssignmentRequestAsync(
                long requestId,
                long adminId,
                string? adminComment)
        {
            var request =
                await _context.AutoAssignmentRequests
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == requestId);

            if (request == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Auto Assignment request not found."
                };
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "This request has already been reviewed."
                };
            }

            var settings =
                await GetSettingsAsync();

            settings.AutoAssignmentEnabled =
                request.RequestedStatus;

            request.Status =
                ApprovalStatus.Approved;

            request.ReviewedBy =
                adminId;

            request.ReviewedAt =
                DateTime.UtcNow;

            request.AdminComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Auto Assignment request approved successfully."
            };
        }


        // =====================================================
        // Reject Auto Assignment Request
        // =====================================================

        public async Task<ServiceResult>
            RejectAutoAssignmentRequestAsync(
                long requestId,
                long adminId,
                string? adminComment)
        {
            var request =
                await _context.AutoAssignmentRequests
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == requestId);

            if (request == null)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "Auto Assignment request not found."
                };
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                return new ServiceResult
                {
                    IsSuccess = false,
                    Message =
                        "This request has already been reviewed."
                };
            }

            request.Status =
                ApprovalStatus.Rejected;

            request.ReviewedBy =
                adminId;

            request.ReviewedAt =
                DateTime.UtcNow;

            request.AdminComment =
                string.IsNullOrWhiteSpace(adminComment)
                    ? null
                    : adminComment.Trim();

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                IsSuccess = true,
                Message =
                    "Auto Assignment request rejected."
            };
        }
    }
}