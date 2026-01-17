using System.Windows;

namespace Taskmate
{
    public static class FeatureComingSoon
    {
        public static void Show(string featureName)
        {
            MessageBox.Show(
                $"🚧 {featureName} is coming soon!\n\n" +
                $"This feature is enabled but not yet fully implemented. " +
                $"Check back in future updates for the complete functionality.\n\n" +
                $"In the meantime, you can use the existing features to manage your tasks.",
                "Feature Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        public static void ShowWithFallback(string featureName, string fallbackSuggestion)
        {
            MessageBox.Show(
                $"🚧 {featureName} is coming soon!\n\n" +
                $"This feature is enabled but not yet fully implemented.\n\n" +
                $"💡 Temporary workaround:\n{fallbackSuggestion}",
                "Feature Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}