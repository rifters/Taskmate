using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class AvailabilityManagerWindow : Window
    {
        public class PersonAvailability : INotifyPropertyChanged
        {
            private bool isAvailable = true;
            private string role = "General";

            public string Name { get; set; } = string.Empty;
            public bool IsAvailable 
            { 
                get => isAvailable;
                set
                {
                    isAvailable = value;
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
            public string Role
            {
                get => role;
                set
                {
                    role = value ?? "General";
                    OnPropertyChanged(nameof(Role));
                }
            }
            public List<string> AllRoles { get; set; } = new List<string>();

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Dictionary<string, bool> Availability { get; private set; } = new Dictionary<string, bool>();
        public Dictionary<string, string> Roles { get; private set; } = new Dictionary<string, string>();

        private ObservableCollection<PersonAvailability> peopleList = new ObservableCollection<PersonAvailability>();

        public AvailabilityManagerWindow(List<string> people, Dictionary<string, bool>? currentAvailability = null, Dictionary<string, string>? currentRoles = null)
        {
            InitializeComponent();

            var allRoles = RoleManager.GetAllRoles();

            foreach (var person in people)
            {
                peopleList.Add(new PersonAvailability
                {
                    Name = person,
                    IsAvailable = currentAvailability?.ContainsKey(person) == true ? currentAvailability[person] : true,
                    Role = currentRoles?.ContainsKey(person) == true ? currentRoles[person] : "General",
                    AllRoles = allRoles
                });
            }

            dgPeople.ItemsSource = peopleList;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Availability.Clear();
            Roles.Clear();

            foreach (var person in peopleList)
            {
                Availability[person.Name] = person.IsAvailable;
                Roles[person.Name] = person.Role;
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