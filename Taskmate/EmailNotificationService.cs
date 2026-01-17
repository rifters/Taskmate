using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Taskmate
{
    public static class EmailNotificationService
    {
        public static async Task<bool> SendEmailAsync(string subject, string body, NotificationSettings settings)
        {
            if (!settings.EmailEnabled || string.IsNullOrEmpty(settings.EmailToAddress))
                return false;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Task Assigner", settings.EmailFromAddress));
                message.To.Add(new MailboxAddress("", settings.EmailToAddress));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = body,
                    HtmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='background: #f5f5f5; padding: 20px;'>
                                <div style='background: white; padding: 20px; border-radius: 8px; max-width: 600px; margin: 0 auto;'>
                                    <h2 style='color: #1976d2;'>{System.Net.WebUtility.HtmlEncode(subject)}</h2>
                                    <div style='margin: 20px 0;'>
                                        <pre style='font-family: Arial, sans-serif; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(body)}</pre>
                                    </div>
                                    <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                                    <p style='color: #666; font-size: 12px;'>
                                        Sent by Task Assigner at {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                                    </p>
                                </div>
                            </div>
                        </body>
                        </html>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(settings.SmtpServer, settings.SmtpPort, 
                    settings.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                
                if (!string.IsNullOrEmpty(settings.SmtpUsername))
                {
                    await client.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword);
                }
                
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                
                return true;
            }
            catch (Exception ex)
            {
                // Log the full error internally but don't expose to user
                System.Diagnostics.Debug.WriteLine($"Email send failed: {ex}");
                
                // Generic error message for user
                return false;
            }
        }
    }
}