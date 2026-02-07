using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class AssignmentHistoryManager
    {
        private static readonly string HistoryFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "History");

        static AssignmentHistoryManager()
        {
            Directory.CreateDirectory(HistoryFolder);
        }

        public static void SaveAssignment(PersistentAssignment assignment)
        {
            // Organize by year/month folders
            string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
            string folderPath = Path.Combine(HistoryFolder, yearMonth);
            Directory.CreateDirectory(folderPath);

            string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
            string filePath = Path.Combine(folderPath, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(assignment, options);
            File.WriteAllText(filePath, json);
        }

        public static List<PersistentAssignment> GetAllAssignments()
        {
            var assignments = new List<PersistentAssignment>();

            if (!Directory.Exists(HistoryFolder))
                return assignments;

            foreach (var folder in Directory.GetDirectories(HistoryFolder))
            {
                foreach (var file in Directory.GetFiles(folder, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var assignment = JsonSerializer.Deserialize<PersistentAssignment>(json);
                        if (assignment != null)
                            assignments.Add(assignment);
                    }
                    catch { }
                }
            }

            return assignments.OrderByDescending(a => a.Timestamp).ToList();
        }

        public static List<PersistentAssignment> GetAssignmentsByDateRange(DateTime start, DateTime end)
        {
            return GetAllAssignments()
                .Where(a => a.Timestamp >= start && a.Timestamp <= end)
                .ToList();
        }

        public static List<PersistentAssignment> GetAssignmentsByTag(string tag)
        {
            return GetAllAssignments()
                .Where(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static List<PersistentAssignment> SearchAssignments(string searchTerm)
        {
            return GetAllAssignments()
                .Where(a => 
                    a.Tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.Assignments.Any(p => p.Person.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static Dictionary<string, int> GetPersonTaskCount(string personName, DateTime start, DateTime end)
        {
            var taskCounts = new Dictionary<string, int>();
            var assignments = GetAssignmentsByDateRange(start, end);

            foreach (var assignment in assignments)
            {
                var personAssignment = assignment.Assignments
                    .FirstOrDefault(a => a.Person.Equals(personName, StringComparison.OrdinalIgnoreCase));

                if (personAssignment != null)
                {
                    var tasks = personAssignment.Tasks.Split(new[] { ", " }, StringSplitOptions.None);
                    foreach (var task in tasks)
                    {
                        if (taskCounts.ContainsKey(task))
                            taskCounts[task]++;
                        else
                            taskCounts[task] = 1;
                    }
                }
            }

            return taskCounts;
        }


        public static void DeleteAssignment(string id)
        {
            var assignment = GetAllAssignments().FirstOrDefault(a => a.Id == id);
            if (assignment != null)
            {
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        public static void DeleteMultipleAssignments(List<string> ids)
        {
            foreach (var id in ids)
            {
                DeleteAssignment(id);
            }
        }

        public static void DeleteAssignmentsByDateRange(DateTime startDate, DateTime endDate)
        {
            var assignmentsToDelete = GetAssignmentsByDateRange(startDate, endDate);
            var ids = assignmentsToDelete.Select(a => a.Id).ToList();
            DeleteMultipleAssignments(ids);
        }

        public static List<string> GetAllTags()
        {
            return GetAllAssignments()
                .Select(a => a.Tag)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>
        /// Updates completion status for an assignment and saves it back to file
        /// </summary>
        public static void UpdateAssignmentCompletion(PersistentAssignment assignment)
        {
            if (assignment == null) return;

            try
            {
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                // Update the timestamp to track when completion was last updated
                assignment.CompletionUpdatedAt = DateTime.Now;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(assignment, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update assignment completion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get completion statistics for all assignments
        /// </summary>
        public static Dictionary<string, object> GetCompletionStatistics()
        {
            var allAssignments = GetAllAssignments();
            
            return new Dictionary<string, object>
            {
                ["TotalAssignments"] = allAssignments.Count,
                ["CompleteAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage >= 100),
                ["PartialAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100),
                ["IncompleteAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage == 0),
                ["AverageCompletion"] = allAssignments.Count > 0 ? allAssignments.Average(a => a.OverallCompletionPercentage) : 0
            };
        }
    }
}