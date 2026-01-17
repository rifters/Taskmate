using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public class TaskAssignmentRecord
    {
        public string Person { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public int TimesAssigned { get; set; } = 1;
    }

    public static class RotationTracker
    {
        private static readonly string TrackerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "rotation_tracker.json");

        private static List<TaskAssignmentRecord> records = new List<TaskAssignmentRecord>();

        static RotationTracker()
        {
            LoadRecords();
        }

        public static void RecordAssignment(string person, string task)
        {
            var existing = records.FirstOrDefault(r => 
                r.Person.Equals(person, StringComparison.OrdinalIgnoreCase) && 
                r.Task.Equals(task, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.TimesAssigned++;
                existing.AssignedDate = DateTime.Now;
            }
            else
            {
                records.Add(new TaskAssignmentRecord
                {
                    Person = person,
                    Task = task,
                    AssignedDate = DateTime.Now,
                    TimesAssigned = 1
                });
            }

            SaveRecords();
        }

        public static void RecordAssignments(Dictionary<string, List<string>> assignments)
        {
            foreach (var assignment in assignments)
            {
                foreach (var task in assignment.Value)
                {
                    RecordAssignment(assignment.Key, task);
                }
            }
        }

        public static Dictionary<string, int> GetTaskCountForPerson(string person, int daysBack = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            return records
                .Where(r => r.Person.Equals(person, StringComparison.OrdinalIgnoreCase) && r.AssignedDate >= cutoffDate)
                .GroupBy(r => r.Task)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TimesAssigned));
        }

        public static Dictionary<string, int> GetPersonCountForTask(string task, int daysBack = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            return records
                .Where(r => r.Task.Equals(task, StringComparison.OrdinalIgnoreCase) && r.AssignedDate >= cutoffDate)
                .GroupBy(r => r.Person)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TimesAssigned));
        }

        public static List<string> SuggestRotation(string task, List<string> availablePeople)
        {
            // Get who has done this task least recently
            var taskCounts = GetPersonCountForTask(task, 30);
            
            return availablePeople
                .OrderBy(p => taskCounts.ContainsKey(p) ? taskCounts[p] : 0)
                .ThenBy(p => Guid.NewGuid()) // Random tiebreaker
                .ToList();
        }

        public static string GetRotationSuggestion(string task, List<string> availablePeople)
        {
            var sorted = SuggestRotation(task, availablePeople);
            if (sorted.Count == 0)
                return "No suggestions available";

            var counts = GetPersonCountForTask(task, 30);
            var suggested = sorted.First();
            int count = counts.ContainsKey(suggested) ? counts[suggested] : 0;
            
            return count == 0 
                ? $"Suggest: {suggested} (hasn't done this task recently)"
                : $"Suggest: {suggested} (done {count} time(s) in last 30 days)";
        }

        public static void ClearOldRecords(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            records.RemoveAll(r => r.AssignedDate < cutoffDate);
            SaveRecords();
        }

        private static void LoadRecords()
        {
            try
            {
                if (File.Exists(TrackerPath))
                {
                    string json = File.ReadAllText(TrackerPath);
                    var loaded = JsonSerializer.Deserialize<List<TaskAssignmentRecord>>(json);
                    if (loaded != null)
                        records = loaded;
                }
            }
            catch
            {
                records = new List<TaskAssignmentRecord>();
            }
        }

        private static void SaveRecords()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TrackerPath)!);
                string json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TrackerPath, json);
            }
            catch { }
        }

        public static Dictionary<string, Dictionary<string, int>> GetFullRotationReport(List<string> people, List<string> tasks, int daysBack = 30)
        {
            var report = new Dictionary<string, Dictionary<string, int>>();
            
            foreach (var person in people)
            {
                report[person] = GetTaskCountForPerson(person, daysBack);
            }
            
            return report;
        }
    }
}