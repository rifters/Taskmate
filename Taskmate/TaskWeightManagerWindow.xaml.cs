using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class TaskWeightManagerWindow : Window
    {
        public class TaskWeight : INotifyPropertyChanged
        {
            private string weightDisplay = "Easy (1)";
            private string notes = string.Empty;

            public string Name { get; set; } = string.Empty;
            public int Weight { get; set; } = 1;
            public string WeightDisplay
            {
                get => weightDisplay;
                set
                {
                    weightDisplay = value ?? "Easy (1)";
                    Weight = weightDisplay switch
                    {
                        "Easy (1)" => 1,
                        "Medium (2)" => 2,
                        "Hard (3)" => 3,
                        _ => 1
                    };
                    OnPropertyChanged(nameof(WeightDisplay));
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
            public List<string> WeightOptions { get; set; } = new List<string> { "Easy (1)", "Medium (2)", "Hard (3)" };

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Dictionary<string, int> Weights { get; private set; } = new Dictionary<string, int>();
        public Dictionary<string, string> Notes { get; private set; } = new Dictionary<string, string>();

        private ObservableCollection<TaskWeight> taskList = new ObservableCollection<TaskWeight>();

        public TaskWeightManagerWindow(List<string> tasks, Dictionary<string, int>? currentWeights = null, Dictionary<string, string>? currentNotes = null)
        {
            InitializeComponent();

            foreach (var task in tasks)
            {
                int weight = currentWeights?.ContainsKey(task) == true ? currentWeights[task] : 1;
                string notes = currentNotes?.ContainsKey(task) == true ? currentNotes[task] : string.Empty;

                taskList.Add(new TaskWeight
                {
                    Name = task,
                    Weight = weight,
                    WeightDisplay = weight switch
                    {
                        1 => "Easy (1)",
                        2 => "Medium (2)",
                        3 => "Hard (3)",
                        _ => "Easy (1)"
                    },
                    Notes = notes
                });
            }

            dgTasks.ItemsSource = taskList;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Weights.Clear();
            Notes.Clear();

            foreach (var task in taskList)
            {
                Weights[task.Name] = task.Weight;
                Notes[task.Name] = task.Notes;
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