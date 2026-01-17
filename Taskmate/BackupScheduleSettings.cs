using System;

namespace Taskmate
{
    public partial class BackupScheduleSettings
    {
        public bool IsEnabled { get; set; }
        public BackupFrequency Frequency { get; set; } = BackupFrequency.Weekly;
        public DayOfWeek PreferredDay { get; set; } = DayOfWeek.Sunday;
        public TimeSpan PreferredTime { get; set; } = new TimeSpan(2, 0, 0); // 2 AM
        public DateTime? LastBackupDate { get; set; }
        public string BackupLocation { get; set; } = string.Empty;
        public int RetentionDays { get; set; } = 30; // Keep backups for 30 days
        public bool NotifyOnCompletion { get; set; } = true;
    }

    public enum BackupFrequency
    {
        Daily,
        Weekly,
        Monthly
    }

    public partial class BackupScheduleSettings
    {
        public static BackupScheduleSettings Load()
        {
            // TODO: Replace with actual loading logic (e.g., from file, settings, or database)
            // For now, return a new instance with default values
            return new BackupScheduleSettings
            {
                IsEnabled = false,
                Frequency = BackupFrequency.Daily,
                PreferredDay = DayOfWeek.Monday,
                PreferredTime = TimeSpan.FromHours(2),
                LastBackupDate = null,
                BackupLocation = string.Empty,
                RetentionDays = 30,
                NotifyOnCompletion = false
            };
        }
    }
}