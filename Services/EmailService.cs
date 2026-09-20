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

        public EmailService(
            IOptions<EmailSettings> emailOptions)
        {
            _emailSettings = emailOptions.Value;
        }


        // =====================================================
        // Lead Assignment Email
        // =====================================================

        public async Task SendLeadAssignmentEmailAsync(
            string toEmail,
            string salesOfficerName,
            string leadCode,
            string leadName,
            string assignedBy,
            DateTime assignedAt)
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject =
                $"New Lead Assigned - {leadCode}";


            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
                <h2>New Lead Assignment</h2>

                <p>Dear <strong>{salesOfficerName}</strong>,</p>

                <p>A new lead has been assigned to you.</p>

                <table border='1'
                       cellpadding='8'
                       cellspacing='0'
                       style='border-collapse: collapse;'>

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

                <p>
                    Please log in to the CRM system
                    and accept the lead.
                </p>

                <br/>

                <p>Regards,</p>

                <strong>CRM System</strong>
            ";


            message.Body =
                builder.ToMessageBody();


            using var smtp =
                new SmtpClient();


            // -------------------------------------------------
            // Local development certificate handling
            // -------------------------------------------------

            smtp.CheckCertificateRevocation = false;


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


        // =====================================================
        // Profile Change Approval Email
        // =====================================================

        public async Task SendProfileChangeApprovalEmailAsync(
            string toEmail,
            string userName,
            string fieldName,
            string newValue,
            DateTime approvedAt)
        {
            var message = new MimeMessage();


            // -------------------------------------------------
            // Sender
            // -------------------------------------------------

            message.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail));


            // -------------------------------------------------
            // Recipient
            // -------------------------------------------------

            message.To.Add(
                MailboxAddress.Parse(toEmail));


            // -------------------------------------------------
            // Subject
            // -------------------------------------------------

            message.Subject =
                "CRM Profile Change Approved";


            // -------------------------------------------------
            // Email Body
            // -------------------------------------------------

            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif;
                            max-width: 650px;
                            margin: auto;
                            padding: 20px;'>

                    <h2 style='margin-bottom: 20px;'>
                        Profile Change Approved
                    </h2>

                    <p>
                        Dear <strong>{userName}</strong>,
                    </p>

                    <p>
                        Your profile change request has been
                        <strong>approved by Admin</strong>.
                    </p>

                    <table border='1'
                           cellpadding='10'
                           cellspacing='0'
                           style='border-collapse: collapse;
                                  width: 100%;'>

                        <tr>
                            <td>
                                <strong>Changed Field</strong>
                            </td>

                            <td>
                                {fieldName}
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <strong>New Value</strong>
                            </td>

                            <td>
                                {newValue}
                            </td>
                        </tr>

                        <tr>
                            <td>
                                <strong>Approved At</strong>
                            </td>

                            <td>
                                {approvedAt:dd MMM yyyy hh:mm tt}
                            </td>
                        </tr>

                    </table>

                    <br/>

                    <p>
                        Your requested profile change has now
                        been applied to your CRM account.
                    </p>

                    <p>
                        If you did not request this change,
                        please contact the CRM administrator
                        immediately.
                    </p>

                    <br/>

                    <p>Regards,</p>

                    <strong>CRM System</strong>

                </div>
            ";


            message.Body =
                builder.ToMessageBody();


            // -------------------------------------------------
            // SMTP
            // -------------------------------------------------

            using var smtp =
                new SmtpClient();


            smtp.CheckCertificateRevocation = false;


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