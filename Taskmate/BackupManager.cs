using System;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace Taskmate
{
    public static class BackupManager
    {
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner");

        public static bool CreateBackup(string backupPath)
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                {
                    MessageBox.Show("No data to backup.", "Backup", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // Ensure backup path ends with .zip
                if (!backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    backupPath += ".zip";

                // Delete existing backup if it exists
                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                // Create backup
                ZipFile.CreateFromDirectory(DataFolder, backupPath, CompressionLevel.Optimal, false);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool RestoreBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    MessageBox.Show("Backup file not found.", "Restore Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                var result = MessageBox.Show(
                    "⚠️ WARNING: This will overwrite ALL current data!\n\n" +
                    "Your existing assignments, history, templates, and settings will be replaced.\n\n" +
                    "Are you sure you want to continue?",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return false;

                // Create temporary backup of current data
                string tempBackup = Path.Combine(Path.GetTempPath(), $"TaskAssigner_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                if (Directory.Exists(DataFolder))
                {
                    try
                    {
                        ZipFile.CreateFromDirectory(DataFolder, tempBackup);
                    }
                    catch { }
                }

                // Delete current data folder
                if (Directory.Exists(DataFolder))
                    Directory.Delete(DataFolder, true);

                // Extract backup
                ZipFile.ExtractToDirectory(backupPath, DataFolder);

                MessageBox.Show(
                    "Backup restored successfully!\n\n" +
                    "Please restart the application for changes to take full effect.\n\n" +
                    $"A backup of your old data was saved to:\n{tempBackup}",
                    "Restore Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static long GetDataSize()
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                    return 0;

                var dirInfo = new DirectoryInfo(DataFolder);
                return GetDirectorySize(dirInfo);
            }
            catch
            {
                return 0;
            }
        }

        private static long GetDirectorySize(DirectoryInfo directory)
        {
            long size = 0;

            // Add file sizes
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                size += file.Length;
            }

            // Add subdirectory sizes
            DirectoryInfo[] dirs = directory.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                size += GetDirectorySize(dir);
            }

            return size;
        }

        public static string GetDataSizeFormatted()
        {
            long bytes = GetDataSize();
            
            if (bytes < 1024)
                return $"{bytes} bytes";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        public static string GetDefaultBackupDirectory()
        {
            string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string backupDir = Path.Combine(myDocs, "TaskAssigner Backups");
            Directory.CreateDirectory(backupDir);
            return backupDir;
        }
    }
}