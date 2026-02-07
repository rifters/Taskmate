using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Taskmate
{
    /// <summary>
    /// Manages scheduled report generation and delivery
    /// </summary>
    public class ScheduledReportManager
    {
        private static readonly string SchedulesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "Schedules");

        private static readonly string ScheduleConfigFile = Path.Combine(SchedulesFolder, "schedules.json");
        private static Timer _schedulerTimer;
        private static List<ReportSchedule> _activeSchedules;

        /// <summary>
        /// Initialize the scheduler
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory(SchedulesFolder);
                LoadSchedules();
                StartScheduler();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing scheduler: {ex.Message}");
            }
        }

        /// <summary>
        /// Start the background scheduler
        /// </summary>
        public static void StartScheduler()
        {
            if (_schedulerTimer != null)
                _schedulerTimer.Dispose();

            // Check schedules every minute
            _schedulerTimer = new Timer(CheckSchedules, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Stop the scheduler
        /// </summary>
        public static void StopScheduler()
        {
            _schedulerTimer?.Dispose();
        }

        /// <summary>
        /// Check if any schedules need to run
        /// </summary>
        private static void CheckSchedules(object state)
        {
            try
            {
                if (_activeSchedules == null || _activeSchedules.Count == 0)
                    return;

                var now = DateTime.Now;

                foreach (var schedule in _activeSchedules.Where(s => s.IsEnabled))
                {
                    if (ShouldRunSchedule(schedule, now))
                    {
                        Task.Run(() => ExecuteSchedule(schedule));
                        schedule.LastExecuted = now;
                        SaveSchedules();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking schedules: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine if a schedule should run
        /// </summary>
        private static bool ShouldRunSchedule(ReportSchedule schedule, DateTime now)
        {
            if (schedule.LastExecuted == null)
            {
                // Never run, check if time matches
                return CheckTimeMatch(schedule, now);
            }

            var timeSinceLastRun = now - schedule.LastExecuted.Value;

            return schedule.Frequency switch
            {
                ReportFrequency.Daily => timeSinceLastRun.TotalHours >= 23 && CheckTimeMatch(schedule, now),
                ReportFrequency.Weekly => timeSinceLastRun.TotalDays >= 6 && 
                                         now.DayOfWeek == schedule.DayOfWeek &&
                                         CheckTimeMatch(schedule, now),
                ReportFrequency.Monthly => timeSinceLastRun.TotalDays >= 28 && 
                                          now.Day == schedule.DayOfMonth &&
                                          CheckTimeMatch(schedule, now),
                _ => false
            };
        }

        /// <summary>
        /// Check if current time matches schedule time
        /// </summary>
        private static bool CheckTimeMatch(ReportSchedule schedule, DateTime now)
        {
            var scheduleTime = schedule.Time;
            return now.Hour == scheduleTime.Hours && now.Minute >= scheduleTime.Minutes;
        }

        /// <summary>
        /// Execute a scheduled report
        /// </summary>
        private static void ExecuteSchedule(ReportSchedule schedule)
        {
            try
            {
                var assignments = AssignmentHistoryManager.GetAllAssignments();

                if (schedule.ReportType == ReportType.Statistics || schedule.ReportType == ReportType.Both)
                {
                    GenerateStatisticsReport(schedule, assignments);
                }

                if (schedule.ReportType == ReportType.Dashboard || schedule.ReportType == ReportType.Both)
                {
                    GenerateDashboardReport(schedule, assignments);
                }

                schedule.LastExecuted = DateTime.Now;
                SaveSchedules();

                // Log execution
                LogExecution(schedule, true, "Report generated successfully");
            }
            catch (Exception ex)
            {
                LogExecution(schedule, false, ex.Message);
            }
        }

        /// <summary>
        /// Generate statistics report file
        /// </summary>
        private static void GenerateStatisticsReport(ReportSchedule schedule, List<PersistentAssignment> assignments)
        {
            var fileName = $"Statistics_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var filePath = Path.Combine(schedule.OutputFolder, fileName);

            ExcelReportGenerator.GenerateCompletionStatisticsExcel(filePath, assignments);
        }

        /// <summary>
        /// Generate dashboard report file
        /// </summary>
        private static void GenerateDashboardReport(ReportSchedule schedule, List<PersistentAssignment> assignments)
        {
            var fileName = $"Dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(schedule.OutputFolder, fileName);

            var report = GenerateDashboardReportText(assignments);
            File.WriteAllText(filePath, report);
        }

        /// <summary>
        /// Generate dashboard report as text
        /// </summary>
        private static string GenerateDashboardReportText(List<PersistentAssignment> assignments)
        {
            var text = $"PERFORMANCE DASHBOARD REPORT\n";
            text += $"Generated: {DateTime.Now:g}\n";
            text += $"Period: {assignments.Min(a => a.Timestamp):g} to {assignments.Max(a => a.Timestamp):g}\n";
            text += new string('=', 50) + "\n\n";

            // Overall stats
            var total = assignments.Count;
            var complete = assignments.Count(a => a.OverallCompletionPercentage >= 100);
            var partial = assignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
            var incomplete = assignments.Count(a => a.OverallCompletionPercentage == 0);
            var avgCompletion = assignments.Average(a => a.OverallCompletionPercentage);

            text += "KEY METRICS\n";
            text += new string('-', 50) + "\n";
            text += $"Total Assignments: {total}\n";
            text += $"Fully Completed: {complete} ({(complete / (double)total * 100):F1}%)\n";
            text += $"Partial: {partial} ({(partial / (double)total * 100):F1}%)\n";
            text += $"Incomplete: {incomplete} ({(incomplete / (double)total * 100):F1}%)\n";
            text += $"Average Completion: {avgCompletion:F1}%\n\n";

            return text;
        }

        /// <summary>
        /// Log schedule execution
        /// </summary>
        private static void LogExecution(ReportSchedule schedule, bool success, string message)
        {
            var logFolder = Path.Combine(SchedulesFolder, "logs");
            Directory.CreateDirectory(logFolder);

            var logFile = Path.Combine(logFolder, "execution.log");
            var logEntry = $"[{DateTime.Now:g}] {schedule.Name}: {(success ? "SUCCESS" : "FAILED")} - {message}\n";

            try
            {
                File.AppendAllText(logFile, logEntry);
            }
            catch { }
        }

        /// <summary>
        /// Get all schedules
        /// </summary>
        public static List<ReportSchedule> GetSchedules()
        {
            return _activeSchedules ?? new List<ReportSchedule>();
        }

        /// <summary>
        /// Add a new schedule
        /// </summary>
        public static void AddSchedule(ReportSchedule schedule)
        {
            _activeSchedules ??= new List<ReportSchedule>();
            schedule.Id = Guid.NewGuid().ToString();
            _activeSchedules.Add(schedule);
            SaveSchedules();
        }

        /// <summary>
        /// Update an existing schedule
        /// </summary>
        public static void UpdateSchedule(ReportSchedule schedule)
        {
            var existing = _activeSchedules?.FirstOrDefault(s => s.Id == schedule.Id);
            if (existing != null)
            {
                var index = _activeSchedules.IndexOf(existing);
                _activeSchedules[index] = schedule;
                SaveSchedules();
            }
        }

        /// <summary>
        /// Delete a schedule
        /// </summary>
        public static void DeleteSchedule(string scheduleId)
        {
            _activeSchedules?.RemoveAll(s => s.Id == scheduleId);
            SaveSchedules();
        }

        /// <summary>
        /// Load schedules from file
        /// </summary>
        private static void LoadSchedules()
        {
            try
            {
                if (File.Exists(ScheduleConfigFile))
                {
                    var json = File.ReadAllText(ScheduleConfigFile);
                    _activeSchedules = JsonSerializer.Deserialize<List<ReportSchedule>>(json) ?? new List<ReportSchedule>();
                }
                else
                {
                    _activeSchedules = new List<ReportSchedule>();
                }
            }
            catch
            {
                _activeSchedules = new List<ReportSchedule>();
            }
        }

        /// <summary>
        /// Save schedules to file
        /// </summary>
        private static void SaveSchedules()
        {
            try
            {
                Directory.CreateDirectory(SchedulesFolder);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_activeSchedules, options);
                File.WriteAllText(ScheduleConfigFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving schedules: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Report schedule configuration
    /// </summary>
    public class ReportSchedule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonPropertyName("frequency")]
        public ReportFrequency Frequency { get; set; }

        [JsonPropertyName("time")]
        public TimeSpan Time { get; set; }

        [JsonPropertyName("dayOfWeek")]
        public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;

        [JsonPropertyName("dayOfMonth")]
        public int DayOfMonth { get; set; } = 1;

        [JsonPropertyName("reportType")]
        public ReportType ReportType { get; set; }

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; }

        [JsonPropertyName("sendEmail")]
        public bool SendEmail { get; set; }

        [JsonPropertyName("emailRecipients")]
        public List<string> EmailRecipients { get; set; } = new List<string>();

        [JsonPropertyName("lastExecuted")]
        public DateTime? LastExecuted { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Report frequency options
    /// </summary>
    public enum ReportFrequency
    {
        Daily,
        Weekly,
        Monthly
    }

    /// <summary>
    /// Report type options
    /// </summary>
    public enum ReportType
    {
        Statistics,
        Dashboard,
        Both
    }
}
