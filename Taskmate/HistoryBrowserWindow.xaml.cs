using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
            allAssignments = AssignmentHistoryManager.GetAllAssignments() ?? new List<PersistentAssignment>();
            filteredAssignments = new List<PersistentAssignment>(allAssignments);
            
            // Load tags
            var tags = AssignmentHistoryManager.GetAllTags() ?? new List<string>();
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
            try
            {
                if (filteredAssignments == null)
                {
                    filteredAssignments = new List<PersistentAssignment>();
                }

                if (dgHistory != null)
                {
                    dgHistory.ItemsSource = null; // Clear first
                    dgHistory.ItemsSource = filteredAssignments;
                }

                if (lblTotalCount != null)
                {
                    lblTotalCount.Text = $"{filteredAssignments.Count} assignment(s) found";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating display: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            // Start with all assignments and chain LINQ queries
            IEnumerable<PersistentAssignment> filtered = allAssignments;

            // Search filter
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string searchLower = txtSearch.Text.ToLower();
                filtered = filtered.Where(a =>
                    (a.Tag?.ToLower().Contains(searchLower) ?? false) ||
                    (a.GroupName?.ToLower().Contains(searchLower) ?? false) ||
                    (a.Notes?.ToLower().Contains(searchLower) ?? false) ||
                    (a.Assignments?.Any(p => p.Person?.ToLower().Contains(searchLower) ?? false) ?? false));
            }

            // Tag filter
            if (cmbTags.SelectedIndex > 0 && cmbTags.SelectedItem is string selectedTag)
            {
                filtered = filtered.Where(a => a.Tag?.Equals(selectedTag, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            // Date range filter
            if (dtFrom.SelectedDate.HasValue && dtTo.SelectedDate.HasValue)
            {
                var start = dtFrom.SelectedDate.Value;
                var end = dtTo.SelectedDate.Value.AddDays(1);
                filtered = filtered.Where(a => a.Timestamp >= start && a.Timestamp < end);
            }

            // Completion status filter
            if (cmbCompletionStatus.SelectedIndex > 0 && cmbCompletionStatus.SelectedItem is string selectedStatus)
            {
                filtered = filtered.Where(a =>
                {
                    double completionPercentage = a.OverallCompletionPercentage;
                    return selectedStatus switch
                    {
                        "Complete" => completionPercentage >= 100,
                        "Partial" => completionPercentage > 0 && completionPercentage < 100,
                        "Incomplete" => completionPercentage == 0,
                        _ => true
                    };
                });
            }

            // Convert to list only once at the end
            filteredAssignments = filtered.ToList();
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

        private void cmbCompletionStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void dgHistory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgHistory.SelectedItem is PersistentAssignment assignment)
            {
                dgDetails.ItemsSource = assignment.Assignments;
                
                // Show when completion was last updated
                if (assignment.CompletionUpdatedAt.HasValue)
                {
                    txtCompletionUpdatedAt.Text = assignment.CompletionUpdatedAt.Value.ToString("g");
                }
                else
                {
                    txtCompletionUpdatedAt.Text = "Not tracked";
                }
            }
            else
            {
                dgDetails.ItemsSource = null;
                txtCompletionUpdatedAt.Text = "Not tracked";
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

        private void btnViewCompletionStats_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new CompletionStatisticsWindow
            {
                Owner = this
            };
            statsWindow.ShowDialog();
        }

        private void btnViewDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = new PerformanceDashboardWindow
            {
                Owner = this
            };
            dashboardWindow.ShowDialog();
        }

        private void btnEditCompletion_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is not PersistentAssignment assignment)
            {
                MessageBox.Show("Please select an assignment to edit completion status.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Create dialog for editing completion
                var window = new Window
                {
                    Title = "Edit Task Completion",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                var stackPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

                // Show each person with their tasks and checkboxes
                foreach (var person in assignment.Assignments)
                {
                    var border = new Border
                    {
                        BorderBrush = System.Windows.Media.Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var personStack = new StackPanel();

                    // Person name and completion status
                    var personText = new TextBlock
                    {
                        Text = $"{person.Person} ({person.CompletedCount}/{person.TaskCount} complete)",
                        FontWeight = System.Windows.FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    personStack.Children.Add(personText);

                    // Task checkboxes
                    var tasks = person.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(t => t.Trim())
                                           .ToList();

                    foreach (var task in tasks)
                    {
                        var checkbox = new CheckBox
                        {
                            Content = task,
                            IsChecked = person.CompletedTasks.Contains(task),
                            Margin = new Thickness(20, 4, 0, 4)
                        };

                        checkbox.Checked += (s, ev) =>
                        {
                            if (!person.CompletedTasks.Contains(task))
                                person.CompletedTasks.Add(task);
                        };

                        checkbox.Unchecked += (s, ev) =>
                        {
                            person.CompletedTasks.Remove(task);
                        };

                        personStack.Children.Add(checkbox);
                    }

                    border.Child = personStack;
                    stackPanel.Children.Add(border);
                }

                scrollViewer.Content = stackPanel;
                grid.Children.Add(scrollViewer);
                Grid.SetRow(scrollViewer, 0);

                // Buttons
                var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var saveButton = new Button { Content = "Save", Width = 80, Height = 35, Margin = new Thickness(0, 0, 5, 0) };
                var cancelButton = new Button { Content = "Cancel", Width = 80, Height = 35, IsCancel = true };

                saveButton.Click += (s, ev) =>
                {
                    try
                    {
                        AssignmentHistoryManager.UpdateAssignmentCompletion(assignment);
                        MessageBox.Show("Completion status updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        window.DialogResult = true;
                        window.Close();
                        
                        // Refresh the display
                        LoadHistory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                buttonStack.Children.Add(saveButton);
                buttonStack.Children.Add(cancelButton);
                grid.Children.Add(buttonStack);
                Grid.SetRow(buttonStack, 1);

                window.Content = grid;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (PersistentAssignment assignment in items)
                {
                    assignment.IsSelected = true;
                }
                dgHistory.Items.Refresh();
            }
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (PersistentAssignment assignment in items)
                {
                    assignment.IsSelected = false;
                }
                dgHistory.Items.Refresh();
            }
        }

        private void btnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<PersistentAssignment>();
            if (dgHistory.ItemsSource is System.Collections.IEnumerable items)
            {
                foreach (PersistentAssignment assignment in items)
                {
                    if (assignment.IsSelected)
                        selectedItems.Add(assignment);
                }
            }

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one assignment to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {selectedItems.Count} assignment(s)?\n\nThis action cannot be undone.",
                "Confirm Batch Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var ids = selectedItems.Select(a => a.Id).ToList();
                    AssignmentHistoryManager.DeleteMultipleAssignments(ids);
                    LoadHistory();
                    MessageBox.Show($"✓ {selectedItems.Count} assignment(s) deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnDeleteByDateRange_Click(object sender, RoutedEventArgs e)
        {
            if (!dtFrom.SelectedDate.HasValue || !dtTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both start and end dates for the date range.", "Incomplete Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = dtFrom.SelectedDate.Value;
            DateTime endDate = dtTo.SelectedDate.Value;

            if (startDate > endDate)
            {
                MessageBox.Show("Start date cannot be after end date.", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get count of items that will be deleted
            var itemsToDelete = AssignmentHistoryManager.GetAssignmentsByDateRange(startDate, endDate);

            if (itemsToDelete.Count == 0)
            {
                MessageBox.Show("No assignments found in the selected date range.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete {itemsToDelete.Count} assignment(s) from {startDate:d} to {endDate:d}?\n\nThis action cannot be undone.",
                "Confirm Delete by Date Range",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    AssignmentHistoryManager.DeleteAssignmentsByDateRange(startDate, endDate);
                    LoadHistory();
                    MessageBox.Show($"✓ {itemsToDelete.Count} assignment(s) deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}