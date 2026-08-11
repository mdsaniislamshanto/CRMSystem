using CRMSystem.Data;
using CRMSystem.Models.DTOs;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CRMSystem.Constants;

namespace CRMSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Login
        public async Task<LoginResult> LoginAsync(LoginViewModel model)
        {
            var result = new LoginResult();

            // Find user by email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    !u.IsDeleted);

            if (user == null)
            {
                result.IsSuccess = false;
                result.Message = "Invalid email or password.";
                return result;
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(
                    model.Password,
                    user.PasswordHash))
            {
                result.IsSuccess = false;
                result.Message = "Invalid email or password.";
                return result;
            }

            // Check active status
            if (!user.IsActive)
            {
                result.IsSuccess = false;
                result.Message = "Your account is inactive.";
                return result;
            }

            // Make sure role exists
            if (user.Role == null)
            {
                result.IsSuccess = false;
                result.Message = "User role is not configured.";
                return result;
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get current HTTP context
            var httpContext =
                _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                result.IsSuccess = false;
                result.Message =
                    "Unable to create authentication session.";

                return result;
            }

            // =====================================================
            // SESSION
            // =====================================================

            var session = httpContext.Session;

            session.SetString(
                SessionKeys.UserId,
                user.UserId.ToString());

            session.SetString(
                SessionKeys.RoleId,
                user.RoleId.ToString());

            session.SetString(
                SessionKeys.RoleKey,
                user.Role.RoleKey);

            session.SetString(
                SessionKeys.FullName,
                user.FullName);


            // =====================================================
            // COOKIE AUTHENTICATION + ROLE CLAIM
            // =====================================================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.RoleKey)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // Login successful
            result.IsSuccess = true;
            result.Message = "Login successful.";
            result.User = user;

            return result;
        }


        // Logout
        public async void Logout()
        {
            var httpContext =
                _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                // Clear session
                httpContext.Session.Clear();

                // Remove authentication cookie
                await httpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }


        // Get Current User Id
        public long? GetCurrentUserId()
        {
            var userId =
                _httpContextAccessor.HttpContext?
                    .Session
                    .GetString(SessionKeys.UserId);

            if (long.TryParse(userId, out long id))
            {
                return id;
            }

            return null;
        }


        // Get Current User Role
        public string? GetCurrentUserRole()
        {
            return _httpContextAccessor.HttpContext?
                .Session
                .GetString(SessionKeys.RoleKey);
        }
    }
}























//using CRMSystem.Data;
//using CRMSystem.Models.DTOs;
//using CRMSystem.Models.ViewModels;
//using CRMSystem.Services.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using CRMSystem.Constants;


//namespace CRMSystem.Services
//{
//    public class AuthService : IAuthService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public AuthService(
//            ApplicationDbContext context,
//            IHttpContextAccessor httpContextAccessor)
//        {
//            _context = context;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        // Login
//        public async Task<LoginResult> LoginAsync(LoginViewModel model)
//        {
//            var result = new LoginResult();

//            // Find user by email
//            var user = await _context.Users
//                .Include(u => u.Role)
//                .FirstOrDefaultAsync(u =>
//                    u.Email == model.Email &&
//                    !u.IsDeleted);

//            if (user == null)
//            {
//                result.IsSuccess = false;
//                result.Message = "Invalid email or password.";
//                return result;
//            }

//            // Verify password
//            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
//            {
//                result.IsSuccess = false;
//                result.Message = "Invalid email or password.";
//                return result;
//            }

//            // Check active status
//            if (!user.IsActive)
//            {
//                result.IsSuccess = false;
//                result.Message = "Your account is inactive.";
//                return result;
//            }

//            // Update last login time
//            user.LastLoginAt = DateTime.UtcNow;
//            await _context.SaveChangesAsync();

//            // Store session
//            var session = _httpContextAccessor.HttpContext!.Session;

//            session.SetString(SessionKeys.UserId, user.UserId.ToString());
//            session.SetString(SessionKeys.RoleId, user.RoleId.ToString());
//            session.SetString(SessionKeys.RoleKey, user.Role!.RoleKey);
//            session.SetString(SessionKeys.FullName, user.FullName);

//            result.IsSuccess = true;
//            result.Message = "Login successful.";
//            result.User = user;

//            return result;
//        }

//        // Logout
//        public void Logout()
//        {
//            _httpContextAccessor.HttpContext?.Session.Clear();
//        }

//        // Get Current User Id
//        public long? GetCurrentUserId()
//        {
//            var userId = _httpContextAccessor.HttpContext?
//                .Session
//                .GetString(SessionKeys.UserId);

//            if (long.TryParse(userId, out long id))
//            {
//                return id;
//            }

//            return null;
//        }

//        // Get Current User Role
//        public string? GetCurrentUserRole()
//        {
//            return _httpContextAccessor.HttpContext?
//                .Session
//              .GetString(SessionKeys.RoleKey);
//        }
//    }
//}