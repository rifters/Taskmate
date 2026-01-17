using System;
using System.IO;
using System.Windows;

namespace Taskmate
{
    public static class DataManager
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner");

        public static bool ClearAllData()
        {
            var result = MessageBox.Show(
                "⚠️ WARNING: PERMANENT DATA DELETION\n\n" +
                "This will permanently delete ALL your data including:\n\n" +
                "• Assignment history\n" +
                "• Saved groups\n" +
                "• Templates\n" +
                "• Settings and preferences\n" +
                "• Rotation tracking data\n" +
                "• Notification settings\n\n" +
                "This action CANNOT be undone!\n\n" +
                "Are you absolutely sure you want to continue?",
                "Confirm Data Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
                return false;

            // Double confirmation
            var finalConfirm = MessageBox.Show(
                "FINAL CONFIRMATION\n\n" +
                "Type 'DELETE' in your mind and click Yes to proceed.\n\n" +
                "This is your last chance to cancel!",
                "Final Confirmation Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop,
                MessageBoxResult.No);

            if (finalConfirm != MessageBoxResult.Yes)
                return false;

            try
            {
                if (Directory.Exists(DataFolder))
                {
                    // Create final backup before deletion
                    string emergencyBackup = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"TaskAssigner_Emergency_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

                    try
                    {
                        BackupManager.CreateBackup(emergencyBackup);
                        MessageBox.Show(
                            $"Emergency backup created:\n{emergencyBackup}\n\n" +
                            "Keep this file in case you need to recover your data!",
                            "Emergency Backup Created",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch
                    {
                        var continueAnyway = MessageBox.Show(
                            "Failed to create emergency backup!\n\n" +
                            "Continue with deletion anyway?",
                            "Backup Failed",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (continueAnyway != MessageBoxResult.Yes)
                            return false;
                    }

                    // Delete all data
                    Directory.Delete(DataFolder, true);

                    // Log the deletion
                    AuditLogger.Log("GDPR_DATA_DELETION", Environment.UserName, 
                        "All user data deleted via Clear All Data feature");

                    MessageBox.Show(
                        "✓ All data has been permanently deleted.\n\n" +
                        "The application will now close.\n\n" +
                        "When you restart, it will be like the first time you used it.",
                        "Data Deleted",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "No data found to delete.",
                        "No Data",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete data:\n\n{ex.Message}\n\n" +
                    "You may need to manually delete the folder:\n" +
                    DataFolder,
                    "Deletion Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        public static string GetDataFolderPath() => DataFolder;
    }
}