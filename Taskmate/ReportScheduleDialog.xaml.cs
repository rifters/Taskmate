using System;
using System.Windows;
using System.Windows.Forms;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Taskmate
{
    public partial class ReportScheduleDialog : Window
    {
        private ReportSchedule _schedule;
        private bool _isNewSchedule;

        public ReportScheduleDialog(ReportSchedule schedule = null)
        {
            InitializeComponent();
            _schedule = schedule ?? new ReportSchedule { Name = "New Schedule" };
            _isNewSchedule = schedule == null;

            InitializeTimeDropdowns();
            InitializeDayDropdowns();
            LoadScheduleData();
        }

        private void InitializeTimeDropdowns()
        {
            // Hours
            for (int i = 0; i < 24; i++)
            {
                cmbHour.Items.Add(i.ToString("00"));
            }

            // Minutes
            for (int i = 0; i < 60; i += 15)
            {
                cmbMinute.Items.Add(i.ToString("00"));
            }

            cmbHour.SelectedIndex = 0;
            cmbMinute.SelectedIndex = 0;
        }

        private void InitializeDayDropdowns()
        {
            // Day of month
            for (int i = 1; i <= 31; i++)
            {
                cmbDayOfMonth.Items.Add(i.ToString());
            }
            cmbDayOfMonth.SelectedIndex = 0;
        }

        private void LoadScheduleData()
        {
            txtName.Text = _schedule.Name;

            // Set frequency
            cmbFrequency.SelectedIndex = (int)_schedule.Frequency;

            // Set time
            cmbHour.SelectedItem = _schedule.Time.Hours.ToString("00");
            cmbMinute.SelectedItem = _schedule.Time.Minutes.ToString("00");

            // Set day of week
            cmbDayOfWeek.SelectedIndex = (int)_schedule.DayOfWeek;

            // Set day of month
            cmbDayOfMonth.SelectedItem = _schedule.DayOfMonth.ToString();

            // Set report type
            cmbReportType.SelectedIndex = (int)_schedule.ReportType;

            // Set output folder
            txtOutputFolder.Text = _schedule.OutputFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Set enabled
            chkEnabled.IsChecked = _schedule.IsEnabled;

            UpdateVisibility();
        }

        private void cmbFrequency_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var frequencyIndex = cmbFrequency.SelectedIndex;

            // Show day of week for weekly
            cmbDayOfWeek.Visibility = frequencyIndex == 1 ? Visibility.Visible : Visibility.Collapsed;

            // Show day of month for monthly
            cmbDayOfMonth.Visibility = frequencyIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select output folder for reports";
                dialog.SelectedPath = txtOutputFolder.Text;

                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    txtOutputFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                System.Windows.MessageBox.Show("Please enter a schedule name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                System.Windows.MessageBox.Show("Please select an output folder.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _schedule.Name = txtName.Text;
                _schedule.Frequency = (ReportFrequency)cmbFrequency.SelectedIndex;
                _schedule.Time = new TimeSpan(int.Parse(cmbHour.SelectedItem.ToString()), int.Parse(cmbMinute.SelectedItem.ToString()), 0);
                _schedule.DayOfWeek = (DayOfWeek)cmbDayOfWeek.SelectedIndex;
                _schedule.DayOfMonth = int.Parse(cmbDayOfMonth.SelectedItem.ToString());
                _schedule.ReportType = (ReportType)cmbReportType.SelectedIndex;
                _schedule.OutputFolder = txtOutputFolder.Text;
                _schedule.IsEnabled = chkEnabled.IsChecked ?? true;

                if (_isNewSchedule)
                {
                    ScheduledReportManager.AddSchedule(_schedule);
                }
                else
                {
                    ScheduledReportManager.UpdateSchedule(_schedule);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
