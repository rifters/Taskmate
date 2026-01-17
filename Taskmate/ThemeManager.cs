using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public enum AppThemeMode
    {
        System,
        Light,
        Dark
    }

    public static class ThemeManager
    {
        private const string LightThemeUri = "Themes/LightTheme.xaml";
        private const string DarkThemeUri = "Themes/DarkTheme.xaml";

        public static AppThemeMode CurrentThemeMode { get; private set; } = AppThemeMode.System;

        public static void Initialize()
        {
            string savedTheme = Properties.Settings.Default.ThemeMode;
            if (Enum.TryParse(savedTheme, out AppThemeMode mode))
            {
                CurrentThemeMode = mode;
            }

            ApplyTheme();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public static void SetTheme(AppThemeMode mode)
        {
            CurrentThemeMode = mode;
            Properties.Settings.Default.ThemeMode = mode.ToString();
            Properties.Settings.Default.Save();
            ApplyTheme();
        }

        private static void ApplyTheme()
        {
            bool isDark = ShouldUseDarkMode();
            string themeUri = isDark ? DarkThemeUri : LightThemeUri;

            var app = Application.Current;
            var existingTheme = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);

            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }

            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            app.Resources.MergedDictionaries.Add(newTheme);
        }

        private static bool ShouldUseDarkMode()
        {
            return CurrentThemeMode switch
            {
                AppThemeMode.Light => false,
                AppThemeMode.Dark => true,
                AppThemeMode.System => IsSystemDarkMode(),
                _ => false
            };
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && CurrentThemeMode == AppThemeMode.System)
            {
                Application.Current.Dispatcher.Invoke(ApplyTheme);
            }
        }

        public static void Cleanup()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
    }
}   