using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Taskmate
{
    public static class CsvExporter
    {
        public static void ExportToCsv(List<AssignmentResult> assignments, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Person,Task Count,Tasks");
            
            foreach (var assignment in assignments)
            {
                string tasks = assignment.Tasks.Replace(",", ";"); // Escape commas
                csv.AppendLine($"\"{assignment.Person}\",{assignment.TaskCount},\"{tasks}\"");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        public static string FormatForClipboard(List<AssignmentResult> assignments)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine("              TASK ASSIGNMENTS");
            sb.AppendLine("═══════════════════════════════════════════════════");
            sb.AppendLine();
            
            foreach (var assignment in assignments)
            {
                sb.AppendLine($"👤 {assignment.Person} ({assignment.TaskCount} tasks)");
                sb.AppendLine(new string('─', 50));
                var taskList = assignment.Tasks.Split(new[] { ", " }, System.StringSplitOptions.None);
                foreach (var task in taskList)
                {
                    sb.AppendLine($"   • {task}");
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }

        public static string FormatPersonTasks(string person, string tasks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Tasks for {person}:");
            sb.AppendLine(new string('─', 40));
            var taskList = tasks.Split(new[] { ", " }, System.StringSplitOptions.None);
            foreach (var task in taskList)
            {
                sb.AppendLine($"• {task}");
            }
            return sb.ToString();
        }
    }
}