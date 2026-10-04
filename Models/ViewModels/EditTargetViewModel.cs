using System.ComponentModel.DataAnnotations;
using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class EditTargetViewModel
    {
        public long TargetId { get; set; }

        public long UserId { get; set; }

        [Display(Name = "Assigned To")]
        public string UserName { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Target Period")]
        public TargetPeriodType PeriodType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Target count must be at least 1.")]
        [Display(Name = "Target Count")]
        public int TargetCount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }
    }
}
