using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.Entities
{
    public class SystemSettings : BaseEntity
    {
        [Key]
        public long SettingId { get; set; }

        [Required]
        public bool AutoAssignmentEnabled { get; set; }
    }
}