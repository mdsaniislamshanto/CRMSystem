namespace CRMSystem.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendLeadAssignmentEmailAsync(
            string toEmail,
            string salesOfficerName,
            string leadCode,
            string leadName,
            string assignedBy,
            DateTime assignedAt);
    }
}