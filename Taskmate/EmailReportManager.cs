using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Taskmate
{
    /// <summary>
    /// Manages email report delivery
    /// </summary>
    public class EmailReportManager
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "Email");

        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "email.json");
        private static EmailSettings _settings;

        /// <summary>
        /// Load email settings
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    _settings = JsonSerializer.Deserialize<EmailSettings>(json) ?? new EmailSettings();
                }
                else
                {
                    _settings = new EmailSettings();
                }
            }
            catch
            {
                _settings = new EmailSettings();
            }
        }

        /// <summary>
        /// Save email settings
        /// </summary>
        public static void SaveSettings(EmailSettings settings)
        {
            try
            {
                _settings = settings;
                Directory.CreateDirectory(SettingsFolder);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving email settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Get email settings
        /// </summary>
        public static EmailSettings GetSettings()
        {
            if (_settings == null)
                LoadSettings();

            return _settings ?? new EmailSettings();
        }

        /// <summary>
        /// Send email report
        /// </summary>
        public static bool SendReport(string subject, string body, List<string> recipients, string attachmentPath = null)
        {
            try
            {
                if (_settings == null)
                    LoadSettings();

                if (!_settings.IsConfigured)
                    return false;

                using (var client = new SmtpClient())
                {
                    // Connect to SMTP server
                    client.Connect(_settings.SmtpServer, _settings.SmtpPort, _settings.UseSSL);

                    // Authenticate
                    if (!string.IsNullOrEmpty(_settings.Username))
                    {
                        client.Authenticate(_settings.Username, _settings.Password);
                    }

                    // Create message
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrWhiteSpace(recipient))
                            message.To.Add(MailboxAddress.Parse(recipient.Trim()));
                    }

                    message.Subject = subject;

                    // Body with HTML support
                    var bodyBuilder = new BodyBuilder { HtmlBody = body };

                    // Add attachment if provided
                    if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                    {
                        bodyBuilder.Attachments.Add(attachmentPath);
                    }

                    message.Body = bodyBuilder.ToMessageBody();

                    // Send
                    client.Send(message);
                    client.Disconnect(true);

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test email settings
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                if (_settings == null)
                    LoadSettings();

                if (!_settings.IsConfigured)
                    return false;

                using (var client = new SmtpClient())
                {
                    client.Connect(_settings.SmtpServer, _settings.SmtpPort, _settings.UseSSL);

                    if (!string.IsNullOrEmpty(_settings.Username))
                    {
                        client.Authenticate(_settings.Username, _settings.Password);
                    }

                    client.Disconnect(true);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate HTML report body
        /// </summary>
        public static string GenerateReportHTML(List<PersistentAssignment> assignments, string title)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #1976d2; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #1976d2; color: white; }}
        tr:nth-child(even) {{ background-color: #f5f5f5; }}
        .metric {{ display: inline-block; margin-right: 20px; }}
        .metric-value {{ font-size: 24px; font-weight: bold; color: #1976d2; }}
        .metric-label {{ font-size: 12px; color: #666; }}
        .success {{ color: #4caf50; }}
        .warning {{ color: #ff9800; }}
        .error {{ color: #f44336; }}
    </style>
</head>
<body>
    <h1>{title}</h1>
    <p>Generated: {DateTime.Now:g}</p>";

            // Add metrics
            var total = assignments.Count;
            var complete = assignments.FindAll(a => a.OverallCompletionPercentage >= 100).Count;
            var partial = assignments.FindAll(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100).Count;
            var incomplete = assignments.FindAll(a => a.OverallCompletionPercentage == 0).Count;
            var avg = total > 0 ? assignments.Average(a => a.OverallCompletionPercentage) : 0;

            html += $@"
    <div>
        <div class='metric'>
            <div class='metric-value'>{total}</div>
            <div class='metric-label'>Total Assignments</div>
        </div>
        <div class='metric'>
            <div class='metric-value success'>{complete}</div>
            <div class='metric-label'>Completed</div>
        </div>
        <div class='metric'>
            <div class='metric-value warning'>{partial}</div>
            <div class='metric-label'>Partial</div>
        </div>
        <div class='metric'>
            <div class='metric-value error'>{incomplete}</div>
            <div class='metric-label'>Incomplete</div>
        </div>
        <div class='metric'>
            <div class='metric-value'>{avg:F1}%</div>
            <div class='metric-label'>Average</div>
        </div>
    </div>";

            // Add recent assignments table
            html += $@"
    <h2>Recent Assignments</h2>
    <table>
        <tr>
            <th>Date</th>
            <th>Tag</th>
            <th>People</th>
            <th>Completion %</th>
        </tr>";

            foreach (var assignment in assignments.GetRange(0, Math.Min(20, assignments.Count)))
            {
                var completion = assignment.OverallCompletionPercentage;
                var completionClass = completion >= 100 ? "success" : completion > 0 ? "warning" : "error";
                html += $@"
        <tr>
            <td>{assignment.Timestamp:g}</td>
            <td>{assignment.Tag}</td>
            <td>{assignment.Assignments.Count}</td>
            <td><span class='{completionClass}'>{completion:F1}%</span></td>
        </tr>";
            }

            html += @"
    </table>
</body>
</html>";

            return html;
        }
    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        [JsonPropertyName("smtpServer")]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [JsonPropertyName("smtpPort")]
        public int SmtpPort { get; set; } = 587;

        [JsonPropertyName("useSSL")]
        public bool UseSSL { get; set; } = true;

        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("fromEmail")]
        public string FromEmail { get; set; } = "";

        [JsonPropertyName("fromName")]
        public string FromName { get; set; } = "TaskAssigner Reports";

        [JsonPropertyName("isConfigured")]
        public bool IsConfigured => !string.IsNullOrEmpty(FromEmail) && !string.IsNullOrEmpty(SmtpServer);

        [JsonIgnore]
        public bool IsValid => 
            !string.IsNullOrEmpty(SmtpServer) &&
            SmtpPort > 0 &&
            !string.IsNullOrEmpty(FromEmail) &&
            (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password));
    }
}
