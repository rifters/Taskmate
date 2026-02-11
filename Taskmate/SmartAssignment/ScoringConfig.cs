namespace Taskmate.SmartAssignment
{
    /// <summary>
    /// Configuration for the smart assignment scoring algorithm.
    /// Allows customization of weights and thresholds.
    /// </summary>
    public class ScoringConfig
    {
        /// <summary>
        /// Weight for capacity score in overall calculation (0-1)
        /// Default: 0.25 (25%)
        /// </summary>
        public double CapacityWeight { get; set; } = 0.25;

        /// <summary>
        /// Weight for role/skill score in overall calculation (0-1)
        /// Default: 0.20 (20%)
        /// </summary>
        public double RoleWeight { get; set; } = 0.20;

        /// <summary>
        /// Weight for success rate score in overall calculation (0-1)
        /// Default: 0.30 (30%)
        /// </summary>
        public double SuccessRateWeight { get; set; } = 0.30;

        /// <summary>
        /// Weight for availability score in overall calculation (0-1)
        /// Default: 0.15 (15%)
        /// </summary>
        public double AvailabilityWeight { get; set; } = 0.15;

        /// <summary>
        /// Weight for balance score in overall calculation (0-1)
        /// Default: 0.10 (10%)
        /// </summary>
        public double BalanceWeight { get; set; } = 0.10;

        /// <summary>
        /// Threshold above which workload is considered high (0-1)
        /// Default: 0.80 (80% utilization)
        /// </summary>
        public double HighWorkloadThreshold { get; set; } = 0.80;

        /// <summary>
        /// Threshold below which workload is considered low (0-1)
        /// Default: 0.20 (20% utilization)
        /// </summary>
        public double LowCapacityThreshold { get; set; } = 0.20;

        /// <summary>
        /// Minimum acceptable success rate (0-1)
        /// Default: 0.50 (50%)
        /// </summary>
        public double MinimumSuccessRate { get; set; } = 0.50;

        /// <summary>
        /// Validates that all weights sum to approximately 1.0
        /// </summary>
        public bool IsValid()
        {
            var totalWeight = CapacityWeight + RoleWeight + SuccessRateWeight + 
                             AvailabilityWeight + BalanceWeight;
            
            // Allow small floating-point variance
            return totalWeight >= 0.99 && totalWeight <= 1.01;
        }

        /// <summary>
        /// Gets the sum of all weights
        /// </summary>
        public double TotalWeight =>
            CapacityWeight + RoleWeight + SuccessRateWeight + 
            AvailabilityWeight + BalanceWeight;
    }
}
