using System;
using System.Collections.Generic;

namespace Taskmate
{
    public class AssignmentHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public List<AssignmentResult> Assignments { get; set; } = new List<AssignmentResult>();
        public string Description { get; set; } = string.Empty;
    }
}