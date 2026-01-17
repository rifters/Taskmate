using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Taskmate
{
    public partial class BackupScheduleWindow : Window
    {
        private BackupScheduleSettings settings;

        public BackupScheduleWindow()
        {
            InitializeComponent();
            LoadSettings();
            PopulateTimeComboBoxes();
            UpdateUI();
        }

        private void LoadSettings()
        {
            settings = Settings.LoadBackupScheduleSettings();

            // Set default backup location if not set
            if (string.IsNullOrEmpty(settings.BackupLocation))
            {
                settings.BackupLocation = BackupManager.GetDefaultBackupDirectory();
            }

            chkEnableSchedule.IsChecked = settings.IsEnabled;
            cmbFrequency.SelectedIndex = (int)settings.Frequency;
            cmbDayOfWeek.SelectedIndex = (int)settings.PreferredDay;
            txtBackupLocation.Text = settings.BackupLocation;
            txtRetentionDays.Text = settings.RetentionDays.ToString();
            chkNotifyOnCompletion.IsChecked = settings.NotifyOnCompletion;

            // Set time
            int hour = settings.PreferredTime.Hours;
            bool isPM = hour >= 12;
            if (hour > 12) hour -= 12;
            if (hour == 0) hour = 12;

            cmbHour.SelectedItem = hour.ToString();
            cmbMinute.SelectedItem = settings.PreferredTime.Minutes.ToString("00");
            cmbAmPm.SelectedIndex = isPM ? 1 : 0;

            UpdateLastBackupInfo();
        }

        private void PopulateTimeComboBoxes()
        {
            // Hours
            for (int i = 1; i <= 12; i++)
                cmbHour.Items.Add(i.ToString());
            cmbHour.SelectedIndex = 1; // 2 AM default

            // Minutes
            for (int i = 0; i < 60; i += 15)
                cmbMinute.Items.Add(i.ToString("00"));
            cmbMinute.SelectedIndex = 0;

            cmbAmPm.SelectedIndex = 0; // AM
        }

        private void chkEnableSchedule_Changed(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        private void cmbFrequency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pnlDayOfWeek != null)
            {
                pnlDayOfWeek.Visibility = cmbFrequency.SelectedIndex == 1 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void UpdateUI()
        {
            bool enabled = chkEnableSchedule.IsChecked == true;
            
            cmbFrequency.IsEnabled = enabled;
            cmbDayOfWeek.IsEnabled = enabled;
            cmbHour.IsEnabled = enabled;
            cmbMinute.IsEnabled = enabled;
            cmbAmPm.IsEnabled = enabled;
            txtBackupLocation.IsEnabled = enabled;
            txtRetentionDays.IsEnabled = enabled;
            chkNotifyOnCompletion.IsEnabled = enabled;
        }

        private void UpdateLastBackupInfo()
        {
            if (settings.LastBackupDate.HasValue)
            {
                txtLastBackup.Text = $"Last backup: {settings.LastBackupDate.Value:MMM dd, yyyy h:mm tt}";
            }
            else
            {
                txtLastBackup.Text = "Last backup: Never";
            }

            if (settings.IsEnabled)
            {
                DateTime next = CalculateNextBackupDate();
                txtNextBackup.Text = $"Next backup: {next:MMM dd, yyyy h:mm tt}";
            }
            else
            {
                txtNextBackup.Text = "Next backup: Not scheduled";
            }
        }

        private DateTime CalculateNextBackupDate()
        {
            DateTime now = DateTime.Now;
            DateTime nextBackup = now.Date.Add(settings.PreferredTime);

            switch (settings.Frequency)
            {
                case BackupFrequency.Daily:
                    if (nextBackup <= now)
                        nextBackup = nextBackup.AddDays(1);
                    break;

                case BackupFrequency.Weekly:
                    while (nextBackup.DayOfWeek != settings.PreferredDay || nextBackup <= now)
                        nextBackup = nextBackup.AddDays(1);
                    break;

                case BackupFrequency.Monthly:
                    nextBackup = new DateTime(now.Year, now.Month, 1, 
                        settings.PreferredTime.Hours, 
                        settings.PreferredTime.Minutes, 0);
                    if (nextBackup <= now)
                        nextBackup = nextBackup.AddMonths(1);
                    break;
            }

            return nextBackup;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select backup location",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(txtBackupLocation.Text))
                dialog.SelectedPath = txtBackupLocation.Text;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtBackupLocation.Text = dialog.SelectedPath;
            }
        }

        private void btnTestBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string location = string.IsNullOrEmpty(txtBackupLocation.Text) 
                    ? BackupManager.GetDefaultBackupDirectory() 
                    : txtBackupLocation.Text;

                string backupFile = BackupManager.CreateBackup(location);
                
                MessageBox.Show(
                    $"Test backup completed successfully!\n\nLocation:\n{backupFile}",
                    "Backup Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Backup test failed:\n\n{ex.Message}",
                    "Backup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate
                if (!int.TryParse(txtRetentionDays.Text, out int retention) || retention < 1)
                {
                    MessageBox.Show("Please enter a valid retention period (1 or more days).", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (chkEnableSchedule.IsChecked == true && string.IsNullOrWhiteSpace(txtBackupLocation.Text))
                {
                    MessageBox.Show("Please select a backup location.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save settings
                settings.IsEnabled = chkEnableSchedule.IsChecked == true;
                settings.Frequency = (BackupFrequency)cmbFrequency.SelectedIndex;
                settings.PreferredDay = (DayOfWeek)cmbDayOfWeek.SelectedIndex;
                
                int hour = int.Parse(cmbHour.SelectedItem.ToString());
                int minute = int.Parse(cmbMinute.SelectedItem.ToString());
                bool isPM = cmbAmPm.SelectedIndex == 1;
                
                if (isPM && hour != 12) hour += 12;
                if (!isPM && hour == 12) hour = 0;
                
                settings.PreferredTime = new TimeSpan(hour, minute, 0);
                settings.BackupLocation = txtBackupLocation.Text;
                settings.RetentionDays = retention;
                settings.NotifyOnCompletion = chkNotifyOnCompletion.IsChecked == true;

                Settings.SaveBackupScheduleSettings(settings);

                MessageBox.Show(
                    "Backup schedule settings saved successfully!",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving settings:\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}