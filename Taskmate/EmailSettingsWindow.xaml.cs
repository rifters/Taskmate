using System;
using System.Windows;

namespace Taskmate
{
    public partial class EmailSettingsWindow : Window
    {
        private EmailSettings _settings;

        public EmailSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                EmailReportManager.LoadSettings();
                _settings = EmailReportManager.GetSettings();

                txtSmtpServer.Text = _settings.SmtpServer;
                txtSmtpPort.Text = _settings.SmtpPort.ToString();
                chkUseSSL.IsChecked = _settings.UseSSL;
                txtFromEmail.Text = _settings.FromEmail;
                txtFromName.Text = _settings.FromName;
                txtUsername.Text = _settings.Username;
                pwdPassword.Password = _settings.Password;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtTestStatus.Text = "Testing connection...";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Gray;

                // Update settings temporarily
                _settings.SmtpServer = txtSmtpServer.Text;
                if (int.TryParse(txtSmtpPort.Text, out int port))
                    _settings.SmtpPort = port;
                _settings.UseSSL = chkUseSSL.IsChecked ?? true;
                _settings.FromEmail = txtFromEmail.Text;
                _settings.FromName = txtFromName.Text;
                _settings.Username = txtUsername.Text;
                _settings.Password = pwdPassword.Password;

                EmailReportManager.SaveSettings(_settings);

                if (EmailReportManager.TestConnection())
                {
                    txtTestStatus.Text = "? Connection successful!";
                    txtTestStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtTestStatus.Text = "? Connection failed. Check settings.";
                    txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                txtTestStatus.Text = $"? Error: {ex.Message}";
                txtTestStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFromEmail.Text))
                {
                    MessageBox.Show("From Email Address is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSmtpServer.Text))
                {
                    MessageBox.Show("SMTP Server is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtSmtpPort.Text, out int port) || port <= 0)
                {
                    MessageBox.Show("SMTP Port must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _settings.SmtpServer = txtSmtpServer.Text;
                _settings.SmtpPort = port;
                _settings.UseSSL = chkUseSSL.IsChecked ?? true;
                _settings.FromEmail = txtFromEmail.Text;
                _settings.FromName = txtFromName.Text;
                _settings.Username = txtUsername.Text;
                _settings.Password = pwdPassword.Password;

                EmailReportManager.SaveSettings(_settings);
                MessageBox.Show("Email settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
