using System.Collections.Generic;

namespace Taskmate
{
    public class AssignmentResult
    {
        public string Person { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public string Tasks { get; set; } = string.Empty;
        public double Capacity { get; set; } = 1.0;
        public string WorkloadPercentage { get; set; } = "100%";

        // Completion tracking
        public List<string> CompletedTasks { get; set; } = new List<string>();
        public bool IsPersonComplete { get; set; } = false;

        // Tagging
        public string CurrentTag { get; set; } = "Untagged";

        // NEW: Color coding properties
        public bool IsOverloaded => GetWorkloadValue() > 120;
        public bool IsUnderloaded => GetWorkloadValue() < 80;
        public bool IsSlightlyHigh => GetWorkloadValue() >= 100 && GetWorkloadValue() <= 120;

        // Completion status properties
        public int CompletedCount => CompletedTasks.Count;
        public double CompletionPercentage => TaskCount > 0 ? (CompletedCount / (double)TaskCount) * 100 : 100;
        public string CompletionStatus
        {
            get
            {
                if (TaskCount == 0) return "Complete";
                if (CompletionPercentage >= 100) return "Complete";
                if (CompletionPercentage > 0) return "Partial";
                return "Incomplete";
            }
        }

        private double GetWorkloadValue()
        {
            if (string.IsNullOrEmpty(WorkloadPercentage))
                return 100;

            string numericPart = WorkloadPercentage.Replace("%", "").Trim();
            if (double.TryParse(numericPart, out double value))
                return value;

            return 100;
        }

        /// <summary>
        /// Recalculate task count and other metrics based on current Tasks string
        /// </summary>
        public void RecalculateMetrics()
        {
            if (string.IsNullOrWhiteSpace(Tasks))
            {
                TaskCount = 0;
                WorkloadPercentage = "0%";
            }
            else
            {
                var taskList = Tasks.Split(',');
                TaskCount = taskList.Length;
                
                // Workload percentage is based on task count (simple model: 20% per task)
                double workload = TaskCount * 20.0;
                WorkloadPercentage = $"{workload:F0}%";
            }
        }
    }
}
