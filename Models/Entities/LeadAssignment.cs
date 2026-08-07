using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRMSystem.Enums;

namespace CRMSystem.Models.Entities
{
    public class LeadAssignment : BaseEntity
    {
        [Key]
        public long AssignmentId { get; set; }

        [Required]
        public long LeadId { get; set; }

        [Required]
        public long SalesOfficerId { get; set; }

        [Required]
        public long AssignedBy { get; set; }

        public DateTime AssignedAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public AssignmentStatus AssignmentStatus { get; set; } = AssignmentStatus.Pending;
        [ForeignKey(nameof(LeadId))]
        public Lead? Lead { get; set; }

        [ForeignKey(nameof(SalesOfficerId))]
        public User? SalesOfficer { get; set; }

        [ForeignKey(nameof(AssignedBy))]
        public User? AssignedByUser { get; set; }

        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}