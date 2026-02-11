using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Taskmate.SmartAssignment;
using Taskmate.Utilities;

namespace Taskmate
{
    public partial class TaskAssignmentEditorWindow : Window
    {
        private List<string> _allTasks;
        private Dictionary<string, string> _taskAssignments;
        private List<AssignmentResult> _currentAssignments;
        private bool _assignmentsChanged = false;

        public List<AssignmentResult> UpdatedAssignments => _currentAssignments;
        public bool AssignmentsChanged => _assignmentsChanged;

        public TaskAssignmentEditorWindow(List<string> tasks, List<AssignmentResult> assignments)
        {
            InitializeComponent();
            _allTasks = tasks ?? new List<string>();
            _currentAssignments = assignments ?? new List<AssignmentResult>();
            _taskAssignments = new Dictionary<string, string>();

            // Build task -> person mapping
            foreach (var assignment in _currentAssignments)
            {
                foreach (var task in assignment.Tasks.Split(','))
                {
                    var trimmedTask = task.Trim();
                    if (!string.IsNullOrEmpty(trimmedTask))
                    {
                        _taskAssignments[trimmedTask] = assignment.Person;
                    }
                }
            }

            LoadTasks();
        }

        private void LoadTasks()
        {
            lbTasks.ItemsSource = _allTasks;
            txtTaskCount.Text = $"{_allTasks.Count} tasks";
        }

        private async void LbTasks_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lbTasks.SelectedItem is string task)
            {
                DisplayTaskDetails(task);
                await LoadSuggestionsForTask(task);
            }
        }

        private void DisplayTaskDetails(string task)
        {
            txtSelectedTask.Text = task;

            // Find who this task is assigned to
            if (_taskAssignments.TryGetValue(task, out var assignee))
            {
                txtCurrentAssignee.Text = assignee;
            }
            else
            {
                txtCurrentAssignee.Text = "(Unassigned)";
            }
        }

        private async Task LoadSuggestionsForTask(string selectedTask)
        {
            try
            {
                // Get all people who could do this task
                var allPeople = _currentAssignments.Select(a => a.Person).ToList();
                var allAssignments = AssignmentHistoryManager.GetAllAssignments() ?? new List<PersistentAssignment>();

                if (allPeople.Count > 0 && allAssignments.Count > 0)
                {
                    await smartSuggestionsPanel.LoadSuggestionsAsync(allPeople, allAssignments);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading suggestions for task", ex);
            }
        }

        private void SmartSuggestionsPanel_PersonSelected(object sender, PersonSelectedEventArgs e)
        {
            if (lbTasks.SelectedItem is string task)
            {
                // Assign the task to this person
                AssignTaskToPerson(task, e.PersonName);
            }
        }

        private void AssignTaskToPerson(string task, string person)
        {
            try
            {
                // Remove task from current assignee if it has one
                if (_taskAssignments.ContainsKey(task))
                {
                    var currentAssignee = _taskAssignments[task];
                    var currentAssignment = _currentAssignments.FirstOrDefault(a => a.Person == currentAssignee);
                    if (currentAssignment != null)
                    {
                        // Remove task from their list
                        var taskList = currentAssignment.Tasks.Split(',').Select(t => t.Trim()).ToList();
                        taskList.Remove(task);
                        currentAssignment.Tasks = string.Join(", ", taskList);
                    }
                }

                // Add task to new assignee
                var assignment = _currentAssignments.FirstOrDefault(a => a.Person == person);
                if (assignment != null)
                {
                    var taskList = string.IsNullOrEmpty(assignment.Tasks)
                        ? new List<string>()
                        : assignment.Tasks.Split(',').Select(t => t.Trim()).ToList();

                    if (!taskList.Contains(task))
                    {
                        taskList.Add(task);
                        assignment.Tasks = string.Join(", ", taskList);
                    }
                }

                // Update mapping
                _taskAssignments[task] = person;
                
                // Mark that assignments have changed
                _assignmentsChanged = true;

                // Refresh display
                DisplayTaskDetails(task);

                MessageBox.Show($"Task '{task}' assigned to {person}", "Assignment Updated", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error assigning task", ex);
                MessageBox.Show("Error assigning task", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUnassign_Click(object sender, RoutedEventArgs e)
        {
            if (lbTasks.SelectedItem is string task)
            {
                try
                {
                    if (_taskAssignments.TryGetValue(task, out var assignee))
                    {
                        var assignment = _currentAssignments.FirstOrDefault(a => a.Person == assignee);
                        if (assignment != null)
                        {
                            var taskList = assignment.Tasks.Split(',').Select(t => t.Trim()).ToList();
                            taskList.Remove(task);
                            assignment.Tasks = string.Join(", ", taskList);
                        }

                        _taskAssignments.Remove(task);
                        _assignmentsChanged = true;
                        DisplayTaskDetails(task);

                        MessageBox.Show($"Task '{task}' unassigned", "Task Unassigned", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error unassigning task", ex);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
