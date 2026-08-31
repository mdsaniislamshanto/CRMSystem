using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class LeadViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string LeadName { get; set; } = string.Empty;

        public string? Profession { get; set; }

        public string? Email { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        public LeadSource Source { get; set; }

        public LeadPriority Priority { get; set; }

        public LeadStatus Status { get; set; }

        public string? Description { get; set; }

        public DateTime? FollowUpDate { get; set; }

        // ==========================================
        // Active Assignment Information
        // ==========================================

        public string? AssignedOfficerName { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public AssignmentStatus? AssignmentStatus { get; set; }

        // ==========================================
        // Acceptance SLA Information
        // ==========================================

        public bool AcceptanceSLAMissed { get; set; }

        public string AcceptanceSLAStatus { get; set; } = "Not Assigned";
    }
}