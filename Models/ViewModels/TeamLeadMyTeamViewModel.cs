namespace CRMSystem.Models.ViewModels
{
    public class TeamLeadMyTeamViewModel
    {
        public string TeamLeadName { get; set; } = string.Empty;

        public int TeamMemberCount { get; set; }

        public List<TeamMemberViewModel> TeamMembers { get; set; }
            = new();
    }


    public class TeamMemberViewModel
    {
        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public int TotalLeads { get; set; }

        public int ActiveLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int PendingFollowUps { get; set; }
    }
}