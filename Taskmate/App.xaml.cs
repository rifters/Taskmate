using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Taskmate.Properties;

namespace Taskmate
{
   public partial class App : Application
   {
        private DispatcherTimer? _backupTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create settings if they don't exist
            if (string.IsNullOrEmpty(Taskmate.Properties.Settings.Default.ThemeMode))
            {

               Taskmate.Properties.Settings.Default.ThemeMode = "System";

               Taskmate.Properties.Settings.Default.Save();
            }

            ThemeManager.Initialize();

            // Start backup scheduler
            InitializeBackupScheduler();
        }

        private void InitializeBackupScheduler()
        {
            // Check for scheduled backups every hour
            _backupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(1)
            };
            _backupTimer.Tick += BackupTimer_Tick;
            _backupTimer.Start();

            // Also check immediately on startup
            CheckAndExecuteScheduledBackup();
        }

        private void BackupTimer_Tick(object? sender, EventArgs e)
        {
            CheckAndExecuteScheduledBackup();
        }

        public void CheckAndExecuteScheduledBackup()
        {
            try
            {
                var settings = BackupScheduleSettings.Load();

                if (!settings.IsEnabled)
                    return;

                if (ShouldRunBackup(settings))
                {
                    ExecuteScheduledBackup(settings);
                }

                // Clean up old backups
                CleanupOldBackups(settings);
            }
            catch (Exception ex)
            {
                // Log error silently - don't disrupt user
                AuditLogger.Log("BACKUP_ERROR", Environment.UserName, 
                    $"Scheduled backup check failed: {ex.Message}");
            }
        }

        private bool ShouldRunBackup(BackupScheduleSettings settings)
        {
            DateTime now = DateTime.Now;

            // If never backed up, run it
            if (!settings.LastBackupDate.HasValue)
                return true;

            DateTime lastBackup = settings.LastBackupDate.Value;
            DateTime nextScheduled = CalculateNextBackupDate(settings, lastBackup);

            return now >= nextScheduled;
        }

        private DateTime CalculateNextBackupDate(BackupScheduleSettings settings, DateTime lastBackup)
        {
            DateTime next = lastBackup.Date.Add(settings.PreferredTime);

            switch (settings.Frequency)
            {
                case BackupFrequency.Daily:
                    next = lastBackup.AddDays(1).Date.Add(settings.PreferredTime);
                    break;

                case BackupFrequency.Weekly:
                    next = lastBackup.AddDays(1).Date.Add(settings.PreferredTime);
                    while (next.DayOfWeek != settings.PreferredDay)
                        next = next.AddDays(1);
                    break;

                case BackupFrequency.Monthly:
                    next = lastBackup.AddMonths(1);
                    next = new DateTime(next.Year, next.Month, 1, 
                        settings.PreferredTime.Hours, 
                        settings.PreferredTime.Minutes, 0);
                    break;
            }

            return next;
        }

        private void ExecuteScheduledBackup(BackupScheduleSettings settings)
        {
            try
            {
                string location = string.IsNullOrEmpty(settings.BackupLocation)
                    ? BackupManager.GetDefaultBackupDirectory()
                    : settings.BackupLocation;

                // Generate backup file path with timestamp
                string fileName = $"TaskAssigner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                string backupFile = Path.Combine(location, fileName);

                // Ensure directory exists
                Directory.CreateDirectory(location);

                bool backupSuccess = BackupManager.CreateBackup(backupFile);

                // Update last backup time only if backup succeeded
                if (backupSuccess)
                {
                    settings.LastBackupDate = DateTime.Now;
                    Taskmate.Properties.Settings.SaveBackupScheduleSettings(settings);

                    // Log success
                    AuditLogger.Log("SCHEDULED_BACKUP", Environment.UserName, 
                        $"Automatic backup completed at: {backupFile}");

                    // Show notification if enabled
                    if (settings.NotifyOnCompletion)
                    {
                        ShowBackupNotification(backupFile);
                    }
                }
                else
                {
                    throw new Exception("BackupManager.CreateBackup returned false.");
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Log("BACKUP_ERROR", Environment.UserName, 
                    $"Scheduled backup failed: {ex.Message}");

                // Show error notification
                MessageBox.Show(
                    $"Scheduled backup failed:\n\n{ex.Message}\n\n" +
                    "Please check your backup settings.",
                    "Backup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ShowBackupNotification(string backupFile)
        {
            // Check if notifications are available
            var notificationSettings = Taskmate.Properties.Settings.Default;

            // Replace 'EnableToast' with a check for MessageBox fallback only
            // or add your own logic if you have a different notification setting
            // For now, always use MessageBox as fallback
            Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(
                    $"Automatic backup completed successfully!\n\n{backupFile}",
                    "Backup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        private void CleanupOldBackups(BackupScheduleSettings settings)
        {
            try
            {
                string location = string.IsNullOrEmpty(settings.BackupLocation)
                    ? BackupManager.GetDefaultBackupDirectory()
                    : settings.BackupLocation;

                if (!Directory.Exists(location))
                    return;

                DateTime cutoffDate = DateTime.Now.AddDays(-settings.RetentionDays);

                var files = Directory.GetFiles(location, "TaskAssigner_Backup_*.zip");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        AuditLogger.Log("BACKUP_CLEANUP", Environment.UserName, 
                            $"Deleted old backup: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - cleanup is not critical
                AuditLogger.Log("BACKUP_CLEANUP_ERROR", Environment.UserName, 
                    $"Failed to cleanup old backups: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _backupTimer?.Stop();
            ThemeManager.Cleanup();
            base.OnExit(e);
        }
    }
}