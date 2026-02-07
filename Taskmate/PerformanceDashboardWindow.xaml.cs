using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class PerformanceDashboardWindow : Window
    {
        private List<PersistentAssignment> allAssignments;
        private List<PersistentAssignment> filteredAssignments;

        public PerformanceDashboardWindow()
        {
            InitializeComponent();
            // Load dashboard asynchronously to prevent freezing
            Loaded += async (s, e) => await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                // Load all assignments on background thread
                allAssignments = await Task.Run(() => AssignmentHistoryManager.GetAllAssignments() ?? new List<PersistentAssignment>());
                filteredAssignments = new List<PersistentAssignment>(allAssignments);

                // Setup date range defaults (last 90 days)
                dtTo.SelectedDate = DateTime.Today;
                dtFrom.SelectedDate = DateTime.Today.AddDays(-90);

                // Load people list for filter - preserve current selection
                string currentSelection = cmbPerson.SelectedItem?.ToString() ?? "All People";
                
                var people = allAssignments
                    .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                cmbPerson.Items.Clear();
                cmbPerson.Items.Add("All People");
                foreach (var person in people)
                {
                    cmbPerson.Items.Add(person);
                }

                // Restore previous selection
                int selectionIndex = cmbPerson.Items.IndexOf(currentSelection);
                cmbPerson.SelectedIndex = selectionIndex >= 0 ? selectionIndex : 0;

                // Update dashboard on UI thread
                await UpdateDashboardAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDashboard()
        {
            try
            {
                // Load all assignments
                allAssignments = AssignmentHistoryManager.GetAllAssignments();
                filteredAssignments = new List<PersistentAssignment>(allAssignments);

                // Setup date range defaults (last 90 days)
                dtTo.SelectedDate = DateTime.Today;
                dtFrom.SelectedDate = DateTime.Today.AddDays(-90);

                // Load people list for filter
                var people = allAssignments
                    .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                cmbPerson.Items.Clear();
                cmbPerson.Items.Add("All People");
                foreach (var person in people)
                {
                    cmbPerson.Items.Add(person);
                }
                cmbPerson.SelectedIndex = 0;

                // Update dashboard
                UpdateDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFiltersAsync();
        }

        private async void ApplyFiltersAsync()
        {
            // Guard against null if filters changed before data loaded
            if (allAssignments == null)
                return;

            filteredAssignments = new List<PersistentAssignment>(allAssignments);

            // Date range filter
            if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
            {
                var start = dtFrom.SelectedDate.Value;
                var end = dtTo.SelectedDate.Value.AddDays(1);
                filteredAssignments = filteredAssignments
                    .Where(a => a.Timestamp >= start && a.Timestamp < end)
                    .ToList();
            }

            // Person filter - filter assignments to only include selected person's assignments
            if (cmbPerson.SelectedIndex > 0 && cmbPerson.SelectedItem is string selectedPerson)
            {
                filteredAssignments = filteredAssignments
                    .Select(assignment => new PersistentAssignment
                    {
                        Timestamp = assignment.Timestamp,
                        Tag = assignment.Tag,
                        GroupName = assignment.GroupName,
                        Assignments = assignment.Assignments
                            .Where(ar => ar.Person == selectedPerson)
                            .ToList(),
                        Notes = assignment.Notes,
                        UserNotes = assignment.UserNotes
                    })
                    .Where(a => a.Assignments.Count > 0)  // Only keep assignments with data for this person
                    .ToList();
            }

            await UpdateDashboardAsync();
        }

        private async Task UpdateDashboardAsync()
        {
            try
            {
                // Calculate metrics on background thread
                await Task.Run(() =>
                {
                    int totalAssignments = filteredAssignments.Count;
                    int completeCount = filteredAssignments.Count(a => a.OverallCompletionPercentage >= 100);
                    int partialCount = filteredAssignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
                    int incompleteCount = filteredAssignments.Count(a => a.OverallCompletionPercentage == 0);
                    double avgCompletion = filteredAssignments.Count > 0 ? filteredAssignments.Average(a => a.OverallCompletionPercentage) : 0;

                     // Update metric cards on UI thread
                     Dispatcher.Invoke(() =>
                     {
                         txtTotalAssignments.Text = totalAssignments.ToString();
                         txtTotalAssignmentsTrend.Text = $"Total in period";
                         txtAvgCompletion.Text = $"{avgCompletion:F1}%";
                         txtCompletionTrend.Text = avgCompletion >= 80 ? "Good" : avgCompletion >= 50 ? "Fair" : "Poor";
                         txtCompleteCount.Text = completeCount.ToString();
                        txtCompletePercent.Text = $"{(totalAssignments > 0 ? (completeCount / (double)totalAssignments * 100) : 0):F0}%";
                        txtPartialCount.Text = partialCount.ToString();
                        txtPartialPercent.Text = $"{(totalAssignments > 0 ? (partialCount / (double)totalAssignments * 100) : 0):F0}%";
                        txtIncompleteCount.Text = incompleteCount.ToString();
                        txtIncompletePercent.Text = $"{(totalAssignments > 0 ? (incompleteCount / (double)totalAssignments * 100) : 0):F0}%";

                        // Update dashboard info
                        string dateRange = "";
                        if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
                        {
                            dateRange = $" ({dtFrom.SelectedDate:MMM d, yyyy} - {dtTo.SelectedDate:MMM d, yyyy})";
                        }
                        string personFilter = cmbPerson.SelectedIndex > 0 ? $" for {cmbPerson.SelectedItem}" : "";
                        txtDashboardInfo.Text = $"Real-time analytics{dateRange}{personFilter}";
                    });
                });

                // Load data on background thread
                LoadTopPerformers();
                LoadProblematicTasks();
                LoadCharts();
                LoadRecentActivity();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDashboard()
        {
            try
            {
                // Calculate metrics
                int totalAssignments = filteredAssignments.Count;
                int completeCount = filteredAssignments.Count(a => a.OverallCompletionPercentage >= 100);
                int partialCount = filteredAssignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
                int incompleteCount = filteredAssignments.Count(a => a.OverallCompletionPercentage == 0);
                double avgCompletion = filteredAssignments.Count > 0 ? filteredAssignments.Average(a => a.OverallCompletionPercentage) : 0;

                // Update metric cards
                txtTotalAssignments.Text = totalAssignments.ToString();
                txtTotalAssignmentsTrend.Text = $"Total in period";

                txtAvgCompletion.Text = $"{avgCompletion:F1}%";
                txtCompletionTrend.Text = avgCompletion >= 80 ? "? Good" : avgCompletion >= 50 ? "? Fair" : "? Poor";

                txtCompleteCount.Text = completeCount.ToString();
                txtCompletePercent.Text = $"{(totalAssignments > 0 ? (completeCount / (double)totalAssignments * 100) : 0):F0}%";

                txtPartialCount.Text = partialCount.ToString();
                txtPartialPercent.Text = $"{(totalAssignments > 0 ? (partialCount / (double)totalAssignments * 100) : 0):F0}%";

                txtIncompleteCount.Text = incompleteCount.ToString();
                txtIncompletePercent.Text = $"{(totalAssignments > 0 ? (incompleteCount / (double)totalAssignments * 100) : 0):F0}%";

                // Update dashboard info
                string dateRange = "";
                if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
                {
                    dateRange = $" ({dtFrom.SelectedDate:MMM d, yyyy} - {dtTo.SelectedDate:MMM d, yyyy})";
                }
                string personFilter = cmbPerson.SelectedIndex > 0 ? $" for {cmbPerson.SelectedItem}" : "";
                txtDashboardInfo.Text = $"Real-time analytics{dateRange}{personFilter}";

                // Load top performers
                LoadTopPerformers();

                // Load problematic tasks
                LoadProblematicTasks();

                // Load charts
                LoadCharts();

                // Load recent activity
                LoadRecentActivity();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTopPerformers()
        {
            var personStats = new List<PersonPerformanceItem>();
            var people = filteredAssignments
                .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                .Distinct()
                .ToList();

            foreach (var person in people)
            {
                var personAssignments = filteredAssignments
                    .Where(a => a.Assignments.Any(ar => ar.Person == person))
                    .SelectMany(a => a.Assignments.Where(ar => ar.Person == person))
                    .ToList();

                if (personAssignments.Count > 0)
                {
                    int totalTasks = personAssignments.Sum(a => a.TaskCount);
                    int completedTasks = personAssignments.Sum(a => a.CompletedCount);
                    double completionRate = totalTasks > 0 ? (completedTasks / (double)totalTasks * 100) : 0;

                    personStats.Add(new PersonPerformanceItem
                    {
                        Person = person,
                        CompletionRate = completionRate
                    });
                }
            }

            // Show top 10 performers
            dgTopPerformers.ItemsSource = personStats.OrderByDescending(p => p.CompletionRate).Take(10).ToList();
        }

        private void LoadProblematicTasks()
        {
            var taskStats = new List<TaskProblematicItem>();
            var tasks = filteredAssignments
                .SelectMany(a => a.Assignments.SelectMany(ar => ar.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())))
                .Distinct()
                .ToList();

            foreach (var task in tasks)
            {
                int timesAssigned = 0;
                int timesCompleted = 0;

                foreach (var assignment in filteredAssignments)
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
                    double incompleteRate = ((timesAssigned - timesCompleted) / (double)timesAssigned * 100);
                    taskStats.Add(new TaskProblematicItem
                    {
                        Task = task,
                        IncompleteRate = incompleteRate
                    });
                }
            }

            // Show most incomplete tasks (top 10)
            dgProblematicTasks.ItemsSource = taskStats.OrderByDescending(t => t.IncompleteRate).Take(10).ToList();
        }

        private void LoadCharts()
        {
            try
            {
                if (filteredAssignments.Count == 0)
                    return;

                // Completion Trend Chart (Line Chart)
                chartCompletionTrend.Model = DashboardChartGenerator.CreateCompletionTrendChart(filteredAssignments);

                // Completion Status Chart (Pie Chart)
                chartCompletionStatus.Model = DashboardChartGenerator.CreateCompletionStatusChart(filteredAssignments);

                // Person Performance Chart (Bar Chart)
                chartPersonPerformance.Model = DashboardChartGenerator.CreatePersonPerformanceChart(filteredAssignments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading charts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentActivity()
        {
            // Show most recent assignments (top 20)
            var recentActivity = filteredAssignments
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .ToList();

            dgRecentActivity.ItemsSource = recentActivity;
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardAsync();
        }

        private void btnExportDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var report = GenerateDashboardReport();
                Clipboard.SetText(report);
                MessageBox.Show("Dashboard report copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = $"Dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        // Ensure directory exists
                        var dir = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);

                        // Generate PDF
                        PdfReportGenerator.GenerateDashboardReport(saveDialog.FileName, filteredAssignments);
                        
                        MessageBox.Show($"Dashboard exported to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Ask to open
                        if (MessageBox.Show("Would you like to open the PDF?", "Open PDF", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception innerEx)
                    {
                        MessageBox.Show($"Failed to generate PDF:\n\n{innerEx.Message}\n\n{innerEx.InnerException?.Message}", "PDF Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    FileName = $"Dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ExcelReportGenerator.GenerateCompletionStatisticsExcel(saveDialog.FileName, filteredAssignments);
                    MessageBox.Show($"Dashboard exported to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateDashboardReport()
        {
            var report = $"PERFORMANCE DASHBOARD REPORT\n" +
                        $"Generated: {DateTime.Now:g}\n";

            if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
            {
                report += $"Period: {dtFrom.SelectedDate:MMM d, yyyy} - {dtTo.SelectedDate:MMM d, yyyy}\n";
            }

            if (cmbPerson.SelectedIndex > 0)
            {
                report += $"Person: {cmbPerson.SelectedItem}\n";
            }

            report += $"\n=== KEY METRICS ===\n";
            report += $"Total Assignments: {txtTotalAssignments.Text}\n";
            report += $"Average Completion: {txtAvgCompletion.Text}\n";
            report += $"Fully Completed: {txtCompleteCount.Text} ({txtCompletePercent.Text})\n";
            report += $"Partial: {txtPartialCount.Text} ({txtPartialPercent.Text})\n";
            report += $"Incomplete: {txtIncompleteCount.Text} ({txtIncompletePercent.Text})\n";

            report += $"\n=== TOP PERFORMERS ===\n";
            if (dgTopPerformers.ItemsSource is List<PersonPerformanceItem> topPerformers)
            {
                foreach (var performer in topPerformers)
                {
                    report += $"{performer.Person}: {performer.CompletionRate:F1}%\n";
                }
            }

            report += $"\n=== PROBLEMATIC TASKS ===\n";
            if (dgProblematicTasks.ItemsSource is List<TaskProblematicItem> problems)
            {
                foreach (var problem in problems)
                {
                    report += $"{problem.Task}: {problem.IncompleteRate:F1}% incomplete\n";
                }
            }

            return report;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class PersonPerformanceItem
    {
        public string Person { get; set; }
        public double CompletionRate { get; set; }
    }

    public class TaskProblematicItem
    {
        public string Task { get; set; }
        public double IncompleteRate { get; set; }
    }
}
