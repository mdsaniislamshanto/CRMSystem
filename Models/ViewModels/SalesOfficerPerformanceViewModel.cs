namespace CRMSystem.Models.ViewModels
{
    public class SalesOfficerPerformanceViewModel
    {
        public long SalesOfficerId { get; set; }

        public string SalesOfficerName { get; set; } = string.Empty;

        public int TotalAssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int PendingAcceptance { get; set; }

        public int CompletedLeads { get; set; }

        public int TotalFeedbacks { get; set; }

        public int AcceptanceSLAMissed { get; set; }

        public int FirstFeedbackSLAMissed { get; set; }

        public int OverdueFollowUps { get; set; }
    }
}