using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class ProfileViewModel
    {
        public long UserId { get; set; }


        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;


        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }


        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;


        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }


        // Existing saved image path
        public string? ProfileImage { get; set; }


        // New uploaded image
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImageFile { get; set; }


        public string RoleName { get; set; } = string.Empty;

        public string EmployeeCode { get; set; } = string.Empty;


        // =====================================================
        // Personal Notification Preference
        // =====================================================

        public bool NotificationsEnabled { get; set; } = true;
    }
}