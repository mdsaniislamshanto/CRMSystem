namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerDetailsViewModel
    {
        // =====================================================
        // Basic Information
        // =====================================================

        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? ProfileImage { get; set; }

        public bool IsActive { get; set; }


        // =====================================================
        // Lead Performance
        // =====================================================

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int PendingAcceptance { get; set; }

        public int CompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int OverdueFollowUps { get; set; }


        // =====================================================
        // Acceptance Performance
        // =====================================================

        public double AcceptanceRate { get; set; }


        // =====================================================
        // SLA Performance
        // =====================================================

        public int AcceptanceSLAMissed { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }

        public int NextFeedbackSLAMissed { get; set; }


        // =====================================================
        // Recent Lead Activity
        // =====================================================

        public List<SalesOfficerLeadActivityViewModel> RecentLeads { get; set; }
            = new();


        // =====================================================
        // Recent Feedback Activity
        // =====================================================

        public List<SalesOfficerFeedbackActivityViewModel> RecentFeedbacks { get; set; }
            = new();
    }


    // =========================================================
    // Recent Lead Activity
    // =========================================================

    public class SalesOfficerLeadActivityViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? NextFollowUpDate { get; set; }
    }


    // =========================================================
    // Recent Feedback Activity
    // =========================================================

    public class SalesOfficerFeedbackActivityViewModel
    {
        public long FeedbackId { get; set; }

        public long LeadId { get; set; }

        public string LeadName { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? NextFollowUpDate { get; set; }

        public bool NextFeedbackSLAMissed { get; set; }
    }
}