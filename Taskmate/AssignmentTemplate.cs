using System;
using System.Collections.Generic;

namespace Taskmate
{
    public class AssignmentTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public List<AssignmentResult> Assignments { get; set; } = new List<AssignmentResult>();
        public string GroupName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}