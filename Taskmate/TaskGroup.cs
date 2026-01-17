using System.Collections.Generic;

namespace Taskmate
{
    public class TaskGroup
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new List<string>();
        public List<string> People { get; set; } = new List<string>();
        public Dictionary<string, double> Capacities { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, string> TaskCategories { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> PeopleAvailability { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, string> PeopleRoles { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, int> TaskWeights { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> TaskNotes { get; set; } = new Dictionary<string, string>();
        
        // New fields
        public Dictionary<string, int> TaskTimeEstimates { get; set; } = new Dictionary<string, int>(); // in minutes
        public Dictionary<string, string> TaskCategoryAssignments { get; set; } = new Dictionary<string, string>();
    }
}