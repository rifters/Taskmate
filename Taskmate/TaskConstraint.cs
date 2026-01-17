using System.Collections.Generic;

namespace Taskmate
{
    public class TaskConstraint
    {
        public Dictionary<string, List<string>> Exclusions { get; set; } = new Dictionary<string, List<string>>();
        
        // Check if a person can be assigned a task
        public bool CanAssign(string person, string task)
        {
            if (Exclusions.ContainsKey(person))
            {
                return !Exclusions[person].Contains(task);
            }
            return true;
        }
        
        // Add an exclusion
        public void AddExclusion(string person, string task)
        {
            if (!Exclusions.ContainsKey(person))
            {
                Exclusions[person] = new List<string>();
            }
            
            if (!Exclusions[person].Contains(task))
            {
                Exclusions[person].Add(task);
            }
        }
        
        // Remove an exclusion
        public void RemoveExclusion(string person, string task)
        {
            if (Exclusions.ContainsKey(person))
            {
                Exclusions[person].Remove(task);
                if (Exclusions[person].Count == 0)
                {
                    Exclusions.Remove(person);
                }
            }
        }
    }
}