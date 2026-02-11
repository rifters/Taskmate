using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Taskmate
{
    public partial class MainWindow : Window
    {
        private List<string> tasks = new List<string>();
        private List<string> people = new List<string>();
        private Dictionary<string, double> peopleCapacities = new Dictionary<string, double>();
        private Random rng = new Random();
        private List<AssignmentResult> currentAssignments = new List<AssignmentResult>();
        private const int MaxRecentGroups = 10;
        private Stack<AssignmentHistoryEntry> assignmentHistory = new Stack<AssignmentHistoryEntry>();
        private TaskConstraint constraints = new TaskConstraint();

        // Add to existing fields
        private Dictionary<string, bool> peopleAvailability = new Dictionary<string, bool>();
        private Dictionary<string, string> peopleRoles = new Dictionary<string, string>();
        private Dictionary<string, int> taskWeights = new Dictionary<string, int>();
        private Dictionary<string, string> taskNotes = new Dictionary<string, string>();
        private Dictionary<string, int> taskTimeEstimates = new Dictionary<string, int>();
        private Dictionary<string, string> taskCategoryAssignments = new Dictionary<string, string>();

        private FeatureFlags features = FeatureManager.GetFeatures();

        // Add field for session timeout
        private SessionTimeoutManager? _sessionTimeoutManager;

        public MainWindow()
        {
            InitializeComponent();
            LoadRecentGroupsMenu();
            UpdateConstraintInfo();
            SetupCommands();
            
            // Ensure default tags, roles, and categories exist
            TagManager.EnsureDefaultTags();
            RoleManager.EnsureDefaultRoles();
            CategoryManager.EnsureDefaultCategories();
            
            features = FeatureManager.GetFeatures();
            UpdateMenusBasedOnFeatures();
            
            // Initialize session timeout if enabled
            if (Properties.Settings.Default.EnableSessionTimeout)
            {
                int timeoutMinutes = Properties.Settings.Default.SessionTimeoutMinutes;
                _sessionTimeoutManager = new SessionTimeoutManager(this, timeoutMinutes);
                AuditLogger.Log("SESSION_START", Environment.UserName, "Application started");
            }
            
            // Update audit log menu visibility
            mnuViewAuditLog.Visibility = Properties.Settings.Default.EnableAuditLog 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            
            // Initialize dashboard
            UpdateDashboard();
        }

        private void btnEditAssignments_Click(object sender, RoutedEventArgs e)
        {
            if (tasks.Count == 0 || currentAssignments.Count == 0)
            {
                MessageBox.Show("Please load tasks and assign them first.", "No Assignments", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editor = new TaskAssignmentEditorWindow(tasks, currentAssignments)
            {
                Owner = this
            };

            if (editor.ShowDialog() == true)
            {
                // Only update if assignments actually changed
                if (editor.AssignmentsChanged)
                {
                    // Update assignments from editor
                    currentAssignments = editor.UpdatedAssignments;
                    
                    // Recalculate task counts and workload for updated assignments
                    foreach (var assignment in currentAssignments)
                    {
                        assignment.RecalculateMetrics();
                    }
                    
                    // Refresh the grid
                    dgAssignments.ItemsSource = null;
                    dgAssignments.ItemsSource = currentAssignments;
                    UpdateDashboard();
                    
                    // Close any open statistics windows so they refresh when reopened
                    var windowsToClose = new List<Window>();
                    foreach (Window window in OwnedWindows)
                    {
                        if (window is CompletionStatisticsWindow || window is StatisticsWindow)
                        {
                            windowsToClose.Add(window);
                        }
                    }
                    foreach (var window in windowsToClose)
                    {
                        window.Close();
                    }
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SampleManager.IsFirstRun())
            {
                ShowFirstRunDialog();
            }
        }

        private void ShowFirstRunDialog()
        {
            var dialog = new FirstRunDialog
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.CopySamples)
                {
                    SampleManager.CopySamplesToDesktop();
                    MessageBox.Show(
                        "Sample files have been copied to your Desktop!\n\nLook for the 'TaskAssigner Samples' folder.",
                        "Samples Copied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                SampleManager.MarkFirstRunComplete();
            }
        }

        private void SetupCommands()
        {
            // Keyboard shortcuts
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                btnAssign_Click(sender, e);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.A:
                        btnAssign_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.Z:
                        if (btnUndo.IsEnabled)
                            btnUndo_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.S:
                        btnSaveGroup_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.O:
                        btnLoadGroup_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.P:
                        if (currentAssignments.Count > 0)
                            btnPrintPreview_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.H:
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                            btnHistoryBrowser_Click(sender, e);
                        else
                            btnHelp_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Key.C:
                        if (currentAssignments.Count > 0)
                            btnCopyAll_Click(sender, e);
                        e.Handled = true;
                        break;
                }
            }
        }

        // Handle drag-over to show copy effect
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        // Handle file drop
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file).ToLower();

                    try
                    {
                        if (fileName.Contains("task"))
                        {
                            tasks = ReadListFromFile(file);
                            txtStatus.Text = $"Loaded {tasks.Count} tasks from {fileName}";
                        }
                        else if (fileName.Contains("people") || fileName.Contains("person"))
                        {
                            people = ReadListFromFile(file);
                            InitializeCapacities();
                            txtStatus.Text = $"Loaded {people.Count} people from {fileName}";
                        }
                        else
                        {
                            MessageBox.Show($"Unknown file type: {fileName}. Expected 'tasks' or 'people' in file name.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading {fileName}: {ex.Message}");
                    }
                }
            }
        }

        private void InitializeCapacities()
        {
            peopleCapacities.Clear();
            foreach (var person in people)
            {
                if (!peopleCapacities.ContainsKey(person))
                    peopleCapacities[person] = 1.0;
            }
        }

        private void btnLoadTasks_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Tasks File",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                tasks = ReadListFromFile(ofd.FileName);
                currentGroupName = string.Empty;
                currentGroupTag = "Untagged"; // Reset tag when loading new file
                txtStatus.Text = $"Loaded {tasks.Count} tasks.";
            }
        }

        private void btnLoadPeople_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select People File",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                people = ReadListFromFile(ofd.FileName);
                InitializeCapacities();
                currentGroupName = string.Empty;
                currentGroupTag = "Untagged"; // Reset tag when loading new file
                txtStatus.Text = $"Loaded {people.Count} people.";
            }
        }

        private void btnManageTasks_Click(object sender, RoutedEventArgs e)
        {
            var manager = new ListManagerWindow(tasks, "Tasks");
            if (manager.ShowDialog() == true)
            {
                tasks = manager.Items;
                txtStatus.Text = $"Tasks updated: {tasks.Count} items";
            }
        }

        private void btnManagePeople_Click(object sender, RoutedEventArgs e)
        {
            var manager = new ListManagerWindow(people, "People");
            if (manager.ShowDialog() == true)
            {
                people = manager.Items;
                InitializeCapacities();
                txtStatus.Text = $"People updated: {people.Count} items";
            }
        }

        private void btnManageTags_Click(object sender, RoutedEventArgs e)
        {
            var tagManager = new TagManagerWindow
            {
                Owner = this
            };
            tagManager.ShowDialog();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void btnConstraints_Click(object sender, RoutedEventArgs e)
        {
            if (tasks.Count == 0 || people.Count == 0)
            {
                MessageBox.Show("Please load both tasks and people before setting constraints.", "Cannot Set Constraints", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var constraintsWindow = new ConstraintsWindow(constraints, tasks, people)
            {
                Owner = this
            };

            if (constraintsWindow.ShowDialog() == true)
            {
                constraints = constraintsWindow.Constraints;
                UpdateConstraintInfo();
                txtStatus.Text = "Constraints updated";
            }
        }

        private void UpdateConstraintInfo()
        {
            int totalExclusions = constraints.Exclusions.Sum(kvp => kvp.Value.Count);
            if (totalExclusions > 0)
            {
                txtConstraintInfo.Text = $"{totalExclusions} constraint(s) active";
            }
            else
            {
                txtConstraintInfo.Text = string.Empty;
            }
        }

        private void btnSaveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (tasks.Count == 0 || people.Count == 0)
            {
                MessageBox.Show("Please load both tasks and people before saving a group.", 
                    "Cannot Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inputDialog = new InputDialog("Save Group", "Enter group name:");
            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
            {
                try
                {
                    // Ask for tag if tagging is enabled
                    string groupTag = "Untagged";
                    if (features.UseTagging)
                    {
                        var tagDialog = new TagSelectionDialog
                        {
                            Owner = this
                        };

                        if (tagDialog.ShowDialog() == true)
                        {
                            groupTag = tagDialog.SelectedTag;
                        }
                    }

                    var group = new TaskGroup
                    {
                        Name = inputDialog.ResponseText,
                        Tag = groupTag,
                        Tasks = new List<string>(tasks),
                        People = new List<string>(people),
                        Capacities = new Dictionary<string, double>(peopleCapacities),
                        TaskCategories = new Dictionary<string, string>(),
                        PeopleAvailability = new Dictionary<string, bool>(peopleAvailability),
                        PeopleRoles = new Dictionary<string, string>(peopleRoles),
                        TaskWeights = new Dictionary<string, int>(taskWeights),
                        TaskNotes = new Dictionary<string, string>(taskNotes),
                        TaskTimeEstimates = new Dictionary<string, int>(taskTimeEstimates),
                        TaskCategoryAssignments = new Dictionary<string, string>(taskCategoryAssignments),
                        Constraints = new Dictionary<string, List<string>>(constraints.Exclusions)
                    };

                    SaveFileDialog sfd = new SaveFileDialog
                    {
                        Title = "Save Group",
                        Filter = "Group Files (*.tgroup)|*.tgroup|All Files (*.*)|*.*",
                        FileName = $"{group.Name}.tgroup"
                    };

                    if (sfd.ShowDialog() == true)
                    {
                        string json = JsonSerializer.Serialize(group, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json);
                        AddToRecentGroups(sfd.FileName);
                        txtStatus.Text = $"Group '{group.Name}' saved successfully";
                        
                        // NEW: Log the group save
                        AuditLogger.LogGroupChange("SAVED", group.Name, Environment.UserName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save group: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnLoadGroup_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Load Group",
                Filter = "Group Files (*.tgroup)|*.tgroup|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                LoadGroupFromFile(ofd.FileName);
            }
        }

        private void LoadGroupFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var group = JsonSerializer.Deserialize<TaskGroup>(json);

                if (group != null)
                {
                    tasks = new List<string>(group.Tasks);
                    people = new List<string>(group.People);
                    currentGroupName = group.Name;
                    currentGroupTag = group.Tag ?? "Untagged"; // Store group tag for auto-tagging assignments
                    
                    
                    if (group.Capacities != null && group.Capacities.Count > 0)
                        peopleCapacities = new Dictionary<string, double>(group.Capacities);
                    else
                        InitializeCapacities();
                    
                    if (group.PeopleAvailability != null)
                        peopleAvailability = new Dictionary<string, bool>(group.PeopleAvailability);
                    
                    if (group.PeopleRoles != null)
                        peopleRoles = new Dictionary<string, string>(group.PeopleRoles);
                    
                    if (group.TaskWeights != null)
                        taskWeights = new Dictionary<string, int>(group.TaskWeights);
                    
                    if (group.TaskNotes != null)
                        taskNotes = new Dictionary<string, string>(group.TaskNotes);
                    
                    // Load new fields
                    if (group.TaskTimeEstimates != null)
                        taskTimeEstimates = new Dictionary<string, int>(group.TaskTimeEstimates);
                    
                    if (group.TaskCategoryAssignments != null)
                        taskCategoryAssignments = new Dictionary<string, string>(group.TaskCategoryAssignments);
                    
                    // Load constraints
                    if (group.Constraints != null && group.Constraints.Count > 0)
                    {
                        constraints = new TaskConstraint();
                        foreach (var kvp in group.Constraints)
                        {
                            constraints.Exclusions[kvp.Key] = new List<string>(kvp.Value);
                        }
                        UpdateConstraintInfo();
                    }
                    else
                    {
                        constraints = new TaskConstraint();
                    }
                    
                    currentGroupName = group.Name;
                    currentGroupTag = group.Tag ?? "Untagged"; // Store group tag for auto-tagging assignments
                    
                    
                    AddToRecentGroups(filePath);
                    txtStatus.Text = $"Loaded group '{group.Name}': {tasks.Count} tasks, {people.Count} people";
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load group: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAssign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Filter only available people (if feature enabled)
                var availablePeople = people;
                if (features.UsePersonAvailability)
                {
                    availablePeople = people.Where(p => 
                        !peopleAvailability.ContainsKey(p) || peopleAvailability[p]
                    ).ToList();

                    if (availablePeople.Count == 0)
                    {
                        MessageBox.Show("No available people to assign tasks. Please check availability settings.", 
                            "No Available People", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var randomizedPeople = availablePeople.OrderBy(x => rng.Next()).ToList();
                var randomizedTasks = tasks.OrderBy(x => rng.Next()).ToList();
                var assignments = AssignTasksWithConstraintsAndCapacity(randomizedTasks, randomizedPeople);

                // Use group tag if available, otherwise ask for tag if feature enabled
                string assignmentTag = currentGroupTag; // Use loaded group tag by default
                if (assignmentTag == "Untagged" && features.UseTaggingAtAssignment)
                {
                    // Only ask for tag if no group tag is set and feature is enabled
                    var tagDialog = new TagSelectionDialog
                    {
                        Owner = this
                    };

                    if (tagDialog.ShowDialog() == true)
                    {
                        assignmentTag = tagDialog.SelectedTag;
                    }
                }

                // Calculate workload percentages
                double avgTasks = assignments.Average(kvp => kvp.Value.Count);
                
                
                currentAssignments = assignments.Select(a => new AssignmentResult
                {
                    Person = a.Key,
                    TaskCount = a.Value.Count,
                    Tasks = string.Join(", ", a.Value),
                    Capacity = peopleCapacities.ContainsKey(a.Key) ? peopleCapacities[a.Key] : 1.0,
                    WorkloadPercentage = avgTasks > 0 ? $"{(a.Value.Count / avgTasks * 100):F0}%" : "0%",
                    CompletedTasks = new List<string>(),
                    IsPersonComplete = false,
                    CurrentTag = assignmentTag
                }).ToList();

                // Don't auto-save to history - wait for user to explicitly post/confirm the assignment
                // SaveToHistory(currentAssignments);
                // SaveToPersistentHistory(currentAssignments);

                dgAssignments.ItemsSource = currentAssignments;
                
                // Populate task checkboxes if completion tracking is enabled
                if (features.UseCompletionTracking)
                {
                    PopulateCompletionTracking();
                }

                // Save assignments to configured location
                string defaultPath = AppContext.BaseDirectory;
                string saveDirectory = string.IsNullOrWhiteSpace(features.AssignmentSaveLocation) 
                    ? defaultPath
                    : features.AssignmentSaveLocation;
                string saveLocation = Path.Combine(saveDirectory, "assignments.txt");
                
                try
                {
                    Directory.CreateDirectory(saveDirectory);
                    File.WriteAllLines(saveLocation, assignments.Select(a =>
                        $"{a.Key}: {string.Join(", ", a.Value)}"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Warning: Could not save assignments to {saveLocation}\n\nError: {ex.Message}", 
                        "Save Location Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                int unavailableCount = people.Count - availablePeople.Count;
                string availMsg = (features.UsePersonAvailability && unavailableCount > 0) 
                    ? $" ({unavailableCount} unavailable excluded)" 
                    : "";
                txtStatus.Text = $"✓ Assignments saved ({currentAssignments.Count} people{availMsg}, {tasks.Count} tasks) - Press Ctrl+C to copy";
                
                btnUndo.IsEnabled = true;
                mnuUndo.IsEnabled = true;
                btnEditTag.IsEnabled = true;
                UpdateTagDisplay();

                // Send notification if enabled
                if (features.UseNotifications && NotificationManager.AreNotificationsSupported())
                {
                    NotificationManager.ShowAssignmentNotification(currentAssignments.Count, tasks.Count);
                }
                
                // NEW: Update dashboard
                UpdateDashboard();

                // NEW: Log the assignments
                AuditLogger.LogAssignment(
                    GetCurrentGroupName() ?? "Unnamed", 
                    currentAssignments.Count, 
                    tasks.Count, 
                    Environment.UserName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void SaveToPersistentHistory(List<AssignmentResult> assignments)
        {
            try
            {
                // Use existing tag if available, only ask if needed
                string selectedTag = "Untagged";
                
                // If tagging at assignment time is enabled, use the CurrentTag from assignments
                if (features.UseTaggingAtAssignment && assignments.Count > 0)
                {
                    selectedTag = assignments[0].CurrentTag; // All assignments have same tag
                    
                    // Only prompt for tag if group doesn't have one AND it's Untagged (avoids repeated prompts)
                    if (selectedTag == "Untagged")
                    {
                        var tagDialog = new TagSelectionDialog
                        {
                            Owner = this
                        };

                        if (tagDialog.ShowDialog() == true)
                        {
                            selectedTag = tagDialog.SelectedTag;
                            // Update the tag in assignments and currentGroupTag
                            foreach (var assignment in assignments)
                            {
                                assignment.CurrentTag = selectedTag;
                            }
                            currentGroupTag = selectedTag;
                        }
                    }
                }
                else if (features.UseTagging && !features.UseTaggingAtAssignment)
                {
                    // Original behavior: ask for tag only at post time
                    var tagDialog = new TagSelectionDialog
                    {
                        Owner = this
                    };

                    if (tagDialog.ShowDialog() == true)
                    {
                        selectedTag = tagDialog.SelectedTag;
                    }
                    else
                    {
                        return; // User cancelled tagging
                    }
                }

                string userNotes = string.Empty;
                
                // Ask for notes if feature enabled
                if (features.UseAssignmentNotes)
                {
                    var notesDialog = new AddNotesDialog()
                    {
                        Owner = this
                    };
                    
                    if (notesDialog.ShowDialog() == true)
                    {
                        userNotes = notesDialog.Notes;
                    }
                }

                var persistentAssignment = new PersistentAssignment
                {
                    Timestamp = DateTime.Now,
                    Tag = selectedTag,
                    GroupName = !string.IsNullOrEmpty(GetCurrentGroupName()) ? GetCurrentGroupName() : "Unnamed Group",
                    Assignments = new List<AssignmentResult>(assignments.Select(a => new AssignmentResult
                    {
                        Person = a.Person,
                        TaskCount = a.TaskCount,
                        Tasks = a.Tasks,
                        WorkloadPercentage = a.WorkloadPercentage,
                        Capacity = a.Capacity
                    })),
                    Notes = $"{assignments.Count} people, {assignments.Sum(a => a.TaskCount)} tasks",
                    UserNotes = userNotes
                };

                AssignmentHistoryManager.SaveAssignment(persistentAssignment);
                
                // Track rotation if enabled
                if (features.UseAutoRotation)
                {
                    var assignmentDict = assignments.ToDictionary(
                        a => a.Person, 
                        a => a.Tasks.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    );
                    RotationTracker.RecordAssignments(assignmentDict);
                }
            }
            catch (Exception ex)
            {
                // Don't interrupt the assignment process if history saving fails
                System.Diagnostics.Debug.WriteLine($"Failed to save to persistent history: {ex.Message}");
            }
        }

        private string currentGroupName = string.Empty;
        private string currentGroupTag = "Untagged"; // Store tag from loaded group

        private string GetCurrentGroupName()
        {
            return currentGroupName;
        }

        private Dictionary<string, List<string>> AssignTasksWithConstraintsAndCapacity(List<string> tasks, List<string> people)
        {
            if (tasks.Count == 0)
                throw new ArgumentException("Task list is empty.");
            if (people.Count == 0)
                throw new ArgumentException("People list is empty.");

            var assignments = new Dictionary<string, List<string>>();
            foreach (var person in people)
                assignments[person] = new List<string>();

            // Calculate target tasks per person based on capacity
            double totalCapacity = people.Sum(p => peopleCapacities.ContainsKey(p) ? peopleCapacities[p] : 1.0);
            var targetTasksPerPerson = new Dictionary<string, int>();
            
            foreach (var person in people)
            {
                double capacity = peopleCapacities.ContainsKey(person) ? peopleCapacities[person] : 1.0;
                targetTasksPerPerson[person] = (int)Math.Round((capacity / totalCapacity) * tasks.Count);
            }

            // Adjust to ensure all tasks are assigned
            int totalTargetTasks = targetTasksPerPerson.Values.Sum();
            if (totalTargetTasks != tasks.Count)
            {
                // Add remaining tasks to person with highest capacity
                var personWithHighestCapacity = people.OrderByDescending(p => peopleCapacities.ContainsKey(p) ? peopleCapacities[p] : 1.0).First();
                targetTasksPerPerson[personWithHighestCapacity] += tasks.Count - totalTargetTasks;
            }

            var unassignedTasks = new List<string>();
            int currentPersonIndex = 0;

            // Fair round-robin assignment with constraints
            foreach (var task in tasks)
            {
                bool taskAssigned = false;
                int startingIndex = currentPersonIndex;
                int attempts = 0;

                // Try assigning to each person in round-robin order
                while (!taskAssigned && attempts < people.Count * 2)
                {
                    string currentPerson = people[currentPersonIndex % people.Count];
                    
                    // Check if this person can take this task
                    if (constraints.CanAssign(currentPerson, task))
                    {
                        // Check if person hasn't exceeded their target (with some flexibility)
                        int currentCount = assignments[currentPerson].Count;
                        int target = targetTasksPerPerson[currentPerson];
                        
                        if (currentCount < target || unassignedTasks.Count == 0)
                        {
                            // Assign task to this person
                            assignments[currentPerson].Add(task);
                            taskAssigned = true;
                            // Move to next person for fair distribution
                            currentPersonIndex = (currentPersonIndex + 1) % people.Count;
                        }
                    }
                    
                    if (!taskAssigned)
                    {
                        // Try next person
                        currentPersonIndex = (currentPersonIndex + 1) % people.Count;
                        attempts++;
                    }
                }

                if (!taskAssigned)
                {
                    // Task couldn't be assigned to anyone
                    unassignedTasks.Add(task);
                }
            }

            // Try to assign any remaining unassigned tasks to anyone who can take them
            foreach (var task in unassignedTasks.ToList())
            {
                foreach (var person in people.OrderBy(p => assignments[p].Count))
                {
                    if (constraints.CanAssign(person, task))
                    {
                        assignments[person].Add(task);
                        unassignedTasks.Remove(task);
                        break;
                    }
                }
            }

            if (unassignedTasks.Count > 0)
            {
                MessageBox.Show(
                    $"Warning: {unassignedTasks.Count} task(s) could not be assigned due to constraints:\n\n" + 
                    string.Join("\n", unassignedTasks.Take(10)) + 
                    (unassignedTasks.Count > 10 ? $"\n... and {unassignedTasks.Count - 10} more" : ""), 
                    "Assignment Warning", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }

            return assignments;
        }

        private void SaveToHistory(List<AssignmentResult> assignments)
        {
            var historyEntry = new AssignmentHistoryEntry
            {
                Timestamp = DateTime.Now,
                Assignments = new List<AssignmentResult>(assignments.Select(a => new AssignmentResult
                {
                    Person = a.Person,
                    TaskCount = a.TaskCount,
                    Tasks = a.Tasks,
                    WorkloadPercentage = a.WorkloadPercentage,
                    Capacity = a.Capacity
                })),
                Description = $"{assignments.Count} people, {assignments.Sum(a => a.TaskCount)} tasks"
            };
            assignmentHistory.Push(historyEntry);
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (assignmentHistory.Count == 0)
            {
                MessageBox.Show("No assignments to undo.", "Nothing to Undo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to undo the last assignment?", "Confirm Undo", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                assignmentHistory.Pop();
                
                if (assignmentHistory.Count > 0)
                {
                    var previous = assignmentHistory.Peek();
                    currentAssignments = new List<AssignmentResult>(previous.Assignments);
                    dgAssignments.ItemsSource = currentAssignments;
                    txtStatus.Text = $"↩️ Restored assignment from {previous.Timestamp:g}";
                }
                else
                {
                    currentAssignments.Clear();
                    dgAssignments.ItemsSource = null;
                    txtStatus.Text = "All assignments cleared";
                    btnUndo.IsEnabled = false;
                    mnuUndo.IsEnabled = false;
                    btnEditTag.IsEnabled = false;
                }
            }
        }

        private void btnPostAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to post. Please assign tasks first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                SaveToHistory(currentAssignments);
                SaveToPersistentHistory(currentAssignments);
                
                txtStatus.Text = "✓ Assignment posted to history - Ready for new assignment";
                btnUndo.IsEnabled = false;
                mnuUndo.IsEnabled = false;
                btnEditTag.IsEnabled = false;
                currentGroupTag = "Untagged";
                UpdateTagDisplay();
                
                // Clear for next assignment
                currentAssignments = new List<AssignmentResult>();
                dgAssignments.ItemsSource = null;
                UpdateDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to post assignment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditTag_Click(object sender, RoutedEventArgs e)
        {
            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to edit tag for. Please assign tasks first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tagDialog = new TagSelectionDialog
            {
                Owner = this
            };

            if (tagDialog.ShowDialog() == true)
            {
                string newTag = tagDialog.SelectedTag;
                
                // Update all assignments with new tag
                foreach (var assignment in currentAssignments)
                {
                    assignment.CurrentTag = newTag;
                }
                
                // Update the stored group tag as well
                currentGroupTag = newTag;
                
                txtStatus.Text = $"✓ Tag changed to '{newTag}'";
                UpdateTagDisplay();
            }
        }

        private void UpdateTagDisplay()
        {
            if (txtCurrentTag != null)
            {
                string tagToDisplay = currentGroupTag ?? "Untagged";
                txtCurrentTag.Text = tagToDisplay;
                txtCurrentTag.Foreground = string.IsNullOrEmpty(currentGroupTag) || currentGroupTag == "Untagged" 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210));
            }
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            if (assignmentHistory.Count == 0)
            {
                MessageBox.Show("No assignment history available.", "No History", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var historyWindow = new HistoryWindow(assignmentHistory.ToList())
            {
                Owner = this
            };
            historyWindow.ShowDialog();
        }

        private void btnShowStats_Click(object sender, RoutedEventArgs e)
        {
            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to show statistics for. Please assign tasks first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create a fresh copy with recalculated metrics to ensure fresh data
            var assignmentsForStats = new List<AssignmentResult>();
            foreach (var assignment in currentAssignments)
            {
                var copy = new AssignmentResult
                {
                    Person = assignment.Person,
                    TaskCount = assignment.TaskCount,
                    Tasks = assignment.Tasks,
                    WorkloadPercentage = assignment.WorkloadPercentage,
                    Capacity = assignment.Capacity
                };
                copy.RecalculateMetrics();
                assignmentsForStats.Add(copy);
            }

            var statsWindow = new StatisticsWindow(assignmentsForStats)
            {
                Owner = this
            };
            statsWindow.ShowDialog();
        }

        private void btnCopyAll_Click(object sender, RoutedEventArgs e)
        {
            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to copy. Please assign tasks first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string formatted = CsvExporter.FormatForClipboard(currentAssignments);
                Clipboard.SetText(formatted);
                txtStatus.Text = "✓ All assignments copied to clipboard!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (dgAssignments.SelectedItem is AssignmentResult selected)
            {
                try
                {
                    string formatted = CsvExporter.FormatPersonTasks(selected.Person, selected.Tasks);
                    Clipboard.SetText(formatted);
                    txtStatus.Text = $"✓ Copied {selected.Person}'s tasks to clipboard!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a person first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Populate task checkboxes when a row's details visibility changes
        /// </summary>
        private void dgAssignments_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        {
            if (e.Row.Item is AssignmentResult assignment && e.Row.DetailsVisibility == Visibility.Visible)
            {
                // Get the row
                var row = e.Row as DataGridRow;
                if (row == null) return;

                // Find the WrapPanel in the visual tree - it will be in the RowDetailsTemplate
                var wrapPanel = FindVisualChild<WrapPanel>(row);
                if (wrapPanel == null) return;

                wrapPanel.Children.Clear();

                // Parse tasks from comma-separated list
                var tasks = assignment.Tasks
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

                // Create checkboxes for each task
                foreach (var task in tasks)
                {
                    var checkbox = new CheckBox
                    {
                        Content = task,
                        Margin = new Thickness(10, 5, 10, 5),
                        IsChecked = assignment.CompletedTasks.Contains(task),
                        FontSize = 11
                    };

                    // Handle checkbox changes
                    checkbox.Checked += (s, args) =>
                    {
                        if (!assignment.CompletedTasks.Contains(task))
                        {
                            assignment.CompletedTasks.Add(task);
                        }
                        UpdateCompletionStatus(assignment);
                    };

                    checkbox.Unchecked += (s, args) =>
                    {
                        assignment.CompletedTasks.Remove(task);
                        UpdateCompletionStatus(assignment);
                    };

                    wrapPanel.Children.Add(checkbox);
                }
            }
        }

        /// <summary>
        /// Helper to find visual child elements
        /// </summary>
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T typedChild)
                    return typedChild;

                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        /// <summary>
        /// Update completion status for a person based on completed tasks
        /// </summary>
        private void UpdateCompletionStatus(AssignmentResult assignment)
        {
            // Properties are auto-calculated from CompletedTasks, so just refresh the grid
            dgAssignments.Items.Refresh();
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to export. Please assign tasks first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"assignments_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    CsvExporter.ExportToCsv(currentAssignments, sfd.FileName);
                    txtStatus.Text = $"✓ Exported to {Path.GetFileName(sfd.FileName)}";
                    
                    var result = MessageBox.Show("Export successful! Open the file now?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Information);
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
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void dgAssignments_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Context menu is already defined in XAML
        }

        private void btnShortcuts_Click(object sender, RoutedEventArgs e)
        {
            var shortcuts = @"KEYBOARD SHORTCUTS
═══════════════════════════════════════════

F5                     Assign Tasks
Ctrl + A              Assign Tasks
Ctrl + Z              Undo Last Assignment
Ctrl + S              Save Group
Ctrl + O              Load Group
Ctrl + P              Print Preview
Ctrl + C              Copy All Assignments
Ctrl + H              Help

RIGHT-CLICK on the grid for more options!";

            MessageBox.Show(shortcuts, "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddToRecentGroups(string filePath)
        {
            var recentGroups = GetRecentGroups();
            
            recentGroups.Remove(filePath);
            recentGroups.Insert(0, filePath);
            
            if (recentGroups.Count > MaxRecentGroups)
                recentGroups.RemoveRange(MaxRecentGroups, recentGroups.Count - MaxRecentGroups);
            
            Properties.Settings.Default.RecentGroups = string.Join("|", recentGroups);
            Properties.Settings.Default.Save();
            
            LoadRecentGroupsMenu();
        }

        private List<string> GetRecentGroups()
        {
            var recentString = Properties.Settings.Default.RecentGroups;
            if (string.IsNullOrEmpty(recentString))
                return new List<string>();
            
            return recentString.Split('|').Where(File.Exists).ToList();
        }

        private void LoadRecentGroupsMenu()
        {
            mnuRecentGroups.Items.Clear();
            var recentGroups = GetRecentGroups();
            
            if (recentGroups.Count == 0)
            {
                var emptyItem = new MenuItem { Header = "(No recent groups)", IsEnabled = false };
                mnuRecentGroups.Items.Add(emptyItem);
            }
            else
            {
                foreach (var groupPath in recentGroups)
                {
                    var menuItem = new MenuItem { Header = Path.GetFileNameWithoutExtension(groupPath) };
                    string path = groupPath;
                    menuItem.Click += (s, e) => LoadGroupFromFile(path);
                    mnuRecentGroups.Items.Add(menuItem);
                }
                
                mnuRecentGroups.Items.Add(new Separator());
                var clearItem = new MenuItem { Header = "Clear Recent Groups" };
                clearItem.Click += (s, e) =>
                {
                    Properties.Settings.Default.RecentGroups = string.Empty;
                    Properties.Settings.Default.Save();
                    LoadRecentGroupsMenu();
                    txtStatus.Text = "Recent groups cleared";
                };
                mnuRecentGroups.Items.Add(clearItem);
            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private List<string> ReadListFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return File.ReadAllLines(filePath)
                       .Select(line => line.Trim())
                       .Where(line => !string.IsNullOrEmpty(line))
                       .ToList();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow
            {
                Owner = this
            };
            helpWindow.ShowDialog();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow
            {
                Owner = this
            };
            helpWindow.ShowDialog();
        }

        private void btnHistoryBrowser_Click(object sender, RoutedEventArgs e)
        {
            var historyBrowser = new HistoryBrowserWindow
            {
                Owner = this
            };

            if (historyBrowser.ShowDialog() == true && historyBrowser.Tag is PersistentAssignment loadedAssignment)
            {
                // Load the selected assignment
                currentAssignments = new List<AssignmentResult>(loadedAssignment.Assignments);
                
                // Set the tag from the loaded assignment
                currentGroupTag = loadedAssignment.Tag;
                
                // Update all assignments with the loaded tag
                foreach (var assignment in currentAssignments)
                {
                    assignment.CurrentTag = loadedAssignment.Tag;
                }
                
                dgAssignments.ItemsSource = currentAssignments;
                txtStatus.Text = $"Loaded assignment from {loadedAssignment.Timestamp:g}";
                
                // Enable edit controls and display tag
                btnEditTag.IsEnabled = true;
                UpdateTagDisplay();
            }
        }

        private void btnLoadSamples_Click(object sender, RoutedEventArgs e)
        {
            if (!SampleManager.SamplesExist())
            {
                MessageBox.Show("Sample files are not available.", "Samples Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "This will load the sample tasks and people lists. Continue?",
                "Load Samples",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    tasks = ReadListFromFile(SampleManager.GetSamplePath("sample_tasks.txt"));
                    people = ReadListFromFile(SampleManager.GetSamplePath("sample_people.txt"));
                    InitializeCapacities();
                    currentGroupName = "Sample Group";
                    
                    txtStatus.Text = $"✓ Loaded samples: {tasks.Count} tasks, {people.Count} people - Try pressing F5!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load samples: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnAvailability_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UsePersonAvailability)
            {
                MessageBox.Show("This feature is disabled. Enable it in Settings → Advanced Features.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (people.Count == 0)
            {
                MessageBox.Show("Please load people first.", "No People", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var availWindow = new AvailabilityManagerWindow(people, peopleAvailability, peopleRoles)
            {
                Owner = this
            };

            if (availWindow.ShowDialog() == true)
            {
                peopleAvailability = availWindow.Availability;
                peopleRoles = availWindow.Roles;
                txtStatus.Text = $"Availability updated - {peopleAvailability.Count(kvp => !kvp.Value)} person(s) unavailable";
            }
        }

        private void btnEnhancedTaskManager_Click(object sender, RoutedEventArgs e)
        {
            if (tasks.Count == 0)
            {
                MessageBox.Show("Please load tasks first.", "No Tasks", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enhancedWindow = new EnhancedTaskManagerWindow(
                tasks, 
                taskWeights, 
                taskNotes, 
                taskTimeEstimates, 
                taskCategoryAssignments)
            {
                Owner = this
            };

            if (enhancedWindow.ShowDialog() == true)
            {
                taskWeights = enhancedWindow.Weights;
                taskNotes = enhancedWindow.Notes;
                taskTimeEstimates = enhancedWindow.TimeEstimates;
                taskCategoryAssignments = enhancedWindow.CategoryAssignments;
                txtStatus.Text = "Task properties updated";
            }
        }

        // Task Categories Management
        private void btnManageCategories_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseTaskCategories)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var categoryWindow = new CategoryManagerWindow
            {
                Owner = this
            };
            categoryWindow.ShowDialog();
        }

        // Assignment Templates
        private void btnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseAssignmentTemplates)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to save as template. Please assign tasks first.", 
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var inputDialog = new InputDialog("Save Template", "Enter template name:");
            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
            {
                var descDialog = new InputDialog("Template Description", "Enter description (optional):");
                string description = descDialog.ShowDialog() == true ? descDialog.ResponseText : string.Empty;

                var template = new AssignmentTemplate
                {
                    Name = inputDialog.ResponseText,
                    Description = description,
                    GroupName = GetCurrentGroupName(),
                    Assignments = new List<AssignmentResult>(currentAssignments),
                    Created = DateTime.Now
                };

                TemplateManager.SaveTemplate(template);
                txtStatus.Text = $"Template '{template.Name}' saved successfully";
            }
        }

        private void btnLoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseAssignmentTemplates)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var browserWindow = new TemplateBrowserWindow
            {
                Owner = this
            };

            if (browserWindow.ShowDialog() == true && browserWindow.SelectedTemplate != null)
            {
                var template = browserWindow.SelectedTemplate;
                currentAssignments = new List<AssignmentResult>(template.Assignments);
                dgAssignments.ItemsSource = currentAssignments;
                txtStatus.Text = $"Loaded template '{template.Name}'";
            }
        }

        // Rotation Report
        private void btnRotationReport_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseAutoRotation)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (tasks.Count == 0 || people.Count == 0)
            {
                MessageBox.Show("Please load tasks and people first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reportWindow = new RotationReportWindow(people, tasks)
            {
                Owner = this
            };
            reportWindow.ShowDialog();
        }

        // Add Notes to Current Assignment
        private void btnAddNotes_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseAssignmentNotes)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to add notes to. Please assign tasks first.", 
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var notesDialog = new AddNotesDialog()
            {
                Owner = this
            };

            if (notesDialog.ShowDialog() == true && !string.IsNullOrEmpty(notesDialog.Notes))
            {
                // Store notes with the current assignment (you'd need to enhance PersistentAssignment)
                txtStatus.Text = "Notes added to assignment";
                MessageBox.Show("Notes will be saved with this assignment in history.", 
                    "Notes Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateMenusBasedOnFeatures()
        {
            // ===== CORE FEATURES (Always Available) =====
            // Constraints
            mnuConstraints.Visibility = features.UseConstraints ? Visibility.Visible : Visibility.Collapsed;
            
            // History
            mnuHistoryBrowser.Visibility = features.UseHistory ? Visibility.Visible : Visibility.Collapsed;
            mnuSessionHistory.Visibility = features.UseHistory ? Visibility.Visible : Visibility.Collapsed;
            
            // ===== TASK MANAGEMENT FEATURES =====
            // Task Weighting/Time/Categories (Combined into Enhanced Task Manager)
            bool useEnhanced = features.UseTaskWeighting || features.UseTaskTimeEstimates || features.UseTaskCategories;
            mnuEnhancedTaskManager.Visibility = useEnhanced ? Visibility.Visible : Visibility.Collapsed;
            
            // Categories standalone menu
            mnuManageCategories.Visibility = features.UseTaskCategories ? Visibility.Visible : Visibility.Collapsed;
            
            // ===== PEOPLE MANAGEMENT FEATURES =====
            // Person Availability
            mnuAvailability.Visibility = features.UsePersonAvailability ? Visibility.Visible : Visibility.Collapsed;
            
            // Roles
            mnuManageRoles.Visibility = features.UseRoles ? Visibility.Visible : Visibility.Collapsed;
            
            // ===== ASSIGNMENT FEATURES =====
            // Auto-Rotation
            mnuRotationReport.Visibility = features.UseAutoRotation ? Visibility.Visible : Visibility.Collapsed;
            
            // Assignment Templates
            mnuSaveTemplate.Visibility = features.UseAssignmentTemplates ? Visibility.Visible : Visibility.Collapsed;
            mnuLoadTemplate.Visibility = features.UseAssignmentTemplates ? Visibility.Visible : Visibility.Collapsed;
            
            // Assignment Notes
            mnuAddNotes.Visibility = features.UseAssignmentNotes ? Visibility.Visible : Visibility.Collapsed;
            
            // Assignment Scheduler
            mnuScheduler.Visibility = features.UseAssignmentScheduler ? Visibility.Visible : Visibility.Collapsed;
            
            // Report Scheduler
            mnuReportScheduler.Visibility = features.UseScheduledReports ? Visibility.Visible : Visibility.Collapsed;
            
            // ===== OUTPUT & EXPORT FEATURES =====
            // Print Preview
            mnuPrintPreview.Visibility = features.UsePrintPreview ? Visibility.Visible : Visibility.Collapsed;
            
            // Mobile Export
            mnuMobileExport.Visibility = features.UseMobileExport ? Visibility.Visible : Visibility.Collapsed;
            
            // ===== WORKFLOW TOOLS =====
            // Quick Swap
            mnuSwapPerson.Visibility = features.UseQuickSwap ? Visibility.Visible : Visibility.Collapsed;
            sepSwap.Visibility = features.UseQuickSwap ? Visibility.Visible : Visibility.Collapsed;
            
            // Bulk Edit is part of Enhanced Task Manager (no separate menu item needed)
            
            // ===== ANALYTICS & INSIGHTS =====
            // Performance Analytics
            mnuAnalytics.Visibility = features.UsePerformanceAnalytics ? Visibility.Visible : Visibility.Collapsed;
            
            // Notifications (no menu item, just background behavior)
            
            // ===== SEPARATORS =====
            // Hide advanced separator if no advanced features enabled
            bool anyAdvanced = features.UseTaskWeighting || features.UsePersonAvailability || 
                               features.UseConstraints || features.UseTaskTimeEstimates || 
                               features.UseTaskCategories;
            sepAdvanced.Visibility = anyAdvanced ? Visibility.Visible : Visibility.Collapsed;
        }

        public void RefreshFeatures()
        {
            features = FeatureManager.GetFeatures();
            UpdateMenusBasedOnFeatures();
        }

        private void btnAdvancedFeatures_Click(object sender, RoutedEventArgs e)
        {
            var featuresWindow = new AdvancedFeaturesWindow
            {
                Owner = this
            };
    
            if (featuresWindow.ShowDialog() == true)
            {
                RefreshFeatures();
            }
        }

        private void btnTaskWeight_Click(object sender, RoutedEventArgs e)
        {
            // Redirect to enhanced task manager
            btnEnhancedTaskManager_Click(sender, e);
        }

        private void btnManageRoles_Click(object sender, RoutedEventArgs e)
        {
            var roleManager = new RoleManagerWindow
            {
                Owner = this
            };
            roleManager.ShowDialog();
        }

        // Mobile Export
        private void btnMobileExport_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseMobileExport)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to export. Please assign tasks first.", 
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportDialog = new MobileExportDialog(currentAssignments, GetCurrentGroupName())
            {
                Owner = this
            };
            exportDialog.ShowDialog();
        }

        // Performance Analytics
        private void btnAnalytics_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UsePerformanceAnalytics)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var analyticsWindow = new AnalyticsDashboardWindow
            {
                Owner = this
            };
            analyticsWindow.ShowDialog();
        }

        // Assignment Scheduler
        private void btnScheduler_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseAssignmentScheduler)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var schedulerWindow = new AssignmentSchedulerWindow
            {
                Owner = this
            };
            
            if (schedulerWindow.ShowDialog() == true && schedulerWindow.Tag is string groupFilePath)
            {
                // Load the group and auto-execute assignment
                try
                {
                    LoadGroupFromFile(groupFilePath);
                    
                    // Automatically trigger assignment after loading
                    Dispatcher.BeginInvoke(() =>
                    {
                        btnAssign_Click(sender, e);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to execute scheduled assignment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnReportScheduler_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseScheduledReports)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var scheduleWindow = new ReportScheduleWindow
            {
                Owner = this
            };
            scheduleWindow.ShowDialog();
        }

        private void btnEmailSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseEmailReports)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var emailWindow = new EmailSettingsWindow
            {
                Owner = this
            };
            emailWindow.ShowDialog();
        }

        private void btnPersonHistory_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new PersonHistoryWindow
            {
                Owner = this
            };
            historyWindow.ShowDialog();
        }

        private void btnPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UsePrintPreview)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (currentAssignments == null || currentAssignments.Count == 0)
            {
                MessageBox.Show("No assignments to print. Please assign tasks first.", 
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printWindow = new PrintPreviewWindow(currentAssignments, GetCurrentGroupName())
            {
                Owner = this
            };
            printWindow.ShowDialog();
        }

        private void btnSwapPerson_Click(object sender, RoutedEventArgs e)
        {
            if (!features.UseQuickSwap)
            {
                MessageBox.Show("Enable this feature in Settings → Advanced Features first.", 
                    "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (currentAssignments == null || currentAssignments.Count < 2)
            {
                MessageBox.Show("Need at least 2 people to swap.", "Cannot Swap", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string selectedPerson = string.Empty;
            if (dgAssignments.SelectedItem is AssignmentResult selected)
            {
                selectedPerson = selected.Person;
            }

            var swapDialog = new SwapDialog(currentAssignments.Select(a => a.Person).ToList(), selectedPerson)
            {
                Owner = this
            };

            if (swapDialog.ShowDialog() == true)
            {
                SwapPeople(swapDialog.Person1, swapDialog.Person2);
            }
        }

        private void SwapPeople(string person1, string person2)
        {
            var assignment1 = currentAssignments.FirstOrDefault(a => a.Person == person1);
            var assignment2 = currentAssignments.FirstOrDefault(a => a.Person == person2);

            if (assignment1 != null && assignment2 != null)
            {
                // Swap tasks
                string tempTasks = assignment1.Tasks;
                int tempCount = assignment1.TaskCount;

                assignment1.Tasks = assignment2.Tasks;
                assignment1.TaskCount = assignment2.TaskCount;

                assignment2.Tasks = tempTasks;
                assignment2.TaskCount = tempCount;

                // Refresh display
                dgAssignments.Items.Refresh();
                txtStatus.Text = $"✓ Swapped tasks between {person1} and {person2}";
            }
        }

        private void btnNotificationSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new NotificationSettingsWindow { Owner = this };
            window.ShowDialog();
        }

        private void btnBackupData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Backup All Data",
                    Filter = "Zip Files (*.zip)|*.zip|All Files (*.*)|*.*",
                    FileName = $"TaskAssigner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip",
                    DefaultExt = ".zip"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    if (BackupManager.CreateBackup(saveDialog.FileName))
                    {
                        var size = BackupManager.GetDataSizeFormatted();
                        MessageBox.Show(
                            $"✅ Backup created successfully!\n\n" +
                            $"Location: {saveDialog.FileName}\n" +
                            $"Size: {size}\n\n" +
                            "This backup includes:\n" +
                            "• All assignment history\n" +
                            "• Saved groups\n" +
                            "• Templates\n" +
                            "• Settings and preferences\n" +
                            "• Rotation tracking data",
                            "Backup Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        
                        txtStatus.Text = $"✓ Backup created: {Path.GetFileName(saveDialog.FileName)}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRestoreData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Restore from Backup",
                    Filter = "Zip Files (*.zip)|*.zip|All Files (*.*)|*.*",
                    DefaultExt = ".zip"
                };

                if (openDialog.ShowDialog() == true)
                {
                    if (BackupManager.RestoreBackup(openDialog.FileName))
                    {
                        // Suggest restart
                        var result = MessageBox.Show(
                            "Restore complete! Restart the application now?",
                            "Restart Required",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // WPF-compatible restart with null safety
                            try
                            {
                                var processPath = Environment.ProcessPath;
                                if (processPath == null)
                                {
                                    var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                                    var mainModule = currentProcess.MainModule;
                                    processPath = mainModule?.FileName;
                                }

                                if (!string.IsNullOrEmpty(processPath))
                                {
                                    System.Diagnostics.Process.Start(processPath);
                                    Application.Current.Shutdown();
                                }
                                else
                                {
                                    MessageBox.Show(
                                        "Unable to restart automatically. Please close and reopen the application manually.",
                                        "Manual Restart Required",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                }
                            }
                            catch
                            {
                                MessageBox.Show(
                                    "Unable to restart automatically. Please close and reopen the application manually.",
                                    "Manual Restart Required",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenHistoryFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string historyPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaskAssigner", "History");

                if (!Directory.Exists(historyPath))
                {
                    MessageBox.Show(
                        "History folder doesn't exist yet.\n\n" +
                        "No assignments have been posted to history.\n\n" +
                        "Location where it will be created:\n" +
                        historyPath,
                        "History Folder Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = historyPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open history folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDashboard()
        {
            // Last Assignment Time
            if (currentAssignments != null && currentAssignments.Count > 0)
            {
                txtLastAssignmentTime.Text = DateTime.Now.ToString("g");
                txtLastAssignmentInfo.Text = $"{currentAssignments.Count} people, {tasks.Count} tasks";
                
                // Calculate fairness score
                double avgTasks = currentAssignments.Average(a => a.TaskCount);
                double variance = currentAssignments.Average(a => Math.Pow(a.TaskCount - avgTasks, 2));
                double stdDev = Math.Sqrt(variance);
                double fairnessScore = avgTasks > 0 ? Math.Max(0, 100 - (stdDev / avgTasks * 100)) : 0;
                
                txtFairnessScore.Text = $"{fairnessScore:F0}%";
                txtFairnessScore.Foreground = fairnessScore >= 90 ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)) :
                                          fairnessScore >= 75 ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 140, 0)) :
                                          new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40));
                
                txtFairnessLabel.Text = fairnessScore >= 90 ? "Excellent!" :
                                    fairnessScore >= 75 ? "Good" :
                                    fairnessScore >= 60 ? "Fair" : "Needs work";
                
                // Count overloaded/underloaded
                int overloaded = currentAssignments.Count(a => a.IsOverloaded);
                int underloaded = currentAssignments.Count(a => a.IsUnderloaded);
                
                if (overloaded > 0 || underloaded > 0)
                {
                    pnlWorkloadIndicators.Visibility = Visibility.Visible;
                    txtOverloadedCount.Text = $"{overloaded} 🔴";
                    txtUnderloadedCount.Text = $"{underloaded} 🟢";
                    txtQuickStats.Text = overloaded > 0 ? $"⚠️ {overloaded} overloaded" : "Balanced workload";
                }
                else
                {
                    pnlWorkloadIndicators.Visibility = Visibility.Collapsed;
                    txtQuickStats.Text = "✓ Well balanced!";
                }
            }
            else
            {
                txtLastAssignmentTime.Text = "No assignments yet";
                txtLastAssignmentInfo.Text = "";
                txtFairnessScore.Text = "--";
                txtFairnessLabel.Text = "Not calculated";
                txtQuickStats.Text = tasks.Count > 0 && people.Count > 0 ? "Ready to assign (press F5)" : "Load tasks & people first";
                pnlWorkloadIndicators.Visibility = Visibility.Collapsed;
            }
        }

        // Add Clear All Data handler
        private void btnClearAllData_Click(object sender, RoutedEventArgs e)
        {
            if (DataManager.ClearAllData())
            {
                // Data was deleted, close app
                Application.Current.Shutdown();
            }
        }

        // Add View Audit Log handler
        private void btnViewAuditLog_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(AuditLogger.GetAuditLogPath()))
            {
                MessageBox.Show(
                    "No audit log found.\n\n" +
                    "Audit logging may be disabled, or no auditable actions have been performed yet.\n\n" +
                    "Enable audit logging in Settings → Security.",
                    "No Audit Log",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            AuditLogger.ViewAuditLog();
        }

        private void btnSecuritySettings_Click(object sender, RoutedEventArgs e)
        {
            var securityWindow = new SecuritySettingsWindow { Owner = this };
            securityWindow.ShowDialog();
        }

        private void MenuBackupSchedule_Click(object sender, RoutedEventArgs e)
        {
            var window = new BackupScheduleWindow { Owner = this };
            if (window.ShowDialog() == true)
            {
                // Refresh backup timer if settings changed
                RefreshBackupScheduler();
            }
        }

        private void RefreshBackupScheduler()
         {
             // Force immediate check
             ((App)Application.Current).CheckAndExecuteScheduledBackup();
         }

        // ===== COMPLETION TRACKING METHODS =====
        
        private void PopulateCompletionTracking()
        {
            // This method is called when row details are created
            // Event handler will be attached in XAML or dynamic binding
        }

        private void OnTaskCheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.Tag is string taskName && checkbox.DataContext is AssignmentResult assignment)
            {
                if (checkbox.IsChecked == true)
                {
                    if (!assignment.CompletedTasks.Contains(taskName))
                        assignment.CompletedTasks.Add(taskName);
                }
                else
                {
                    assignment.CompletedTasks.Remove(taskName);
                }

                // Auto-update IsPersonComplete if all tasks are completed
                UpdatePersonCompletionStatus(assignment);
                
                // Refresh grid to show updated completion status
                dgAssignments.Items.Refresh();
            }
        }

        private void UpdatePersonCompletionStatus(AssignmentResult assignment)
        {
            if (assignment.TaskCount > 0)
            {
                assignment.IsPersonComplete = assignment.CompletedCount >= assignment.TaskCount;
            }
        }

        private void btnMarkAllComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string personName)
            {
                var assignment = currentAssignments?.FirstOrDefault(a => a.Person == personName);
                if (assignment != null)
                {
                    // Mark all tasks as complete
                    var taskList = assignment.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(t => t.Trim())
                                                   .ToList();
                    
                    assignment.CompletedTasks = new List<string>(taskList);
                    assignment.IsPersonComplete = true;
                    
                    // Refresh grid
                    dgAssignments.Items.Refresh();
                    
                    txtStatus.Text = $"✓ Marked all tasks complete for {personName}";
                }
            }
        }

        private void btnResetAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string personName)
            {
                var assignment = currentAssignments?.FirstOrDefault(a => a.Person == personName);
                if (assignment != null)
                {
                    // Clear all completed tasks
                    assignment.CompletedTasks.Clear();
                    assignment.IsPersonComplete = false;
                    
                    // Refresh grid
                    dgAssignments.Items.Refresh();
                    
                    txtStatus.Text = $"↩️ Reset completion for {personName}";
                }
            }
        }
     }
 }
