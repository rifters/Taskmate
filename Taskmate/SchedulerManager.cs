using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class SchedulerManager
    {
        private static readonly string SchedulerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "scheduled_assignments.json");

        public static List<ScheduledAssignment> GetAllScheduledAssignments()
        {
            try
            {
                if (File.Exists(SchedulerPath))
                {
                    string json = File.ReadAllText(SchedulerPath);
                    var scheduled = JsonSerializer.Deserialize<List<ScheduledAssignment>>(json);
                    return scheduled ?? new List<ScheduledAssignment>();
                }
            }
            catch { }
            
            return new List<ScheduledAssignment>();
        }

        public static void SaveScheduledAssignment(ScheduledAssignment assignment)
        {
            var all = GetAllScheduledAssignments();
            
            var existing = all.FirstOrDefault(a => a.Id == assignment.Id);
            if (existing != null)
            {
                all.Remove(existing);
            }
            
            all.Add(assignment);
            SaveAll(all);
        }

        public static void DeleteScheduledAssignment(string id)
        {
            var all = GetAllScheduledAssignments();
            all.RemoveAll(a => a.Id == id);
            SaveAll(all);
        }

        public static List<ScheduledAssignment> GetUpcomingAssignments(int daysAhead = 7)
        {
            var all = GetAllScheduledAssignments();
            var cutoff = DateTime.Now.AddDays(daysAhead);
            
            return all.Where(a => a.IsEnabled && a.ScheduledDate <= cutoff && a.ScheduledDate >= DateTime.Now)
                      .OrderBy(a => a.ScheduledDate)
                      .ToList();
        }

        private static void SaveAll(List<ScheduledAssignment> assignments)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SchedulerPath)!);
                string json = JsonSerializer.Serialize(assignments, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SchedulerPath, json);
            }
            catch { }
        }
    }
}