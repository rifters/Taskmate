using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Taskmate.SmartAssignment
{
    /// <summary>
    /// Scoring metrics for a person-task assignment recommendation.
    /// Provides detailed breakdown of why a person is or isn't recommended for a task.
    /// </summary>
    public class AssignmentScore
    {
        /// <summary>
        /// The person being evaluated
        /// </summary>
        public string PersonName { get; set; } = string.Empty;

        /// <summary>
        /// Overall score (0-100). Higher is better.
        /// This is a weighted combination of all sub-scores.
        /// </summary>
        public double OverallScore { get; set; }

        /// <summary>
        /// Capacity score (0-100). Based on current workload.
        /// Higher = more available capacity
        /// </summary>
        public double CapacityScore { get; set; }

        /// <summary>
        /// Role/Skill score (0-100). Based on task requirements vs person capabilities.
        /// 100 = perfect match, 0 = no relevant skills
        /// </summary>
        public double RoleScore { get; set; }

        /// <summary>
        /// Success rate score (0-100). Based on historical completion rates.
        /// Higher = more reliable completion
        /// </summary>
        public double SuccessRateScore { get; set; }

        /// <summary>
        /// Availability score (0-100). Based on calendar/time constraints.
        /// Higher = more available during task period
        /// </summary>
        public double AvailabilityScore { get; set; }

        /// <summary>
        /// Balance score (0-100). Based on team fairness.
        /// Higher = more aligned with team average workload
        /// </summary>
        public double BalanceScore { get; set; }

        /// <summary>
        /// Human-readable summary of why this person got this score
        /// </summary>
        public string ReasonForScore { get; set; } = string.Empty;

        /// <summary>
        /// List of warnings (why this person might not be ideal)
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// List of strengths (why this person is a good choice)
        /// </summary>
        public List<string> Strengths { get; set; } = new();

        /// <summary>
        /// Ranking position (1st, 2nd, 3rd, etc.)
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Background color for UI display based on score
        /// </summary>
        public SolidColorBrush BackgroundColor { get; set; } = new SolidColorBrush(Colors.White);

        /// <summary>
        /// Gets a visual representation of the overall score
        /// </summary>
        public string ScoreVisualization
        {
            get
            {
                var filled = (int)(OverallScore / 10);
                var empty = 10 - filled;
                return new string('?', filled) + new string('?', empty) + $" {OverallScore:F0}%";
            }
        }
    }
}
