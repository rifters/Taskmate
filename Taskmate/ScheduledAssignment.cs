using System;
using System.Collections.Generic;

namespace Taskmate
{
    public class ScheduledAssignment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string GroupFilePath { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        public int RecurrenceInterval { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastExecuted { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }
}