using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class SalesTarget : BaseEntity
    {
        [Key]
        public long TargetId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public TargetPeriodType PeriodType { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TargetCount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }
    }
}