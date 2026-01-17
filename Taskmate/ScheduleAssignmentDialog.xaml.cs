using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class ScheduleAssignmentDialog : Window
    {
        public ScheduledAssignment? ScheduledAssignment { get; private set; }

        public ScheduleAssignmentDialog(ScheduledAssignment? existing = null)
        {
            InitializeComponent();

            if (existing != null)
            {
                txtName.Text = existing.Name;
                txtGroupPath.Text = existing.GroupFilePath;
                dpDate.SelectedDate = existing.ScheduledDate.Date;
                txtHour.Text = existing.ScheduledDate.Hour.ToString();
                txtMinute.Text = existing.ScheduledDate.Minute.ToString("00");
                chkRecurring.IsChecked = existing.IsRecurring;
                txtInterval.Text = existing.RecurrenceInterval.ToString();
                txtNotes.Text = existing.Notes;

                if (existing.IsRecurring)
                {
                    cmbRecurrence.SelectedIndex = (int)existing.RecurrenceType - 1;
                    pnlRecurrence.Visibility = Visibility.Visible;
                }

                Title = "Edit Scheduled Assignment";
                ScheduledAssignment = existing;
            }
            else
            {
                dpDate.SelectedDate = DateTime.Today;
                ScheduledAssignment = new ScheduledAssignment();
            }

            cmbRecurrence.SelectionChanged += (s, e) => UpdateIntervalLabel();
            UpdateIntervalLabel();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Group File",
                Filter = "Group Files (*.tgroup)|*.tgroup|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                txtGroupPath.Text = ofd.FileName;
                
                // Auto-fill name if empty
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    txtName.Text = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
                }
            }
        }

        private void chkRecurring_Changed(object sender, RoutedEventArgs e)
        {
            pnlRecurrence.Visibility = chkRecurring.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateIntervalLabel()
        {
            var suffix = cmbRecurrence.SelectedIndex switch
            {
                0 => "day(s)",
                1 => "week(s)",
                2 => "month(s)",
                _ => "day(s)"
            };
            txtIntervalLabel.Text = suffix;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a name for this scheduled assignment.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtGroupPath.Text))
            {
                MessageBox.Show("Please select a group file.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date.", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtHour.Text, out int hour) || hour < 0 || hour > 23)
            {
                MessageBox.Show("Please enter a valid hour (0-23).", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtHour.Focus();
                return;
            }

            if (!int.TryParse(txtMinute.Text, out int minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("Please enter a valid minute (0-59).", "Validation", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMinute.Focus();
                return;
            }

            // Create scheduled assignment
            if (ScheduledAssignment == null)
                ScheduledAssignment = new ScheduledAssignment();

            ScheduledAssignment.Name = txtName.Text.Trim();
            ScheduledAssignment.GroupFilePath = txtGroupPath.Text;
            ScheduledAssignment.ScheduledDate = dpDate.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
            ScheduledAssignment.IsRecurring = chkRecurring.IsChecked == true;
            ScheduledAssignment.Notes = txtNotes.Text;

            if (ScheduledAssignment.IsRecurring)
            {
                ScheduledAssignment.RecurrenceType = (RecurrenceType)(cmbRecurrence.SelectedIndex + 1);
                if (int.TryParse(txtInterval.Text, out int interval) && interval > 0)
                {
                    ScheduledAssignment.RecurrenceInterval = interval;
                }
                else
                {
                    MessageBox.Show("Please enter a valid recurrence interval (positive number).", "Validation", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtInterval.Focus();
                    return;
                }
            }
            else
            {
                ScheduledAssignment.RecurrenceType = RecurrenceType.None;
                ScheduledAssignment.RecurrenceInterval = 1;
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}