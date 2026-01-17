using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class AnalyticsDashboardWindow : Window
    {
        public class TopContributor
        {
            public int Rank { get; set; }
            public string Person { get; set; } = string.Empty;
            public int TotalTasks { get; set; }
            public int AssignmentCount { get; set; }
        }

        public class FairnessData
        {
            public string Person { get; set; } = string.Empty;
            public int TotalTasks { get; set; }
            public double AvgTasks { get; set; }
            public string FairnessScore { get; set; } = string.Empty;
        }

        public class TaskFrequencyData
        {
            public string TaskName { get; set; } = string.Empty;
            public int Count { get; set; }
            public int UniquePeople { get; set; }
            public DateTime LastDate { get; set; }
        }

        public AnalyticsDashboardWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize window: {ex.Message}", "Initialization Error");
                throw;
            }
            
            // Only load analytics if window is properly initialized
            if (IsInitialized)
            {
                Loaded += (s, e) => LoadAnalytics(7);
            }
        }

        private void LoadAnalytics(int daysBack = 7)
        {
            try
            {
                var assignments = AssignmentHistoryManager.GetAllAssignments();
                
                // Handle null or empty case
                if (assignments == null || assignments.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }
                
                assignments = assignments
                    .Where(a => a.Timestamp >= DateTime.Now.AddDays(-daysBack))
                    .ToList();

                if (assignments.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }

                // Summary stats - with null checks
                if (txtTotalAssignments != null)
                    txtTotalAssignments.Text = assignments.Count.ToString();
                
                var totalTasks = assignments.Sum(a => a.Assignments?.Sum(x => x.TaskCount) ?? 0);
                if (txtTotalTasks != null)
                    txtTotalTasks.Text = totalTasks.ToString();
                
                var uniquePeople = assignments
                    .Where(a => a.Assignments != null)
                    .SelectMany(a => a.Assignments.Select(x => x.Person))
                    .Distinct()
                    .Count();
                if (txtActivePeople != null)
                    txtActivePeople.Text = uniquePeople.ToString();
                
                // Calculate fairness score (0-100%, where 100% is perfectly fair distribution)
                if (txtAvgFairness != null)
                {
                    var taskCounts = assignments
                        .Where(a => a.Assignments != null)
                        .SelectMany(a => a.Assignments)
                        .GroupBy(a => a.Person)
                        .Select(g => g.Sum(x => x.TaskCount))
                        .ToList();
    
                    if (taskCounts.Count > 1)
                    {
                        double avgTasks = taskCounts.Average();
                        double variance = taskCounts.Average(count => Math.Pow(count - avgTasks, 2));
                        double stdDev = Math.Sqrt(variance);
                        
                        // Calculate coefficient of variation and convert to fairness score
                        double cv = avgTasks > 0 ? (stdDev / avgTasks) : 0;
                        double fairnessScore = Math.Max(0, Math.Min(100, 100 - (cv * 100)));
                        
                        txtAvgFairness.Text = $"{fairnessScore:F0}%";
                    }
                    else
                    {
                        txtAvgFairness.Text = "100%"; // Only one person = perfectly fair
                    }
                }

                // Top Contributors
                var contributors = assignments
                    .Where(a => a.Assignments != null)
                    .SelectMany(a => a.Assignments)
                    .GroupBy(a => a.Person)
                    .Select(g => new
                    {
                        Person = g.Key,
                        TotalTasks = g.Sum(x => x.TaskCount),
                        AssignmentCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalTasks)
                    .Take(10)
                    .Select((x, i) => new TopContributor
                    {
                        Rank = i + 1,
                        Person = x.Person,
                        TotalTasks = x.TotalTasks,
                        AssignmentCount = x.AssignmentCount
                    })
                    .ToList();

                if (dgTopContributors != null)
                    dgTopContributors.ItemsSource = contributors;

                // Fairness data - calculate individual fairness scores
                if (dgFairness != null)
                {
                    var taskCounts = assignments
                        .Where(a => a.Assignments != null)
                        .SelectMany(a => a.Assignments)
                        .GroupBy(a => a.Person)
                        .Select(g => g.Sum(x => x.TaskCount))
                        .ToList();
    
                    double overallAvg = taskCounts.Count > 0 ? taskCounts.Average() : 0;
    
                    var fairnessData = assignments
                        .Where(a => a.Assignments != null)
                        .SelectMany(a => a.Assignments)
                        .GroupBy(a => a.Person)
                        .Select(g => new
                        {
                            Person = g.Key,
                            TotalTasks = g.Sum(x => x.TaskCount),
                            AvgTasks = g.Average(x => x.TaskCount)
                        })
                        .Select(x => new FairnessData
                        {
                            Person = x.Person,
                            TotalTasks = x.TotalTasks,
                            AvgTasks = x.AvgTasks,
                            FairnessScore = CalculatePersonFairnessScore(x.TotalTasks, overallAvg)
                        })
                        .OrderByDescending(x => x.TotalTasks)
                        .ToList();

                    dgFairness.ItemsSource = fairnessData;
                }

                // Task frequency - calculate from assignments
                if (dgTaskFrequency != null)
                {
                    var taskFrequency = assignments
                        .Where(a => a.Assignments != null)
                        .SelectMany(a => a.Assignments.Select(x => new { Assignment = a, Person = x.Person, Tasks = x.Tasks }))
                        .SelectMany(x => x.Tasks.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(task => new { Task = task, Person = x.Person, Date = x.Assignment.Timestamp }))
                        .GroupBy(x => x.Task)
                        .Select(g => new TaskFrequencyData
                        {
                            TaskName = g.Key,
                            Count = g.Count(),
                            UniquePeople = g.Select(x => x.Person).Distinct().Count(),
                            LastDate = g.Max(x => x.Date)
                        })
                        .OrderByDescending(x => x.Count)
                        .Take(20)
                        .ToList();
    
                    dgTaskFrequency.ItemsSource = taskFrequency;
                }

                DrawTaskDistributionChart(assignments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading analytics: {ex.Message}\n\nDetails: {ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowEmptyState();
            }
        }

        private void ShowEmptyState()
        {
            if (txtTotalAssignments != null) txtTotalAssignments.Text = "0";
            if (txtTotalTasks != null) txtTotalTasks.Text = "0";
            if (txtActivePeople != null) txtActivePeople.Text = "0";
            if (txtAvgFairness != null) txtAvgFairness.Text = "N/A";

            if (dgTopContributors != null) dgTopContributors.ItemsSource = new List<TopContributor>();
            if (dgFairness != null) dgFairness.ItemsSource = new List<FairnessData>();
            if (dgTaskFrequency != null) dgTaskFrequency.ItemsSource = new List<TaskFrequencyData>();
        }

        private void cmbDateRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDateRange == null || cmbDateRange.SelectedIndex < 0)
                return;
                
            if (cmbDateRange.SelectedIndex == 0)
                LoadAnalytics(7);
            else if (cmbDateRange.SelectedIndex == 1)
                LoadAnalytics(30);
            else if (cmbDateRange.SelectedIndex == 2)
                LoadAnalytics(90);
            else
                LoadAnalytics(3650); // ~10 years
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            cmbDateRange_SelectionChanged(sender, null!);
        }

        private void btnExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Analytics Report",
                    Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"Analytics_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var report = new System.Text.StringBuilder();
                    
                    // Header
                    report.AppendLine("PERFORMANCE ANALYTICS REPORT");
                    report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    report.AppendLine($"Date Range: {GetCurrentDateRangeText()}");
                    report.AppendLine();
                    
                    // Summary Statistics
                    report.AppendLine("SUMMARY STATISTICS");
                    report.AppendLine("Metric,Value");
                    report.AppendLine($"Total Assignments,{txtTotalAssignments?.Text ?? "0"}");
                    report.AppendLine($"Total Tasks,{txtTotalTasks?.Text ?? "0"}");
                    report.AppendLine($"Active People,{txtActivePeople?.Text ?? "0"}");
                    report.AppendLine($"Average Fairness,{txtAvgFairness?.Text ?? "N/A"}");
                    report.AppendLine();
                    
                    // Top Contributors
                    report.AppendLine("TOP CONTRIBUTORS");
                    report.AppendLine("Rank,Person,Total Tasks,Assignment Count");
                    if (dgTopContributors?.ItemsSource is IEnumerable<TopContributor> contributors)
                    {
                        foreach (var contributor in contributors)
                        {
                            report.AppendLine($"{contributor.Rank},{contributor.Person},{contributor.TotalTasks},{contributor.AssignmentCount}");
                        }
                    }
                    report.AppendLine();
                    
                    // Fairness Analysis
                    report.AppendLine("FAIRNESS ANALYSIS");
                    report.AppendLine("Person,Total Tasks,Average Tasks per Assignment,Fairness Score");
                    if (dgFairness?.ItemsSource is IEnumerable<FairnessData> fairnessData)
                    {
                        foreach (var data in fairnessData)
                        {
                            report.AppendLine($"{data.Person},{data.TotalTasks},{data.AvgTasks:F2},{data.FairnessScore}");
                        }
                    }
                    report.AppendLine();
                    
                    // Task Frequency
                    report.AppendLine("TASK FREQUENCY");
                    report.AppendLine("Task Name,Count,Unique People,Last Assigned");
                    if (dgTaskFrequency?.ItemsSource is IEnumerable<TaskFrequencyData> taskFrequency)
                    {
                        foreach (var task in taskFrequency)
                        {
                            report.AppendLine($"{task.TaskName},{task.Count},{task.UniquePeople},{task.LastDate:yyyy-MM-dd}");
                        }
                    }
                    
                    // Write to file
                    System.IO.File.WriteAllText(saveDialog.FileName, report.ToString());
                    
                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", 
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Ask to open the file
                    var result = MessageBox.Show("Would you like to open the report now?", 
                        "Open Report", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export report: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCurrentDateRangeText()
        {
            if (cmbDateRange == null || cmbDateRange.SelectedIndex < 0)
                return "Unknown";
                
            return cmbDateRange.SelectedIndex switch
            {
                0 => "Last 7 Days",
                1 => "Last 30 Days",
                2 => "Last 90 Days",
                3 => "All Time",
                _ => "Unknown"
            };
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DrawTaskDistributionChart(List<PersistentAssignment> assignments)
        {
            if (chartCanvas == null)
                return;
                
            chartCanvas.Children.Clear();
            
            if (assignments == null || assignments.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No data to display",
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(emptyText, chartCanvas.ActualWidth / 2 - 60);
                Canvas.SetTop(emptyText, chartCanvas.ActualHeight / 2 - 10);
                chartCanvas.Children.Add(emptyText);
                return;
            }
            
            // Group by day
            var dailyTasks = assignments
                .GroupBy(a => a.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key,
                    TaskCount = g.Sum(a => a.Assignments?.Sum(x => x.TaskCount) ?? 0)
                })
                .ToList();
            
            if (dailyTasks.Count == 0)
                return;
                
            double maxTasks = dailyTasks.Max(d => d.TaskCount);
            double chartHeight = 160;
            double chartWidth = chartCanvas.ActualWidth - 60;
            double barWidth = Math.Min(40, chartWidth / dailyTasks.Count - 5);
            
            for (int i = 0; i < dailyTasks.Count; i++)
            {
                var day = dailyTasks[i];
                double barHeight = (day.TaskCount / maxTasks) * chartHeight;
                double x = 30 + (i * (barWidth + 5));
                double y = chartHeight - barHeight + 20;
                
                // Bar
                var bar = new System.Windows.Shapes.Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(103, 126, 234)),
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                chartCanvas.Children.Add(bar);
                
                // Value label
                var valueLabel = new TextBlock
                {
                    Text = day.TaskCount.ToString(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White
                };
                Canvas.SetLeft(valueLabel, x + barWidth / 2 - 7);
                Canvas.SetTop(valueLabel, y + 5);
                chartCanvas.Children.Add(valueLabel);
                
                // Date label
                var dateLabel = new TextBlock
                {
                    Text = day.Date.ToString("MM/dd"),
                    FontSize = 9,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                Canvas.SetLeft(dateLabel, x + barWidth / 2 - 15);
                Canvas.SetTop(dateLabel, chartHeight + 25);
                chartCanvas.Children.Add(dateLabel);
            }
        }

        private string CalculatePersonFairnessScore(int personTasks, double overallAvg)
        {
            if (overallAvg == 0) return "N/A";
            
            double deviation = Math.Abs(personTasks - overallAvg) / overallAvg * 100;
            
            if (deviation <= 10) return "⭐ Excellent";
            if (deviation <= 25) return "✓ Good";
            if (deviation <= 40) return "⚠ Fair";
            return "❌ Unbalanced";
        }
    }
}