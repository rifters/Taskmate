using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class ConstraintsWindow : Window
    {
        public TaskConstraint Constraints { get; private set; }
        private List<string> allTasks;
        private List<string> allPeople;

        public ConstraintsWindow(TaskConstraint constraints, List<string> tasks, List<string> people)
        {
            InitializeComponent();
            
            Constraints = new TaskConstraint();
            // Deep copy the constraints
            foreach (var kvp in constraints.Exclusions)
            {
                Constraints.Exclusions[kvp.Key] = new List<string>(kvp.Value);
            }
            
            allTasks = new List<string>(tasks);
            allPeople = new List<string>(people);
            
            lstPeople.ItemsSource = allPeople;
            if (allPeople.Count > 0)
            {
                lstPeople.SelectedIndex = 0;
            }
        }

        private void lstPeople_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshTaskLists();
        }

        private void RefreshTaskLists()
        {
            if (lstPeople.SelectedItem == null)
            {
                lstAvailableTasks.ItemsSource = null;
                lstExcludedTasks.ItemsSource = null;
                return;
            }

            string selectedPerson = lstPeople.SelectedItem.ToString()!;
            
            List<string> excludedTasks = new List<string>();
            if (Constraints.Exclusions.ContainsKey(selectedPerson))
            {
                excludedTasks = new List<string>(Constraints.Exclusions[selectedPerson]);
            }
            
            List<string> availableTasks = allTasks.Except(excludedTasks).ToList();
            
            lstAvailableTasks.ItemsSource = availableTasks;
            lstExcludedTasks.ItemsSource = excludedTasks;
        }

        private void btnExclude_Click(object sender, RoutedEventArgs e)
        {
            if (lstPeople.SelectedItem == null || lstAvailableTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a person and at least one task to exclude.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string person = lstPeople.SelectedItem.ToString()!;
            foreach (var item in lstAvailableTasks.SelectedItems)
            {
                Constraints.AddExclusion(person, item.ToString()!);
            }
            
            RefreshTaskLists();
        }

        private void btnAllow_Click(object sender, RoutedEventArgs e)
        {
            if (lstPeople.SelectedItem == null || lstExcludedTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a person and at least one excluded task to allow.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string person = lstPeople.SelectedItem.ToString()!;
            var itemsToRemove = lstExcludedTasks.SelectedItems.Cast<string>().ToList();
            foreach (var task in itemsToRemove)
            {
                Constraints.RemoveExclusion(person, task);
            }
            
            RefreshTaskLists();
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all constraints?", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Constraints.Exclusions.Clear();
                RefreshTaskLists();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
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