using System;
using System.Collections.Generic;
using System.Windows;

namespace Taskmate
{
    public partial class ReportScheduleWindow : Window
    {
        public ReportScheduleWindow()
        {
            InitializeComponent();
            LoadSchedules();
        }

        private void LoadSchedules()
        {
            try
            {
                var schedules = ScheduledReportManager.GetSchedules();
                dgSchedules.ItemsSource = schedules;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schedules: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewSchedule_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ReportScheduleDialog(null)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                LoadSchedules();
                MessageBox.Show("Schedule created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnEditSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (dgSchedules.SelectedItem is ReportSchedule schedule)
            {
                var dialog = new ReportScheduleDialog(schedule)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadSchedules();
                    MessageBox.Show("Schedule updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a schedule to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDeleteSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (dgSchedules.SelectedItem is ReportSchedule schedule)
            {
                if (MessageBox.Show($"Delete schedule '{schedule.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ScheduledReportManager.DeleteSchedule(schedule.Id);
                    LoadSchedules();
                    MessageBox.Show("Schedule deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a schedule to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnRunNow_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Schedule execution coming in Phase 5 integration.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void dgSchedules_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgSchedules.SelectedItem is ReportSchedule)
            {
                btnEditSchedule_Click(sender, null);
            }
        }

        private void btnViewLogs_Click(object sender, RoutedEventArgs e)
        {
            var logsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TaskAssigner", "Schedules", "logs");

            if (!System.IO.Directory.Exists(logsPath))
            {
                MessageBox.Show("No logs yet. Run a schedule first.", "No Logs", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", logsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
