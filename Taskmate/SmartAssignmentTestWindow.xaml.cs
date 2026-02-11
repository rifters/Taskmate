using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Taskmate.SmartAssignment;
using Taskmate.Utilities;

namespace Taskmate
{
    /// <summary>
    /// Demo window for testing the Smart Assignment Engine
    /// </summary>
    public partial class SmartAssignmentTestWindow : Window
    {
        private SmartAssigner? _smartAssigner;

        public SmartAssignmentTestWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSmartAssigner();
            await LoadTestData();
        }

        private void InitializeSmartAssigner()
        {
            var config = new ScoringConfig
            {
                CapacityWeight = 0.25,
                RoleWeight = 0.20,
                SuccessRateWeight = 0.30,
                AvailabilityWeight = 0.15,
                BalanceWeight = 0.10
            };

            _smartAssigner = new SmartAssigner(config);
        }

        private async Task LoadTestData()
        {
            try
            {
                // Get all assignments from history
                var allAssignments = AssignmentHistoryManager.GetAllAssignments() ?? 
                    new List<PersistentAssignment>();

                if (allAssignments.Count == 0)
                {
                    MessageBox.Show("No assignment history found. Add some assignments first.");
                    return;
                }

                // Get all people
                var allPeople = new List<string>();
                foreach (var assignment in allAssignments)
                {
                    foreach (var result in assignment.Assignments)
                    {
                        if (!allPeople.Contains(result.Person))
                            allPeople.Add(result.Person);
                    }
                }

                if (allPeople.Count == 0)
                {
                    MessageBox.Show("No people found in assignment history.");
                    return;
                }

                // Get suggestions
                var suggestions = await _smartAssigner!.GetSuggestionsAsync(
                    personNames: allPeople,
                    currentAssignments: allAssignments,
                    topN: 5);

                // Display suggestions
                DisplaySuggestions(suggestions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplaySuggestions(List<AssignmentScore> suggestions)
        {
            var panel = new StackPanel { Margin = new Thickness(10) };

            foreach (var suggestion in suggestions)
            {
                var border = new Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = GetBackgroundColor(suggestion.OverallScore)
                };

                var content = new StackPanel();

                // Rank and name
                var rankText = new TextBlock
                {
                    Text = $"#{suggestion.Rank} - {suggestion.PersonName}",
                    FontWeight = System.Windows.FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                content.Children.Add(rankText);

                // Score visualization
                var scoreText = new TextBlock
                {
                    Text = suggestion.ScoreVisualization,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                content.Children.Add(scoreText);

                // Reason
                var reasonText = new TextBlock
                {
                    Text = suggestion.ReasonForScore,
                    Foreground = System.Windows.Media.Brushes.DarkGray,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                content.Children.Add(reasonText);

                // Scores breakdown
                var scoresText = new TextBlock
                {
                    Text = $"Capacity: {suggestion.CapacityScore:F0}% | " +
                           $"Role: {suggestion.RoleScore:F0}% | " +
                           $"Success: {suggestion.SuccessRateScore:F0}% | " +
                           $"Available: {suggestion.AvailabilityScore:F0}% | " +
                           $"Balance: {suggestion.BalanceScore:F0}%",
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                content.Children.Add(scoresText);

                // Warnings
                if (suggestion.Warnings.Count > 0)
                {
                    var warningsText = new TextBlock
                    {
                        Text = "?? " + string.Join(", ", suggestion.Warnings),
                        Foreground = System.Windows.Media.Brushes.Orange,
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    content.Children.Add(warningsText);
                }

                // Strengths
                if (suggestion.Strengths.Count > 0)
                {
                    var strengthsText = new TextBlock
                    {
                        Text = "? " + string.Join(", ", suggestion.Strengths),
                        Foreground = System.Windows.Media.Brushes.Green,
                        FontSize = 10,
                        TextWrapping = TextWrapping.Wrap
                    };
                    content.Children.Add(strengthsText);
                }

                border.Child = content;
                panel.Children.Add(border);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            this.Content = scrollViewer;
        }

        private System.Windows.Media.SolidColorBrush GetBackgroundColor(double score)
        {
            if (score >= 80)
                return new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(200, 255, 200)); // Light green
            if (score >= 60)
                return new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 255, 200)); // Light yellow
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 200, 200)); // Light red
        }
    }
}
