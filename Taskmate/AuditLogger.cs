using System;
using System.IO;
using System.Text;

namespace Taskmate
{
    public static class AuditLogger
    {
        private static readonly string AuditLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "audit_log.txt");

        private static readonly object _lockObject = new object();

        public static void Log(string action, string user, string details)
        {
            // Check if audit logging is enabled
            if (!Properties.Settings.Default.EnableAuditLog)
                return;

            try
            {
                lock (_lockObject)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(AuditLogPath)!);

                    var logEntry = new StringBuilder();
                    logEntry.AppendLine($"=== AUDIT LOG ENTRY ===");
                    logEntry.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    logEntry.AppendLine($"Action: {action}");
                    logEntry.AppendLine($"User: {user}");
                    logEntry.AppendLine($"Machine: {Environment.MachineName}");
                    logEntry.AppendLine($"Details: {details}");
                    logEntry.AppendLine($"======================");
                    logEntry.AppendLine();

                    File.AppendAllText(AuditLogPath, logEntry.ToString());
                }
            }
            catch
            {
                // Silently fail - don't disrupt user experience
            }
        }

        public static void LogGroupChange(string action, string groupName, string user)
        {
            Log($"GROUP_{action.ToUpper()}", user, $"Group: {groupName}");
        }

        public static void LogAssignment(string groupName, int peopleCount, int taskCount, string user)
        {
            Log("ASSIGNMENT_CREATED", user, 
                $"Group: {groupName}, People: {peopleCount}, Tasks: {taskCount}");
        }

        public static void LogSettingsChange(string settingName, string oldValue, string newValue, string user)
        {
            Log("SETTINGS_CHANGED", user, 
                $"Setting: {settingName}, Old: {oldValue}, New: {newValue}");
        }

        public static void LogFeatureToggle(string featureName, bool enabled, string user)
        {
            Log("FEATURE_TOGGLE", user, 
                $"Feature: {featureName}, Enabled: {enabled}");
        }

        public static string GetAuditLogPath() => AuditLogPath;

        public static void ViewAuditLog()
        {
            if (File.Exists(AuditLogPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = AuditLogPath,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }
}