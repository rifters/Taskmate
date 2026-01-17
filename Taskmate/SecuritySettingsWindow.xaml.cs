using System.Windows;

namespace Taskmate
{
    public partial class SecuritySettingsWindow : Window
    {
        public SecuritySettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            chkEnableTimeout.IsChecked = Properties.Settings.Default.EnableSessionTimeout;
            txtTimeoutMinutes.Text = Properties.Settings.Default.SessionTimeoutMinutes.ToString();
            chkEnableAudit.IsChecked = Properties.Settings.Default.EnableAuditLog;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtTimeoutMinutes.Text, out int minutes) || minutes < 1 || minutes > 1440)
            {
                MessageBox.Show("Timeout must be between 1 and 1440 minutes.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Properties.Settings.Default.EnableSessionTimeout = chkEnableTimeout.IsChecked == true;
            Properties.Settings.Default.SessionTimeoutMinutes = minutes;
            Properties.Settings.Default.EnableAuditLog = chkEnableAudit.IsChecked == true;
            Properties.Settings.Default.Save();

            MessageBox.Show("Security settings saved!\n\nRestart the application for session timeout changes to take effect.", 
                "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnViewAudit_Click(object sender, RoutedEventArgs e)
        {
            AuditLogger.ViewAuditLog();
        }

        private void btnClearData_Click(object sender, RoutedEventArgs e)
        {
            if (DataManager.ClearAllData())
            {
                Application.Current.Shutdown();
            }
        }
    }
}