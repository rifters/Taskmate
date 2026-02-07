using System;
using System.Collections.Generic;
using System.Linq;

namespace Taskmate
{
    public class PersistentAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public string Tag { get; set; } = "General";
        public string GroupName { get; set; } = string.Empty;
        public List<AssignmentResult> Assignments { get; set; } = new List<AssignmentResult>();
        public string Notes { get; set; } = string.Empty;
        
        // New field for user notes/comments
        public string UserNotes { get; set; } = string.Empty;
        
        // Completion tracking
        public DateTime? CompletionUpdatedAt { get; set; }
        
        // UI field for batch selection (not persisted)
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSelected { get; set; } = false;

        // Completion status property
        [System.Text.Json.Serialization.JsonIgnore]
        public int TotalCompletedTasks
        {
            get => Assignments.Sum(a => a.CompletedCount);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public int TotalTasks
        {
            get => Assignments.Sum(a => a.TaskCount);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public double OverallCompletionPercentage
        {
            get => TotalTasks > 0 ? (TotalCompletedTasks / (double)TotalTasks) * 100 : 0;
        }
    }
}