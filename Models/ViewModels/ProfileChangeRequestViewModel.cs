using CRMSystem.Enums;

namespace CRMSystem.Models.ViewModels
{
    public class ProfileChangeRequestViewModel
    {
        public long RequestId { get; set; }

        public long UserId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string NewValue { get; set; } = string.Empty;

        public ApprovalStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }

        public long? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? AdminComment { get; set; }
    }
}