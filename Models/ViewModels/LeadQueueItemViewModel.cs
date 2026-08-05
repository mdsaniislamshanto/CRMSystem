namespace CRMSystem.Models.ViewModels
{
    public class LeadQueueItemViewModel
    {
        public long LeadId { get; set; }

        public string LeadCode { get; set; } = string.Empty;

        public string LeadName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}