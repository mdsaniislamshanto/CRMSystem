namespace CRMSystem.Services.Interfaces
{
    public interface IEmailService
    {
        // =====================================================
        // Lead Assignment Email
        // =====================================================

        Task SendLeadAssignmentEmailAsync(
            string toEmail,
            string salesOfficerName,
            string leadCode,
            string leadName,
            string assignedBy,
            DateTime assignedAt);


        // =====================================================
        // Profile Change Approval Email
        // =====================================================

        Task SendProfileChangeApprovalEmailAsync(
            string toEmail,
            string userName,
            string fieldName,
            string newValue,
            DateTime approvedAt);
    }
}