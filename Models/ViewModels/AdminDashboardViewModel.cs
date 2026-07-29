namespace CRMSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int NewLeads { get; set; }

        public int AssignedLeads { get; set; }

        public int AcceptedLeads { get; set; }

        public int InProgressLeads { get; set; }

        public int CompletedLeads { get; set; }

        public int RejectedLeads { get; set; }
    }
}