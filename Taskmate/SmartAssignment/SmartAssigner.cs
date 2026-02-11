using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Taskmate.Utilities;

namespace Taskmate.SmartAssignment
{
    /// <summary>
    /// Intelligent assignment suggestion engine that recommends optimal people for tasks.
    /// 
    /// Features:
    /// - Evaluates multiple factors (capacity, skills, history, availability)
    /// - Provides ranked suggestions with explanations
    /// - Configurable scoring weights
    /// - Performance metrics tracking
    /// 
    /// Algorithm:
    /// 1. Filter eligible people (role/skill requirements)
    /// 2. Score capacity (current workload vs available)
    /// 3. Score success history (past completion rates)
    /// 4. Score availability (calendar conflicts)
    /// 5. Score team balance (fairness distribution)
    /// 6. Calculate weighted overall score
    /// 7. Rank and return top suggestions
    /// </summary>
    public class SmartAssigner
    {
        private readonly ScoringConfig _config;

        /// <summary>
        /// Initialize SmartAssigner with custom or default configuration
        /// </summary>
        public SmartAssigner(ScoringConfig? config = null)
        {
            _config = config ?? new ScoringConfig();
            
            if (!_config.IsValid())
            {
                Logger.LogWarning($"ScoringConfig weights sum to {_config.TotalWeight}. Should be 1.0. Normalizing...");
                NormalizeWeights();
            }
        }

        /// <summary>
        /// Get ranked suggestions for who should be assigned a task.
        /// Returns top N candidates ranked by overall score.
        /// </summary>
        /// <param name="personNames">List of people eligible for this task</param>
        /// <param name="currentAssignments">Current assignments to calculate workload</param>
        /// <param name="topN">How many top suggestions to return (default: 5)</param>
        /// <returns>List of AssignmentScore objects, ranked by score</returns>
        public async Task<List<AssignmentScore>> GetSuggestionsAsync(
            List<string> personNames,
            List<PersistentAssignment> currentAssignments,
            int topN = 5)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (personNames == null || personNames.Count == 0)
                {
                    Logger.LogWarning("SmartAssigner.GetSuggestionsAsync: No eligible people provided");
                    return new List<AssignmentScore>();
                }

                var suggestions = new List<AssignmentScore>();

                foreach (var personName in personNames)
                {
                    var score = await CalculateScoreAsync(personName, currentAssignments);
                    suggestions.Add(score);
                }

                // Sort by overall score (descending) and rank
                suggestions = suggestions
                    .OrderByDescending(s => s.OverallScore)
                    .Select((s, i) => { s.Rank = i + 1; return s; })
                    .Take(topN)
                    .ToList();

                stopwatch.Stop();
                Logger.LogPerformance(
                    "SmartAssigner.GetSuggestionsAsync",
                    stopwatch.ElapsedMilliseconds,
                    personNames.Count);

