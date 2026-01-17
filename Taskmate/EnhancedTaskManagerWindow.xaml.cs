using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class EnhancedTaskManagerWindow : Window
    {
        public class EnhancedTask : INotifyPropertyChanged
        {
            private string category = "General";
            private string difficultyDisplay = "Medium (2)";
            private int timeEstimate = 15;
            private string notes = string.Empty;

            public string Name { get; set; } = string.Empty;
            public string Category
            {
                get => category;
                set
                {
                    category = value ?? "General";
                    OnPropertyChanged(nameof(Category));
                }
            }
            public int Weight { get; set; } = 2;
            public string DifficultyDisplay
            {
                get => difficultyDisplay;
                set
                {
                    difficultyDisplay = value ?? "Medium (2)";
                    Weight = difficultyDisplay switch
                    {
                        "Easy (1)" => 1,
                        "Medium (2)" => 2,
                        "Hard (3)" => 3,
                        _ => 2
                    };
                    OnPropertyChanged(nameof(DifficultyDisplay));
                }
            }
            public int TimeEstimate
            {
                get => timeEstimate;
                set
                {
                    timeEstimate = value < 1 ? 1 : value;
                    OnPropertyChanged(nameof(TimeEstimate));
                }
            }
            public string Notes
            {
                get => notes;
                set
                {
                    notes = value ?? string.Empty;
                    OnPropertyChanged(nameof(Notes));
                }
            }
            public List<string> DifficultyOptions { get; set; } = new List<string> { "Easy (1)", "Medium (2)", "Hard (3)" };
            public List<string> Categories { get; set; } = new List<string>();

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Dictionary<string, int> Weights { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, string> Notes { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, int> TimeEstimates { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, string> CategoryAssignments { get; private set; } = new Dictionary<string, string>();

        private ObservableCollection<EnhancedTask> taskList = new ObservableCollection<EnhancedTask>();

        public EnhancedTaskManagerWindow(
            List<string> tasks,
            Dictionary<string, int>? currentWeights = null,
            Dictionary<string, string>? currentNotes = null,
            Dictionary<string, int>? currentTimeEstimates = null,
            Dictionary<string, string>? currentCategories = null)
        {
            InitializeComponent();

            var categories = CategoryManager.GetAllCategories();

            foreach (var task in tasks)
            {
                int weight = currentWeights?.ContainsKey(task) == true ? currentWeights[task] : 2;
                string notes = currentNotes?.ContainsKey(task) == true ? currentNotes[task] : string.Empty;
                int time = currentTimeEstimates?.ContainsKey(task) == true ? currentTimeEstimates[task] : 15;
                string category = currentCategories?.ContainsKey(task) == true ? currentCategories[task] : "General";

                taskList.Add(new EnhancedTask
                {
                    Name = task,
                    Weight = weight,
                    DifficultyDisplay = weight switch { 1 => "Easy (1)", 2 => "Medium (2)", 3 => "Hard (3)", _ => "Medium (2)" },
                    Notes = notes,
                    TimeEstimate = time,
                    Category = category,
                    Categories = categories
                });
            }

            dgTasks.ItemsSource = taskList;
        }

        private void btnManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var categoryWindow = new CategoryManagerWindow
            {
                Owner = this
            };

            if (categoryWindow.ShowDialog() == true)
            {
                // Refresh categories
                var categories = CategoryManager.GetAllCategories();
                foreach (var task in taskList)
                {
                    task.Categories = categories;
                }
            }
        }

        private void btnBulkEdit_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgTasks.SelectedItems.Cast<EnhancedTask>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select one or more tasks first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var bulkWindow = new BulkEditDialog(selected.Count)
            {
                Owner = this
            };

            if (bulkWindow.ShowDialog() == true)
            {
                foreach (var task in selected)
                {
                    if (bulkWindow.ApplyCategory && !string.IsNullOrEmpty(bulkWindow.SelectedCategory))
                        task.Category = bulkWindow.SelectedCategory;
                    
                    if (bulkWindow.ApplyDifficulty && !string.IsNullOrEmpty(bulkWindow.SelectedDifficulty))
                        task.DifficultyDisplay = bulkWindow.SelectedDifficulty;
                    
                    if (bulkWindow.ApplyTime && bulkWindow.TimeValue > 0)
                        task.TimeEstimate = bulkWindow.TimeValue;
                }

                dgTasks.Items.Refresh();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Weights.Clear();
            Notes.Clear();
            TimeEstimates.Clear();
            CategoryAssignments.Clear();

            foreach (var task in taskList)
            {
                Weights[task.Name] = task.Weight;
                Notes[task.Name] = task.Notes;
                TimeEstimates[task.Name] = task.TimeEstimate;
                CategoryAssignments[task.Name] = task.Category;
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