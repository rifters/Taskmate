namespace Taskmate
{
    public class PersonProfile
    {
        public string Name { get; set; } = string.Empty;
        public double Capacity { get; set; } = 1.0;
        public bool IsAvailable { get; set; } = true;
        public string Role { get; set; } = "General";
    }
}