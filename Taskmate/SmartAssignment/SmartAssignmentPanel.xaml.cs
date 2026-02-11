using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Taskmate.Utilities;

namespace Taskmate.SmartAssignment
{
    /// <summary>
    /// Reusable Smart Assignment suggestions panel that can be added to any window.
    /// Displays ranked recommendations with scores, warnings, and strengths.
    /// </summary>
    public partial class SmartAssignmentPanel : UserControl
    {
        private SmartAssigner? _smartAssigner;
        private List<AssignmentScore> _currentSuggestions = new();

        /// <summary>
        /// Fired when user selects a person from suggestions
        /// </summary>
        public event EventHandler<PersonSelectedEventArgs>? PersonSelected;

        public SmartAssignmentPanel()
        {
            InitializeComponent();
            InitializeSmartAssigner();
        }

        private void InitializeSmartAssigner()
        {
            var config = new ScoringConfig();
            _smartAssigner = new SmartAssigner(config);
        }

        /// <summary>
        /// Load and display suggestions based on available people
        /// </summary>
        public async Task LoadSuggestionsAsync(List<string> eligiblePeople, List<PersistentAssignment> currentAssignments)
        {
            try
            {
                ShowLoading(true);
                HideError();

                if (eligiblePeople == null || eligiblePeople.Count == 0)
                {
                    ShowError("No eligible people available.");
                    return;
                }

                // Get suggestions
                var suggestions = await _smartAssigner!.GetSuggestionsAsync(
                    personNames: eligiblePeople,
                    currentAssignments: currentAssignments,
                    topN: 5);

                _currentSuggestions = suggestions;

                // Add background colors based on score
                foreach (var suggestion in _currentSuggestions)
                {
                    suggestion.BackgroundColor = GetBackgroundBrush(suggestion.OverallScore);
                }

                // Display suggestions
                suggestionsControl.ItemsSource = _currentSuggestions;

                ShowLoading(false);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading Smart Assignment suggestions", ex);
                ShowError($"Failed to load suggestions: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh suggestions with current data
        /// </summary>
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var allAssignments = AssignmentHistoryManager.GetAllAssignments() ?? new List<PersistentAssignment>();
                var allPeople = GetAllPeopleFromAssignments(allAssignments);

                if (allPeople.Count > 0)
                {
                    await LoadSuggestionsAsync(allPeople, allAssignments);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error refreshing suggestions", ex);
                ShowError("Failed to refresh suggestions");
            }
        }

        /// <summary>
        /// Handle when user clicks "Assign" button on a person
        /// </summary>
        private void BtnAssignPerson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string personName)
            {
                // Find the suggestion for this person
                var suggestion = _currentSuggestions.FirstOrDefault(s => s.PersonName == personName);
                
                if (suggestion != null)
                {
                    // Fire event with the selected person
                    PersonSelected?.Invoke(this, new PersonSelectedEventArgs
                    {
                        PersonName = suggestion.PersonName,
                        Score = suggestion.OverallScore,
                        Reason = suggestion.ReasonForScore
                    });
                }
            }
        }

        /// <summary>
        /// Open configuration dialog for scoring weights
        /// </summary>
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new SmartAssignmentConfigWindow
            {
                Owner = Window.GetWindow(this)
            };
            configWindow.ShowDialog();

            // Reload suggestions with new config
            btnRefresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ShowLoading(bool show)
        {
            txtLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            suggestionsControl.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            errorBorder.Visibility = Visibility.Visible;
            suggestionsControl.Visibility = Visibility.Collapsed;
        }

        private void HideError()
        {
            errorBorder.Visibility = Visibility.Collapsed;
        }

        private SolidColorBrush GetBackgroundBrush(double score)
        {
            if (score >= 80)
                return new SolidColorBrush(Color.FromRgb(200, 255, 200)); // Light green
            if (score >= 60)
                return new SolidColorBrush(Color.FromRgb(255, 255, 200)); // Light yellow
            return new SolidColorBrush(Color.FromRgb(255, 200, 200)); // Light red
        }

        private List<string> GetAllPeopleFromAssignments(List<PersistentAssignment> assignments)
        {
            var people = new List<string>();
            foreach (var assignment in assignments)
            {
                foreach (var result in assignment.Assignments)
                {
                    if (!people.Contains(result.Person))
                        people.Add(result.Person);
                }
            }
            return people.OrderBy(p => p).ToList();
        }
    }

    /// <summary>
    /// Event args for when a person is selected from suggestions
    /// </summary>
    public class PersonSelectedEventArgs : EventArgs
    {
        public string PersonName { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
