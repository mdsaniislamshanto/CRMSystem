namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerListViewModel
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
        // Workload Summary
        // =====================================================

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int PendingAcceptance { get; set; }

        public int CompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int OverdueFollowUps { get; set; }
    }
}