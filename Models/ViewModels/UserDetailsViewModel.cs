using System;

namespace CRMSystem.Models.ViewModels
{
    public class UserDetailsViewModel
    {
        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastPasswordChangedAt { get; set; }

        // =====================================================
        // Sales Hierarchy
        // =====================================================

        public long? SalesManagerId { get; set; }

        public string? SalesManagerName { get; set; }

        public long? TeamLeadId { get; set; }

        public string? TeamLeadName { get; set; }
    }
}