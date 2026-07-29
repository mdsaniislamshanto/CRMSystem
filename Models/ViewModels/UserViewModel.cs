namespace CRMSystem.Models.ViewModels
{
    public class UserViewModel
    {
        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }
}