using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class PersonHistoryWindow : Window
    {
        private List<PersonHistoryItem> allHistoryItems = new List<PersonHistoryItem>();
        private List<string> allPeople = new List<string>();
        private DateTime? filterStartDate;
        private DateTime? filterEndDate;

        public PersonHistoryWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Get all assignments from history
                var allAssignments = AssignmentHistoryManager.GetAllAssignments();

                // Extract unique people and build history items
                var peopleSet = new HashSet<string>();
                var historyItems = new List<PersonHistoryItem>();

                foreach (var assignment in allAssignments)
                {
                    foreach (var personResult in assignment.Assignments)
                    {
                        peopleSet.Add(personResult.Person);
                        
                        historyItems.Add(new PersonHistoryItem
                        {
                            Person = personResult.Person,
                            Timestamp = assignment.Timestamp,
                            GroupName = assignment.GroupName,
                            Tag = assignment.Tag,
                            TasksCount = personResult.TaskCount,
                            Tasks = personResult.Tasks,
                            WorkloadPercentage = personResult.WorkloadPercentage,
                            UserNotes = assignment.UserNotes,
                            Capacity = personResult.Capacity
                        });
                    }
                }

                allPeople = peopleSet.OrderBy(p => p).ToList();
                allHistoryItems = historyItems.OrderByDescending(h => h.Timestamp).ToList();

                // Populate combo box
                cmbPerson.ItemsSource = allPeople;
                
                if (allPeople.Count > 0)
                {
                    cmbPerson.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("No assignment history found.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbPerson_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPerson.SelectedItem is string selectedPerson)
            {
                DisplayPersonHistory(selectedPerson);
            }
        }

        private void DisplayPersonHistory(string personName)
        {
            // Filter by selected person
            var personHistory = allHistoryItems
                .Where(h => h.Person == personName)
                .ToList();

            // Apply date filter if set
            if (filterStartDate.HasValue || filterEndDate.HasValue)
            {
                personHistory = personHistory
                    .Where(h =>
                    {
                        bool afterStart = !filterStartDate.HasValue || h.Timestamp.Date >= filterStartDate.Value.Date;
                        bool beforeEnd = !filterEndDate.HasValue || h.Timestamp.Date <= filterEndDate.Value.Date;
                        return afterStart && beforeEnd;
                    })
                    .ToList();
            }

            // Display in grid
            dgHistory.ItemsSource = new ObservableCollection<PersonHistoryItem>(personHistory);

            // Calculate and display statistics
            if (personHistory.Count > 0)
            {
                int totalTasks = personHistory.Sum(h => h.TasksCount);
                int maxTasks = personHistory.Max(h => h.TasksCount);
                int minTasks = personHistory.Min(h => h.TasksCount);
                double avgTasks = personHistory.Average(h => h.TasksCount);
                int assignmentCount = personHistory.Count;

                txtTotalAssignments.Text = assignmentCount.ToString();
                txtAvgTasks.Text = avgTasks.ToString("F1");
                
                txtStatsTotal.Text = totalTasks.ToString();
                txtStatsMax.Text = maxTasks.ToString();
                txtStatsMin.Text = minTasks.ToString();
                txtStatsCount.Text = assignmentCount.ToString();
            }
            else
            {
                txtTotalAssignments.Text = "0";
                txtAvgTasks.Text = "0";
                txtStatsTotal.Text = "0";
                txtStatsMax.Text = "0";
                txtStatsMin.Text = "0";
                txtStatsCount.Text = "0";
            }
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue || dpEndDate.SelectedDate.HasValue)
            {
                filterStartDate = dpStartDate.SelectedDate;
                filterEndDate = dpEndDate.SelectedDate;

                if (cmbPerson.SelectedItem is string selectedPerson)
                {
                    DisplayPersonHistory(selectedPerson);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one date.", "No Filter", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            filterStartDate = null;
            filterEndDate = null;
            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;

            if (cmbPerson.SelectedItem is string selectedPerson)
            {
                DisplayPersonHistory(selectedPerson);
            }
        }
    }

    // Helper class for display
    public class PersonHistoryItem
    {
        public string Person { get; set; }
        public DateTime Timestamp { get; set; }
        public string GroupName { get; set; }
        public string Tag { get; set; }
        public int TasksCount { get; set; }
        public string Tasks { get; set; }
        public string WorkloadPercentage { get; set; }
        public string UserNotes { get; set; }
        public double Capacity { get; set; }
    }
}
