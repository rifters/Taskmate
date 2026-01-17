namespace Taskmate
{
    public class AssignmentResult
    {
        public string Person { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public string Tasks { get; set; } = string.Empty;
        public double Capacity { get; set; } = 1.0;
        public string WorkloadPercentage { get; set; } = "100%";

        // NEW: Color coding properties
        public bool IsOverloaded => GetWorkloadValue() > 120;
        public bool IsUnderloaded => GetWorkloadValue() < 80;
        public bool IsSlightlyHigh => GetWorkloadValue() >= 100 && GetWorkloadValue() <= 120;

        private double GetWorkloadValue()
        {
            if (string.IsNullOrEmpty(WorkloadPercentage))
                return 100;

            string numericPart = WorkloadPercentage.Replace("%", "").Trim();
            if (double.TryParse(numericPart, out double value))
                return value;

            return 100;
        }
    }
}