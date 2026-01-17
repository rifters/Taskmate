namespace Taskmate
{
    public class TaskProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string RequiredRole { get; set; } = "General";
        public int Weight { get; set; } = 1; // 1=Easy, 2=Medium, 3=Hard
        public string Notes { get; set; } = string.Empty;
    }
}