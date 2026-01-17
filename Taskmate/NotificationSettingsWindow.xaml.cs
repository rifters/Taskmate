using System;
using System.Windows;

namespace Taskmate
{
    public partial class NotificationSettingsWindow : Window
    {
        private NotificationSettings settings = null!;

        public NotificationSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            settings = NotificationSettings.Load();

            // Email
            chkEmailEnabled.IsChecked = settings.EmailEnabled;
            txtSmtpServer.Text = settings.SmtpServer;
            txtSmtpPort.Text = settings.SmtpPort.ToString();
            chkSmtpUseSsl.IsChecked = settings.SmtpUseSsl;
            txtSmtpUsername.Text = settings.SmtpUsername;
            txtSmtpPassword.Password = settings.SmtpPassword;
            txtEmailFrom.Text = settings.EmailFromAddress;
            txtEmailTo.Text = settings.EmailToAddress;

            // SMS
            chkSmsEnabled.IsChecked = settings.SmsEnabled;
            txtTwilioAccountSid.Text = settings.TwilioAccountSid;
            txtTwilioAuthToken.Password = settings.TwilioAuthToken;
            txtTwilioFromNumber.Text = settings.TwilioFromNumber;
            txtSmsToNumber.Text = settings.SmsToNumber;
        }

        private void SaveSettings()
        {
            // Email
            settings.EmailEnabled = chkEmailEnabled.IsChecked == true;
            settings.SmtpServer = txtSmtpServer.Text.Trim();
            settings.SmtpPort = int.TryParse(txtSmtpPort.Text, out int port) ? port : 587;
            settings.SmtpUseSsl = chkSmtpUseSsl.IsChecked == true;
            settings.SmtpUsername = txtSmtpUsername.Text.Trim();
            settings.SmtpPassword = txtSmtpPassword.Password;
            settings.EmailFromAddress = txtEmailFrom.Text.Trim();
            settings.EmailToAddress = txtEmailTo.Text.Trim();

            // SMS
            settings.SmsEnabled = chkSmsEnabled.IsChecked == true;
            settings.TwilioAccountSid = txtTwilioAccountSid.Text.Trim();
            settings.TwilioAuthToken = txtTwilioAuthToken.Password.Trim();
            settings.TwilioFromNumber = txtTwilioFromNumber.Text.Trim();
            settings.SmsToNumber = txtSmsToNumber.Text.Trim();
        }

        private bool ValidateEmailSettings()
        {
            if (!settings.EmailEnabled)
                return true;

            if (!IsValidEmail(settings.EmailFromAddress))
            {
                MessageBox.Show("Invalid 'From' email address.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!IsValidEmail(settings.EmailToAddress))
            {
                MessageBox.Show("Invalid 'To' email address.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.SmtpServer))
            {
                MessageBox.Show("SMTP server is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (settings.SmtpPort < 1 || settings.SmtpPort > 65535)
            {
                MessageBox.Show("SMTP port must be between 1 and 65535.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateSmsSettings()
        {
            if (!settings.SmsEnabled)
                return true;

            if (!IsValidPhoneNumber(settings.TwilioFromNumber))
            {
                MessageBox.Show("Invalid Twilio 'From' phone number. Must be in format: +1234567890", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!IsValidPhoneNumber(settings.SmsToNumber))
            {
                MessageBox.Show("Invalid 'To' phone number. Must be in format: +1234567890", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.TwilioAccountSid) || 
                settings.TwilioAccountSid.Length < 30)
            {
                MessageBox.Show("Invalid Twilio Account SID (must be at least 30 characters).", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.TwilioAuthToken) || 
                settings.TwilioAuthToken.Length < 30)
            {
                MessageBox.Show("Invalid Twilio Auth Token (must be at least 30 characters).", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Must start with + and contain 10-15 digits after that
            return System.Text.RegularExpressions.Regex.IsMatch(
                phoneNumber, @"^\+\d{10,15}$");
        }

        private async void btnTestEmail_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            if (!ValidateEmailSettings())
                return;

            if (!settings.EmailEnabled)
            {
                MessageBox.Show("Please enable email notifications first.", "Email Disabled", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                bool success = await EmailNotificationService.SendEmailAsync(
                    "Test Email from Task Assigner",
                    "This is a test email to verify your email notification settings are working correctly.\n\n" +
                    $"Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    settings);

                if (success)
                {
                    MessageBox.Show("Test email sent successfully! Check your inbox.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to send test email. Please check your settings and try again.", 
                        "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending test email:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnTestSms_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            if (!ValidateSmsSettings())
                return;

            if (!settings.SmsEnabled)
            {
                MessageBox.Show("Please enable SMS notifications first.", "SMS Disabled", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                bool success = await SmsNotificationService.SendSmsAsync(
                    $"Test SMS from Task Assigner\nSent at: {DateTime.Now:HH:mm:ss}",
                    settings);

                if (success)
                {
                    MessageBox.Show("Test SMS sent successfully! Check your phone.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to send test SMS. Please check your Twilio credentials and try again.", 
                        "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending test SMS:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            
            if (!ValidateEmailSettings() || !ValidateSmsSettings())
                return;

            settings.Save();
            
            MessageBox.Show("Notification settings saved successfully!", "Saved", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}