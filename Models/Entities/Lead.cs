using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;

namespace CRMSystem.Models.Entities
{
    public class Lead : BaseEntity
    {
        [Key]
        public long LeadId { get; set; }

        [Required]
        [StringLength(20)]
        public string LeadCode { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [Required]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Profession { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Address { get; set; }

        public LeadSource Source { get; set; }

        [StringLength(200)]
        public string? SourceReferenceId { get; set; }

        [StringLength(200)]
        public string? SourceCampaign { get; set; }

        public LeadPriority Priority { get; set; }

        public LeadStatus Status { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public DateTime? LastContactDate { get; set; }

        public bool IsArchived { get; set; } = false;

        public long CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }
    }
}