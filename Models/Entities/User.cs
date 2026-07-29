using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMSystem.Models.Entities
{
    [Index(nameof(EmployeeCode), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User : BaseEntity
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        public long RoleId { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ProfileImage { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        public DateTime? LastLoginAt { get; set; }

        public DateTime? LastPasswordChangedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Property
        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }
    }
}