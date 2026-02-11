using System;
using System.IO;
using System.Diagnostics;

namespace Taskmate.Utilities
{
    /// <summary>
    /// Centralized logging utility for error and diagnostic information
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "logs");

        private const bool ENABLE_PERFORMANCE_LOGGING = true;

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
            }
            catch
            {
                // Fail silently if we can't create log directory
            }
        }

        /// <summary>
        /// Log an error message to file and debug output
        /// </summary>
        public static void LogError(string message, Exception? ex = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] ERROR: {message}";

                if (ex != null)
                {
                    logMessage += $"\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                }

                WriteToFile(logMessage);
                Debug.WriteLine(logMessage);
            }
            catch
            {
                // Silently fail if logging fails
                Debug.WriteLine($"Failed to log error: {message}");
            }
        }

        /// <summary>
        /// Log a warning message to file and debug output
        /// </summary>
        public static void LogWarning(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] WARNING: {message}";
                WriteToFile(logMessage);
                Debug.WriteLine(logMessage);
            }
            catch
            {
                Debug.WriteLine($"Failed to log warning: {message}");
            }
        }

        /// <summary>
        /// Log an informational message to file and debug output
        /// </summary>
        public static void LogInfo(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] INFO: {message}";
                WriteToFile(logMessage);
                Debug.WriteLine(logMessage);
            }
            catch
            {
                Debug.WriteLine($"Failed to log info: {message}");
            }
        }

        /// <summary>
        /// Log a performance metric with operation name and duration in milliseconds
        /// </summary>
        public static void LogPerformance(string operationName, long elapsedMilliseconds, int itemCount = 0)
        {
            if (!ENABLE_PERFORMANCE_LOGGING)
            {
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string itemInfo = itemCount > 0 ? $" ({itemCount} items)" : "";
                string logMessage = $"[{timestamp}] PERF: {operationName} took {elapsedMilliseconds}ms{itemInfo}";
                
                // Only log to debug, not file (to avoid clutter)
                Debug.WriteLine(logMessage);
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Write message to log file
        /// </summary>
        private static void WriteToFile(string message)
        {
            try
            {
                string logFile = Path.Combine(LogDirectory, $"taskmate_{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(logFile, message + Environment.NewLine);
            }
            catch
            {
                // If file logging fails, at least we logged to Debug
            }
        }

        /// <summary>
        /// Clear old log files (older than specified days)
        /// </summary>
        public static void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var files = Directory.GetFiles(LogDirectory, "taskmate_*.log");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Fail silently
            }
        }
    }
}