                return suggestions;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError("Error getting assignment suggestions", ex);
                throw;
            }
        }

        /// <summary>
        /// Calculate comprehensive score for a single person
        /// </summary>
        private async Task<AssignmentScore> CalculateScoreAsync(
            string personName,
            List<PersistentAssignment> currentAssignments)
        {
            var score = new AssignmentScore
            {
                PersonName = personName,
                Warnings = new List<string>(),
                Strengths = new List<string>()
            };

            // 1. Capacity Score (25% weight)
            var capacityScore = CalculateCapacityScore(personName, currentAssignments);
            score.CapacityScore = capacityScore;

            if (capacityScore < 30)
                score.Warnings.Add("High workload - may take longer to complete");
            else if (capacityScore > 80)
                score.Strengths.Add("Low workload - plenty of available capacity");

            // 2. Role Score (20% weight)
            var roleScore = CalculateRoleScore(personName);
            score.RoleScore = roleScore;

            if (roleScore == 100)
                score.Strengths.Add("Perfect role match");
            else if (roleScore < 50)
                score.Warnings.Add("Limited relevant skills - may need support");

            // 3. Success Rate Score (30% weight)
            var successScore = CalculateSuccessRateScore(personName, currentAssignments);
            score.SuccessRateScore = successScore;

            if (successScore > 90)
                score.Strengths.Add("Excellent track record");
            else if (successScore < 60)
                score.Warnings.Add("Below-average completion rate");

            // 4. Availability Score (15% weight)
            var availabilityScore = CalculateAvailabilityScore(personName);
            score.AvailabilityScore = availabilityScore;

            if (availabilityScore < 50)
                score.Warnings.Add("Partially unavailable during task period");

            // 5. Balance Score (10% weight)
            var balanceScore = CalculateBalanceScore(personName, currentAssignments);
            score.BalanceScore = balanceScore;

            if (balanceScore > 80)
                score.Strengths.Add("Maintains fair workload distribution");

            // Calculate overall weighted score
            score.OverallScore =
                (capacityScore * _config.CapacityWeight) +
                (roleScore * _config.RoleWeight) +
                (successScore * _config.SuccessRateWeight) +
                (availabilityScore * _config.AvailabilityWeight) +
                (balanceScore * _config.BalanceWeight);

            score.ReasonForScore = GenerateReason(score);

            return await Task.FromResult(score);
        }

        /// <summary>
        /// Calculate capacity score based on current workload
        /// </summary>
        private double CalculateCapacityScore(string personName, List<PersistentAssignment> assignments)
        {
            // Count current tasks for this person
            var personTasks = assignments
                .SelectMany(a => a.Assignments)
                .Where(a => a.Person.Equals(personName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Assume ~5 tasks per week is full capacity
            var currentLoad = personTasks.Count;
            var capacityThreshold = 5;

            if (currentLoad >= capacityThreshold * 2)
                return 0;  // Completely overwhelmed

            // Scale: 0 tasks = 100, 10 tasks = 0
            var utilization = currentLoad / (double)(capacityThreshold * 2);
            return Math.Max(0, (1.0 - utilization) * 100);
        }

        /// <summary>
        /// Calculate role/skill match score
        /// </summary>
        private double CalculateRoleScore(string personName)
        {
            // For now, assume all people can do all tasks (50% baseline)
            // In future, enhance with role/skill database
            return 50;
        }

        /// <summary>
        /// Calculate success rate based on historical completion
        /// </summary>
        private double CalculateSuccessRateScore(string personName, List<PersistentAssignment> assignments)
        {
            var personAssignments = assignments
                .Where(a => a.Assignments.Any(p =>
                    p.Person.Equals(personName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (personAssignments.Count == 0)
                return 50;  // Default for new people

            var completed = personAssignments
                .Count(a => a.OverallCompletionPercentage >= 100);

            var successRate = completed / (double)personAssignments.Count;
            return Math.Min(100, successRate * 100);
        }

        /// <summary>
        /// Calculate availability score based on time period
        /// </summary>
        private double CalculateAvailabilityScore(string personName)
        {
            // For now, assume everyone is available
            // In future, enhance with calendar integration
            return 100;
        }

        /// <summary>
        /// Calculate balance score to encourage fair distribution
        /// </summary>
        private double CalculateBalanceScore(string personName, List<PersistentAssignment> assignments)
        {
            var allPeople = assignments
                .SelectMany(a => a.Assignments)
                .Select(a => a.Person)
                .Distinct()
                .ToList();

            if (allPeople.Count == 0)
                return 100;

            var avgTaskCount = allPeople
                .Average(p => assignments
                    .SelectMany(a => a.Assignments)
                    .Where(a => a.Person.Equals(p, StringComparison.OrdinalIgnoreCase))
                    .Count());

            var personTaskCount = assignments
                .SelectMany(a => a.Assignments)
                .Where(a => a.Person.Equals(personName, StringComparison.OrdinalIgnoreCase))
                .Count();

            var variance = Math.Abs(personTaskCount - avgTaskCount);

            // Lower variance = higher score
            return Math.Max(0, 100 - (variance * 20));
        }

        /// <summary>
        /// Generate human-readable reason for the score
        /// </summary>
        private string GenerateReason(AssignmentScore score)
        {
            var reasons = new List<string>();

            if (score.SuccessRateScore > 85)
                reasons.Add("Highly reliable");
            if (score.CapacityScore > 70)
                reasons.Add("Available");
            if (score.RoleScore == 100)
                reasons.Add("Skilled");
            if (score.AvailabilityScore > 90)
                reasons.Add("No conflicts");

            return reasons.Count > 0
                ? string.Join(", ", reasons)
                : "Adequate fit";
        }

        /// <summary>
        /// Normalize weights to sum to 1.0
        /// </summary>
        private void NormalizeWeights()
        {
            var total = _config.TotalWeight;
            if (total == 0) return;

            _config.CapacityWeight /= total;
            _config.RoleWeight /= total;
            _config.SuccessRateWeight /= total;
            _config.AvailabilityWeight /= total;
            _config.BalanceWeight /= total;
        }
    }
}
