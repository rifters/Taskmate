using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Taskmate
{
    public partial class HistoryBrowserWindow : Window
    {
        private List<PersistentAssignment> allAssignments = new List<PersistentAssignment>();
        private List<PersistentAssignment> filteredAssignments = new List<PersistentAssignment>();

        public HistoryBrowserWindow()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            allAssignments = AssignmentHistoryManager.GetAllAssignments();
            filteredAssignments = new List<PersistentAssignment>(allAssignments);
            
            // Load tags
            var tags = AssignmentHistoryManager.GetAllTags();
            cmbTags.Items.Clear();
            cmbTags.Items.Add("All Tags");
            foreach (var tag in tags)
            {
                cmbTags.Items.Add(tag);
            }
            cmbTags.SelectedIndex = 0;

            // Set default date range (last 30 days)
            dtTo.SelectedDate = DateTime.Today;
            dtFrom.SelectedDate = DateTime.Today.AddDays(-30);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            dgHistory.ItemsSource = filteredAssignments;
            txtTotalCount.Text = $"{filteredAssignments.Count} assignment(s) found";
        }

        private void ApplyFilters()
        {
            filteredAssignments = new List<PersistentAssignment>(allAssignments);

            // Search filter
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string search = txtSearch.Text.ToLower();
                filteredAssignments = filteredAssignments
                    .Where(a => 
                        a.Tag.ToLower().Contains(search) ||
                        a.GroupName.ToLower().Contains(search) ||
                        a.Notes.ToLower().Contains(search) ||
                        a.Assignments.Any(p => p.Person.ToLower().Contains(search)))
                    .ToList();
            }

            // Tag filter
            if (cmbTags.SelectedIndex > 0 && cmbTags.SelectedItem != null)
            {
                string selectedTag = cmbTags.SelectedItem.ToString()!;
                filteredAssignments = filteredAssignments
                    .Where(a => a.Tag.Equals(selectedTag, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Date range filter
            if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
            {
                var start = dtFrom.SelectedDate.Value;
                var end = dtTo.SelectedDate.Value.AddDays(1); // Include the entire end date
                filteredAssignments = filteredAssignments
                    .Where(a => a.Timestamp >= start && a.Timestamp < end)
                    .ToList();
            }

            UpdateDisplay();
        }

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbTags_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateRange_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void dgHistory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                dgDetails.ItemsSource = assignment.Assignments;
            }
            else
            {
                dgDetails.ItemsSource = null;
            }
        }

        private void btnViewStats_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                var statsWindow = new StatisticsWindow(assignment.Assignments)
                {
                    Owner = this,
                    Title = $"Statistics - {assignment.Tag} ({assignment.Timestamp:g})"
                };
                statsWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please select an assignment first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                try
                {
                    string formatted = CsvExporter.FormatForClipboard(assignment.Assignments);
                    formatted = $"Assignment from {assignment.Timestamp:g} - {assignment.Tag}\n{formatted}";
                    Clipboard.SetText(formatted);
                    MessageBox.Show("Assignment copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an assignment first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Export Assignment to CSV",
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"assignment_{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        CsvExporter.ExportToCsv(assignment.Assignments, sfd.FileName);
                        MessageBox.Show("Export successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an assignment first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            if (filteredAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to generate report from.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Generate Report",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"report_{DateTime.Now:yyyyMMdd}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    GenerateReport(sfd.FileName);
                    MessageBox.Show("Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    var result = MessageBox.Show("Open the report now?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateReport(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("           ASSIGNMENT HISTORY REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"Generated: {DateTime.Now:g}");
            sb.AppendLine($"Period: {dtFrom.SelectedDate:d} to {dtTo.SelectedDate:d}");
            sb.AppendLine($"Total Assignments: {filteredAssignments.Count}");
            sb.AppendLine();

            // Summary by Tag
            var tagGroups = filteredAssignments.GroupBy(a => a.Tag);
            sb.AppendLine("ASSIGNMENTS BY TAG:");
            sb.AppendLine(new string('─', 50));
            foreach (var group in tagGroups.OrderBy(g => g.Key))
            {
                sb.AppendLine($"  {group.Key}: {group.Count()} assignment(s)");
            }
            sb.AppendLine();

            // Person statistics
            var personStats = new Dictionary<string, int>();
            foreach (var assignment in filteredAssignments)
            {
                foreach (var result in assignment.Assignments)
                {
                    if (personStats.ContainsKey(result.Person))
                        personStats[result.Person] += result.TaskCount;
                    else
                        personStats[result.Person] = result.TaskCount;
                }
            }

            sb.AppendLine("TOTAL TASKS PER PERSON:");
            sb.AppendLine(new string('─', 50));
            foreach (var stat in personStats.OrderByDescending(s => s.Value))
            {
                sb.AppendLine($"  {stat.Key}: {stat.Value} task(s)");
            }
            sb.AppendLine();

            // Detailed assignments
            sb.AppendLine("DETAILED ASSIGNMENTS:");
            sb.AppendLine(new string('═', 60));
            foreach (var assignment in filteredAssignments.OrderBy(a => a.Timestamp))
            {
                sb.AppendLine();
                sb.AppendLine($"Date/Time: {assignment.Timestamp:g}");
                sb.AppendLine($"Tag: {assignment.Tag}");
                sb.AppendLine($"Group: {assignment.GroupName}");
                if (!string.IsNullOrEmpty(assignment.Notes))
                    sb.AppendLine($"Notes: {assignment.Notes}");
                sb.AppendLine(new string('─', 60));
                
                foreach (var result in assignment.Assignments)
                {
                    sb.AppendLine($"  {result.Person} ({result.TaskCount} tasks):");
                    var taskList = result.Tasks.Split(new[] { ", " }, StringSplitOptions.None);
                    foreach (var task in taskList)
                    {
                        sb.AppendLine($"    • {task}");
                    }
                }
            }

            System.IO.File.WriteAllText(filePath, sb.ToString());
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                var result = MessageBox.Show(
                    $"This will replace your current assignment with the one from {assignment.Timestamp:g}. Continue?",
                    "Confirm Load",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // This will be handled by the calling window
                    Tag = assignment; // Store for retrieval
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Please select an assignment first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the assignment from {assignment.Timestamp:g}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        AssignmentHistoryManager.DeleteAssignment(assignment.Id);
                        LoadHistory();
                        MessageBox.Show("Assignment deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an assignment first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}