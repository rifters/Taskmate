using System.Windows;
using Microsoft.Win32;

namespace Taskmate
{
    public partial class AdvancedFeaturesWindow : Window
    {
        public AdvancedFeaturesWindow()
        {
            InitializeComponent();
            LoadFeatures();
        }

        private void LoadFeatures()
        {
            var features = FeatureManager.GetFeatures();
            
            // Existing features
            chkTaskWeighting.IsChecked = features.UseTaskWeighting;
            chkAvailability.IsChecked = features.UsePersonAvailability;
            chkRoles.IsChecked = features.UseRoles;
            chkPrintPreview.IsChecked = features.UsePrintPreview;
            chkQuickSwap.IsChecked = features.UseQuickSwap;
            chkConstraints.IsChecked = features.UseConstraints;
            chkHistory.IsChecked = features.UseHistory;
            
            // New features
            chkTaskTimeEstimates.IsChecked = features.UseTaskTimeEstimates;
            chkAutoRotation.IsChecked = features.UseAutoRotation;
            chkTaskCategories.IsChecked = features.UseTaskCategories;
            chkBulkEditMode.IsChecked = features.UseBulkEditMode;
            chkAssignmentTemplates.IsChecked = features.UseAssignmentTemplates;
            chkAssignmentScheduler.IsChecked = features.UseAssignmentScheduler;
            chkPerformanceAnalytics.IsChecked = features.UsePerformanceAnalytics;
            chkAssignmentNotes.IsChecked = features.UseAssignmentNotes;
            chkNotifications.IsChecked = features.UseNotifications;
            chkMobileExport.IsChecked = features.UseMobileExport;
            chkCompletionTracking.IsChecked = features.UseCompletionTracking;
            
            // User-configurable options
            if (chkTagging != null)
                chkTagging.IsChecked = features.UseTagging;
            if (chkTaggingAtAssignment != null)
                chkTaggingAtAssignment.IsChecked = features.UseTaggingAtAssignment;
            if (txtAssignmentLocation != null)
                txtAssignmentLocation.Text = features.AssignmentSaveLocation ?? "";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var features = new FeatureFlags
            {
                // Existing
                UseTaskWeighting = chkTaskWeighting.IsChecked == true,
                UsePersonAvailability = chkAvailability.IsChecked == true,
                UseRoles = chkRoles.IsChecked == true,
                UsePrintPreview = chkPrintPreview.IsChecked == true,
                UseQuickSwap = chkQuickSwap.IsChecked == true,
                UseConstraints = chkConstraints.IsChecked == true,
                UseHistory = chkHistory.IsChecked == true,
                
                // New
                UseTaskTimeEstimates = chkTaskTimeEstimates.IsChecked == true,
                UseAutoRotation = chkAutoRotation.IsChecked == true,
                UseTaskCategories = chkTaskCategories.IsChecked == true,
                UseBulkEditMode = chkBulkEditMode.IsChecked == true,
                UseAssignmentTemplates = chkAssignmentTemplates.IsChecked == true,
                UseAssignmentScheduler = chkAssignmentScheduler.IsChecked == true,
                UsePerformanceAnalytics = chkPerformanceAnalytics.IsChecked == true,
                UseAssignmentNotes = chkAssignmentNotes.IsChecked == true,
                UseNotifications = chkNotifications.IsChecked == true,
                UseMobileExport = chkMobileExport.IsChecked == true,
                UseCompletionTracking = chkCompletionTracking.IsChecked == true,
                
                // User-configurable options
                UseTagging = chkTagging?.IsChecked == true,
                UseTaggingAtAssignment = chkTaggingAtAssignment?.IsChecked == true,
                AssignmentSaveLocation = txtAssignmentLocation?.Text ?? ""
            };

            FeatureManager.SaveFeatures(features);
            
            MessageBox.Show("Settings saved! Some features may require restarting the application for full effect.", 
                "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            chkTaskWeighting.IsChecked = true;
            chkAvailability.IsChecked = true;
            chkRoles.IsChecked = true;
            chkPrintPreview.IsChecked = true;
            chkQuickSwap.IsChecked = true;
            chkConstraints.IsChecked = true;
            chkHistory.IsChecked = true;
            chkTaskTimeEstimates.IsChecked = true;
            chkAutoRotation.IsChecked = true;
            chkTaskCategories.IsChecked = true;
            chkBulkEditMode.IsChecked = true;
            chkAssignmentTemplates.IsChecked = true;
            chkAssignmentScheduler.IsChecked = true;
            chkPerformanceAnalytics.IsChecked = true;
            chkAssignmentNotes.IsChecked = true;
            chkNotifications.IsChecked = true;
            chkMobileExport.IsChecked = true;
            chkCompletionTracking.IsChecked = true;
            chkTagging.IsChecked = true;
            chkTaggingAtAssignment.IsChecked = true;
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            chkTaskWeighting.IsChecked = false;
            chkAvailability.IsChecked = false;
            chkRoles.IsChecked = false;
            chkPrintPreview.IsChecked = false;
            chkQuickSwap.IsChecked = false;
            chkConstraints.IsChecked = false;
            chkHistory.IsChecked = false;
            chkTaskTimeEstimates.IsChecked = false;
            chkAutoRotation.IsChecked = false;
            chkTaskCategories.IsChecked = false;
            chkBulkEditMode.IsChecked = false;
            chkAssignmentTemplates.IsChecked = false;
            chkAssignmentScheduler.IsChecked = false;
            chkPerformanceAnalytics.IsChecked = false;
            chkAssignmentNotes.IsChecked = false;
            chkNotifications.IsChecked = false;
            chkMobileExport.IsChecked = false;
            chkCompletionTracking.IsChecked = false;
            chkTagging.IsChecked = false;
            chkTaggingAtAssignment.IsChecked = false;
            chkCompletionTracking.IsChecked = false;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will reset all feature settings to defaults. Continue?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                FeatureManager.ResetToDefaults();
                LoadFeatures();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnBrowseLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Assignment Save Location"
            };

            if (!string.IsNullOrEmpty(txtAssignmentLocation.Text) && System.IO.Directory.Exists(txtAssignmentLocation.Text))
                dialog.InitialDirectory = txtAssignmentLocation.Text;

            if (dialog.ShowDialog() == true)
            {
                txtAssignmentLocation.Text = dialog.FolderName;
            }
        }
    }
}