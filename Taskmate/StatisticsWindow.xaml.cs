using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Taskmate
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow(List<AssignmentResult> assignments)
        {
            InitializeComponent();
            CalculateStatistics(assignments);
        }

        private void CalculateStatistics(List<AssignmentResult> assignments)
        {
            if (assignments == null || assignments.Count == 0)
            {
                txtTotalTasks.Text = "0";
                txtTotalPeople.Text = "0";
                txtAvgTasks.Text = "0.0";
                txtFairness.Text = "N/A";
                return;
            }

            int totalTasks = assignments.Sum(a => a.TaskCount);
            int totalPeople = assignments.Count;
            double avgTasks = (double)totalTasks / totalPeople;
            
            // Calculate fairness score (0-100, where 100 is perfectly fair)
            double variance = assignments.Average(a => Math.Pow(a.TaskCount - avgTasks, 2));
            double stdDev = Math.Sqrt(variance);
            double fairnessScore = Math.Max(0, 100 - (stdDev * 20)); // Scale it

            txtTotalTasks.Text = totalTasks.ToString();
            txtTotalPeople.Text = totalPeople.ToString();
            txtAvgTasks.Text = avgTasks.ToString("F1");
            txtFairness.Text = $"{fairnessScore:F0}% " + GetFairnessEmoji(fairnessScore);

            // Create distribution bars
            int maxTasks = assignments.Max(a => a.TaskCount);
            foreach (var assignment in assignments.OrderByDescending(a => a.TaskCount))
            {
                CreateDistributionBar(assignment.Person, assignment.TaskCount, maxTasks);
            }
        }

        private string GetFairnessEmoji(double score)
        {
            if (score >= 90) return "🌟";
            if (score >= 70) return "👍";
            if (score >= 50) return "😐";
            return "⚠️";
        }

        private void CreateDistributionBar(string person, int taskCount, int maxTasks)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };

            var nameLabel = new TextBlock
            {
                Text = person,
                Width = 150,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };

            var barWidth = maxTasks > 0 ? (taskCount / (double)maxTasks) * 200 : 0;
            var bar = new Border
            {
                Width = barWidth,
                Height = 25,
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(10, 0, 10, 0)
            };

            var countLabel = new TextBlock
            {
                Text = taskCount.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };

            panel.Children.Add(nameLabel);
            panel.Children.Add(bar);
            panel.Children.Add(countLabel);

            pnlDistribution.Children.Add(panel);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}