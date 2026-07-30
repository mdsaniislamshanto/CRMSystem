using CRMSystem.Configurations;
using CRMSystem.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CRMSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailOptions)
        {
            _emailSettings = emailOptions.Value;
        }

        public async Task SendLeadAssignmentEmailAsync(
            string toEmail,
            string salesOfficerName,
            string leadCode,
            string leadName,
            string assignedBy,
            DateTime assignedAt)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _emailSettings.SenderName,
                _emailSettings.SenderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = $"New Lead Assigned - {leadCode}";

            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
                <h2>New Lead Assignment</h2>

                <p>Dear <strong>{salesOfficerName}</strong>,</p>

                <p>A new lead has been assigned to you.</p>

                <table border='1' cellpadding='8' cellspacing='0'>
                    <tr>
                        <td><strong>Lead Code</strong></td>
                        <td>{leadCode}</td>
                    </tr>

                    <tr>
                        <td><strong>Lead Name</strong></td>
                        <td>{leadName}</td>
                    </tr>

                    <tr>
                        <td><strong>Assigned By</strong></td>
                        <td>{assignedBy}</td>
                    </tr>

                    <tr>
                        <td><strong>Assigned Time</strong></td>
                        <td>{assignedAt:dd MMM yyyy hh:mm tt}</td>
                    </tr>
                </table>

                <br/>

                <p>Please log in to the CRM system and accept the lead.</p>

                <br/>

                <p>Regards,</p>

                <strong>CRM System</strong>
            ";

            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _emailSettings.SenderEmail,
                _emailSettings.Password);

            try
            {
                await smtp.SendAsync(message);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}