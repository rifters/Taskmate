using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class CompletionStatisticsWindow : Window
    {
        public CompletionStatisticsWindow()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                var allAssignments = AssignmentHistoryManager.GetAllAssignments();
                
                if (allAssignments.Count == 0)
                {
                    MessageBox.Show("No assignment history available for statistics.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                    return;
                }

                // Overall Statistics
                int totalAssignments = allAssignments.Count;
                int completeAssignments = allAssignments.Count(a => a.OverallCompletionPercentage >= 100);
                int partialAssignments = allAssignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
                int incompleteAssignments = allAssignments.Count(a => a.OverallCompletionPercentage == 0);
                double averageCompletion = allAssignments.Average(a => a.OverallCompletionPercentage);

                // Display overall stats
                var overallStats = new List<StatItem>
                {
                    new StatItem { Label = "Total Assignments", Value = totalAssignments.ToString(), Percentage = "100%" },
                    new StatItem { Label = "Completed", Value = completeAssignments.ToString(), Percentage = $"{(totalAssignments > 0 ? (completeAssignments / (double)totalAssignments * 100) : 0):F1}%" },
                    new StatItem { Label = "Partial", Value = partialAssignments.ToString(), Percentage = $"{(totalAssignments > 0 ? (partialAssignments / (double)totalAssignments * 100) : 0):F1}%" },
                    new StatItem { Label = "Incomplete", Value = incompleteAssignments.ToString(), Percentage = $"{(totalAssignments > 0 ? (incompleteAssignments / (double)totalAssignments * 100) : 0):F1}%" },
                    new StatItem { Label = "Average Completion", Value = $"{averageCompletion:F1}%", Percentage = "" }
                };

                dgOverallStats.ItemsSource = overallStats;

                // Person-specific statistics
                var personStats = new List<PersonStatItem>();
                var allPeople = allAssignments
                    .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                foreach (var person in allPeople)
                {
                    var personAssignments = allAssignments
                        .Where(a => a.Assignments.Any(ar => ar.Person == person))
                        .SelectMany(a => a.Assignments.Where(ar => ar.Person == person))
                        .ToList();

                    if (personAssignments.Count > 0)
                    {
                        int totalTasks = personAssignments.Sum(a => a.TaskCount);
                        int completedTasks = personAssignments.Sum(a => a.CompletedCount);
                        double completionRate = totalTasks > 0 ? (completedTasks / (double)totalTasks * 100) : 0;

                        personStats.Add(new PersonStatItem
                        {
                            Person = person,
                            TotalAssignments = personAssignments.Count,
                            TotalTasks = totalTasks,
                            CompletedTasks = completedTasks,
                            CompletionRate = completionRate,
                            Status = completionRate >= 100 ? "Complete" : completionRate > 0 ? "Partial" : "Incomplete"
                        });
                    }
                }

                dgPersonStats.ItemsSource = personStats;

                // Task completion analysis
                var taskStats = new List<TaskStatItem>();
                var allTasks = allAssignments
                    .SelectMany(a => a.Assignments.SelectMany(ar => ar.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                foreach (var task in allTasks)
                {
                    int timesAssigned = 0;
                    int timesCompleted = 0;

                    foreach (var assignment in allAssignments)
                    {
                        foreach (var person in assignment.Assignments)
                        {
                            if (person.Tasks.Contains(task, StringComparison.OrdinalIgnoreCase))
                            {
                                timesAssigned++;
                                if (person.CompletedTasks.Contains(task))
                                    timesCompleted++;
                            }
                        }
                    }

                    if (timesAssigned > 0)
                    {
                        taskStats.Add(new TaskStatItem
                        {
                            Task = task,
                            TimesAssigned = timesAssigned,
                            TimesCompleted = timesCompleted,
                            CompletionRate = (timesCompleted / (double)timesAssigned * 100),
                            Status = timesCompleted == timesAssigned ? "Always Done" : timesCompleted == 0 ? "Never Done" : "Sometimes Done"
                        });
                    }
                }

                dgTaskStats.ItemsSource = taskStats;

                // Time-based trend
                var completionByMonth = allAssignments
                    .GroupBy(a => a.Timestamp.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .Select(g => new MonthStatItem
                    {
                        Month = g.Key,
                        Completed = g.Count(a => a.OverallCompletionPercentage >= 100),
                        Partial = g.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100),
                        Incomplete = g.Count(a => a.OverallCompletionPercentage == 0),
                        AverageCompletion = g.Average(a => a.OverallCompletionPercentage)
                    })
                    .ToList();

                dgTrendStats.ItemsSource = completionByMonth;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = GenerateStatisticsReport();
                Clipboard.SetText(stats);
                MessageBox.Show("Statistics copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"CompletionStatistics_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var csv = GenerateCSVReport();
                    System.IO.File.WriteAllText(saveDialog.FileName, csv);
                    MessageBox.Show($"Statistics exported to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"CompletionStatistics_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var allAssignments = AssignmentHistoryManager.GetAllAssignments();
                    ExcelReportGenerator.GenerateCompletionStatisticsExcel(saveDialog.FileName, allAssignments);
                    MessageBox.Show($"Statistics exported to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = $"CompletionStatistics_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var allAssignments = AssignmentHistoryManager.GetAllAssignments();
                    PdfReportGenerator.GenerateStatisticsReport(saveDialog.FileName, allAssignments);
                    MessageBox.Show($"Statistics exported to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateStatisticsReport()
        {
            var stats = $"COMPLETION STATISTICS REPORT\n" +
                       $"Generated: {DateTime.Now:g}\n\n" +
                       $"=== OVERALL STATISTICS ===\n";

            if (dgOverallStats.ItemsSource is List<StatItem> overallStats)
            {
                foreach (var stat in overallStats)
                {
                    stats += $"{stat.Label}: {stat.Value} ({stat.Percentage})\n";
                }
            }

            stats += $"\n=== PERSON STATISTICS ===\n";
            if (dgPersonStats.ItemsSource is List<PersonStatItem> personStats)
            {
                foreach (var person in personStats)
                {
                    stats += $"{person.Person}: {person.CompletedTasks}/{person.TotalTasks} tasks ({person.CompletionRate:F1}%) - {person.Status}\n";
                }
            }

            stats += $"\n=== TASK STATISTICS ===\n";
            if (dgTaskStats.ItemsSource is List<TaskStatItem> taskStats)
            {
                foreach (var task in taskStats)
                {
                    stats += $"{task.Task}: {task.TimesCompleted}/{task.TimesAssigned} times ({task.CompletionRate:F1}%) - {task.Status}\n";
                }
            }

            stats += $"\n=== MONTHLY TRENDS ===\n";
            if (dgTrendStats.ItemsSource is List<MonthStatItem> trendStats)
            {
                foreach (var month in trendStats)
                {
                    stats += $"{month.Month}: Complete={month.Completed}, Partial={month.Partial}, Incomplete={month.Incomplete}, Avg={month.AverageCompletion:F1}%\n";
                }
            }

            return stats;
        }

        private string GenerateCSVReport()
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("COMPLETION STATISTICS REPORT");
            csv.AppendLine($"Generated,{DateTime.Now:g}");
            csv.AppendLine();

            // Overall Statistics
            csv.AppendLine("=== OVERALL STATISTICS ===");
            csv.AppendLine("Metric,Count,Percentage");
            if (dgOverallStats.ItemsSource is List<StatItem> overallStats)
            {
                foreach (var stat in overallStats)
                {
                    csv.AppendLine($"\"{stat.Label}\",{stat.Value},{stat.Percentage}");
                }
            }
            csv.AppendLine();

            // Person Statistics
            csv.AppendLine("=== PERSON STATISTICS ===");
            csv.AppendLine("Person,Total Assignments,Total Tasks,Completed Tasks,Completion %,Status");
            if (dgPersonStats.ItemsSource is List<PersonStatItem> personStats)
            {
                foreach (var person in personStats)
                {
                    csv.AppendLine($"\"{person.Person}\",{person.TotalAssignments},{person.TotalTasks},{person.CompletedTasks},{person.CompletionRate:F1},{person.Status}");
                }
            }
            csv.AppendLine();

            // Task Statistics
            csv.AppendLine("=== TASK STATISTICS ===");
            csv.AppendLine("Task,Times Assigned,Times Completed,Completion %,Status");
            if (dgTaskStats.ItemsSource is List<TaskStatItem> taskStats)
            {
                foreach (var task in taskStats)
                {
                    csv.AppendLine($"\"{task.Task}\",{task.TimesAssigned},{task.TimesCompleted},{task.CompletionRate:F1},{task.Status}");
                }
            }
            csv.AppendLine();

            // Monthly Trends
            csv.AppendLine("=== MONTHLY TRENDS ===");
            csv.AppendLine("Month,Complete,Partial,Incomplete,Average Completion %");
            if (dgTrendStats.ItemsSource is List<MonthStatItem> trendStats)
            {
                foreach (var month in trendStats)
                {
                    csv.AppendLine($"{month.Month},{month.Completed},{month.Partial},{month.Incomplete},{month.AverageCompletion:F1}");
                }
            }

            return csv.ToString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Data classes for display
    public class StatItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Percentage { get; set; }
    }

    public class PersonStatItem
    {
        public string Person { get; set; }
        public int TotalAssignments { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double CompletionRate { get; set; }
        public string Status { get; set; }
    }

    public class TaskStatItem
    {
        public string Task { get; set; }
        public int TimesAssigned { get; set; }
        public int TimesCompleted { get; set; }
        public double CompletionRate { get; set; }
        public string Status { get; set; }
    }

    public class MonthStatItem
    {
        public string Month { get; set; }
        public int Completed { get; set; }
        public int Partial { get; set; }
        public int Incomplete { get; set; }
        public double AverageCompletion { get; set; }
    }
}
