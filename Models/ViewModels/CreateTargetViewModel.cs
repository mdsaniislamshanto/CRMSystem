using System.ComponentModel.DataAnnotations;
using CRMSystem.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRMSystem.Models.ViewModels
{
    public class CreateTargetViewModel
    {
        [Required]
        [Display(Name = "User")]
        public long UserId { get; set; }

        [Required]
        [Display(Name = "Target Period")]
        public TargetPeriodType PeriodType { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target Count")]
        public int TargetCount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } =
            DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } =
            DateTime.Today;

        public IEnumerable<SelectListItem> Users { get; set; } =
            new List<SelectListItem>();
    }
}