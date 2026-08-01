using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class MyAssignedLeadViewModel
    {
        public long AssignmentId { get; set; }

        public long LeadId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public AssignmentStatus AssignmentStatus { get; set; }
    }
}