using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class AssignmentSchedulerWindow : Window
    {
        public AssignmentSchedulerWindow()
        {
            InitializeComponent();
            LoadScheduledAssignments();
        }

        private void LoadScheduledAssignments()
        {
            var scheduled = SchedulerManager.GetAllScheduledAssignments();
            dgScheduled.ItemsSource = scheduled.OrderBy(s => s.ScheduledDate).ToList();
            
            // Update upcoming count
            var upcoming = SchedulerManager.GetUpcomingAssignments(7);
            txtUpcomingCount.Text = upcoming.Count.ToString();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ScheduleAssignmentDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && dialog.ScheduledAssignment != null)
            {
                SchedulerManager.SaveScheduledAssignment(dialog.ScheduledAssignment);
                LoadScheduledAssignments();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgScheduled.SelectedItem is ScheduledAssignment selected)
            {
                var dialog = new ScheduleAssignmentDialog(selected)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && dialog.ScheduledAssignment != null)
                {
                    SchedulerManager.SaveScheduledAssignment(dialog.ScheduledAssignment);
                    LoadScheduledAssignments();
                }
            }
            else
            {
                MessageBox.Show("Please select a scheduled assignment to edit.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgScheduled.SelectedItem is ScheduledAssignment selected)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{selected.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SchedulerManager.DeleteScheduledAssignment(selected.Id);
                    LoadScheduledAssignments();
                }
            }
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (dgScheduled.SelectedItem is ScheduledAssignment selected)
            {
                var result = MessageBox.Show(
                    $"Execute '{selected.Name}' now?\n\nThis will load the group and you can assign tasks.",
                    "Execute Assignment",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show(
                        $"To execute this assignment:\n\n" +
                        $"1. Close this window\n" +
                        $"2. Load the group: {System.IO.Path.GetFileName(selected.GroupFilePath)}\n" +
                        $"3. Click Assign Tasks\n\n" +
                        $"Group file location:\n{selected.GroupFilePath}",
                        "Execute Instructions",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadScheduledAssignments();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void chkEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ScheduledAssignment assignment)
            {
                assignment.IsEnabled = checkBox.IsChecked ?? false;
                SchedulerManager.SaveScheduledAssignment(assignment);
            }
        }
    }
}