using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class AutoAssignmentRequestViewModel
    {
        public long RequestId { get; set; }

        public long RequestedBy { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string SalesManagerName { get; set; } = string.Empty;

        public bool RequestedStatus { get; set; }

        public ApprovalStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }

        public long? ReviewedBy { get; set; }

        public string? ReviewerName { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? AdminComment { get; set; }
    }
}