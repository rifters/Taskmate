using System;
using System.Windows;
using Taskmate.Utilities;

namespace Taskmate.SmartAssignment
{
    /// <summary>
    /// Configuration window for Smart Assignment scoring weights
    /// </summary>
    public partial class SmartAssignmentConfigWindow : Window
    {
        private ScoringConfig _config;

        public SmartAssignmentConfigWindow()
        {
            InitializeComponent();
            _config = new ScoringConfig();
            LoadConfig();
        }

        private void LoadConfig()
        {
            slCapacity.Value = _config.CapacityWeight * 100;
            slRole.Value = _config.RoleWeight * 100;
            slSuccessRate.Value = _config.SuccessRateWeight * 100;
            slAvailability.Value = _config.AvailabilityWeight * 100;
            slBalance.Value = _config.BalanceWeight * 100;

            UpdateLabels();
        }

        private void Slider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            try
            {
                // Check if controls exist
                if (lblCapacity == null || slCapacity == null || 
                    lblRole == null || slRole == null ||
                    lblSuccessRate == null || slSuccessRate == null ||
                    lblAvailability == null || slAvailability == null ||
                    lblBalance == null || slBalance == null ||
                    lblTotal == null)
                    return;

                lblCapacity.Text = $"Capacity: {slCapacity.Value:F0}%";
                lblRole.Text = $"Role/Skills: {slRole.Value:F0}%";
                lblSuccessRate.Text = $"Success Rate: {slSuccessRate.Value:F0}%";
                lblAvailability.Text = $"Availability: {slAvailability.Value:F0}%";
                lblBalance.Text = $"Team Balance: {slBalance.Value:F0}%";

                double total = slCapacity.Value + slRole.Value + slSuccessRate.Value + slAvailability.Value + slBalance.Value;
                lblTotal.Text = $"Total: {total:F0}%";

                // Warn if total is not 100
                if (total < 99 || total > 101)
                {
                    lblTotal.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
                else
                {
                    lblTotal.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error updating labels", ex);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _config = new ScoringConfig();
            LoadConfig();
            MessageBox.Show("Reset to default configuration.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            double total = slCapacity.Value + slRole.Value + slSuccessRate.Value + slAvailability.Value + slBalance.Value;
            if (total < 99 || total > 101)
            {
                MessageBox.Show("Weights must sum to 100%. Please adjust.", "Invalid Configuration", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save configuration (could persist to settings)
            _config.CapacityWeight = slCapacity.Value / 100;
            _config.RoleWeight = slRole.Value / 100;
            _config.SuccessRateWeight = slSuccessRate.Value / 100;
            _config.AvailabilityWeight = slAvailability.Value / 100;
            _config.BalanceWeight = slBalance.Value / 100;

            Logger.LogInfo("Smart Assignment configuration updated");
            MessageBox.Show("Configuration saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
