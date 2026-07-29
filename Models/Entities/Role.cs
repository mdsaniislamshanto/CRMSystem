using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.Entities
{
    [Index(nameof(RoleKey), IsUnique = true)]
    [Index(nameof(RoleName), IsUnique = true)]
    public class Role : BaseEntity
    {
        [Key]
        public long RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleKey { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 1;

        // Navigation Property
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}