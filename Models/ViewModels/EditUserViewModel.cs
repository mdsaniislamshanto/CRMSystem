using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class EditUserViewModel
    {
        public long UserId { get; set; }

        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public long RoleId { get; set; }

        public List<SelectListItem> Roles { get; set; } = new();
    }
}