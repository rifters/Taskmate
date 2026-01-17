using System.Windows;

namespace Taskmate
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentTheme();
        }

        private void LoadCurrentTheme()
        {
            AppThemeMode currentMode = ThemeManager.CurrentThemeMode;
            
            if (currentMode == AppThemeMode.System)
                rbSystem.IsChecked = true;
            else if (currentMode == AppThemeMode.Light)
                rbLight.IsChecked = true;
            else if (currentMode == AppThemeMode.Dark)
                rbDark.IsChecked = true;
        }

        private void ThemeChanged(object sender, RoutedEventArgs e)
        {
            AppThemeMode newMode = AppThemeMode.System;

            if (rbLight.IsChecked == true)
                newMode = AppThemeMode.Light;
            else if (rbDark.IsChecked == true)
                newMode = AppThemeMode.Dark;

            ThemeManager.SetTheme(newMode);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}