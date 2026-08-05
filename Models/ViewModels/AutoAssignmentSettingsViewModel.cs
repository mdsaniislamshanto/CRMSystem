using System.ComponentModel.DataAnnotations;

namespace CRMSystem.Models.ViewModels
{
    public class AutoAssignmentSettingsViewModel
    {
        public long SettingId { get; set; }
        [Display(Name = "Enable Auto Assignment!")]
        public bool AutoAssignmentEnabled { get; set; }
    }
}
