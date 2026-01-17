using System;
using System.IO;
using System.Windows;

namespace Taskmate
{
    public static class SampleManager
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner");
        
        private static readonly string FirstRunMarker = Path.Combine(AppDataFolder, ".firstrun");
        private static readonly string SamplesFolder = "Samples";

        public static bool IsFirstRun()
        {
            return !File.Exists(FirstRunMarker);
        }

        public static void MarkFirstRunComplete()
        {
            Directory.CreateDirectory(AppDataFolder);
            File.WriteAllText(FirstRunMarker, DateTime.Now.ToString());
        }

        public static void CopySamplesToDesktop()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string sampleDestination = Path.Combine(desktopPath, "TaskAssigner Samples");
                
                Directory.CreateDirectory(sampleDestination);

                // Copy sample files
                string[] sampleFiles = { "sample_tasks.txt", "sample_people.txt", "sample_group.tgroup" };
                
                foreach (var file in sampleFiles)
                {
                    string sourcePath = Path.Combine(SamplesFolder, file);
                    string destPath = Path.Combine(sampleDestination, file);
                    
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, destPath, true);
                    }
                }

                // Create a README
                string readmePath = Path.Combine(sampleDestination, "README.txt");
                File.WriteAllText(readmePath, @"TASK ASSIGNER - SAMPLE FILES
════════════════════════════════════════════════

Welcome to Task Assigner! 🎉

This folder contains sample files to help you get started:

📄 sample_tasks.txt     - Example task list
👥 sample_people.txt    - Example people list  
📦 sample_group.tgroup  - Example saved group

HOW TO USE:
1. Drag and drop the .txt files onto the Task Assigner window
   OR use the 'Load Tasks' and 'Load People' buttons

2. Click 'Assign Tasks' (or press F5) to see the magic!

3. Try the 'Load Group' button to load the sample_group.tgroup file

4. Explore all features using the Help menu (Ctrl+H)

CLEANUP:
You can delete this entire folder once you're familiar with the app.
The app will work with your own task and people lists!

Happy Assigning! ✨
");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy samples: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GetSamplePath(string fileName)
        {
            return Path.Combine(SamplesFolder, fileName);
        }

        public static bool SamplesExist()
        {
            return Directory.Exists(SamplesFolder) &&
                   File.Exists(Path.Combine(SamplesFolder, "sample_tasks.txt")) &&
                   File.Exists(Path.Combine(SamplesFolder, "sample_people.txt"));
        }
    }
}