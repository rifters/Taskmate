using System;
using System.Collections.Generic;

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
    }
}