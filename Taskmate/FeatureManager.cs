using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Taskmate
{
    public class FeatureFlags
    {
        // Existing features
        public bool UseTaskWeighting { get; set; } = false;
        public bool UsePersonAvailability { get; set; } = false;
        public bool UseRoles { get; set; } = false;
        public bool UsePrintPreview { get; set; } = true;
        public bool UseQuickSwap { get; set; } = true;
        public bool UseConstraints { get; set; } = true;
        public bool UseHistory { get; set; } = true;
        
        // New features
        public bool UseTaskTimeEstimates { get; set; } = false;
        public bool UseAutoRotation { get; set; } = false;
        public bool UseTaskCategories { get; set; } = false;
        public bool UseBulkEditMode { get; set; } = false;
        public bool UseAssignmentTemplates { get; set; } = false;
        public bool UseAssignmentScheduler { get; set; } = false;
        public bool UsePerformanceAnalytics { get; set; } = false;
        public bool UseAssignmentNotes { get; set; } = false;
        public bool UseNotifications { get; set; } = false;
        public bool UseMobileExport { get; set; } = false;
    }

    public static class FeatureManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "features.json");

        private static FeatureFlags? cachedFlags;

        public static FeatureFlags GetFeatures()
        {
            if (cachedFlags == null)
                LoadFeatures();
            
            return cachedFlags ?? new FeatureFlags();
        }

        public static void SaveFeatures(FeatureFlags flags)
        {
            try
            {
                cachedFlags = flags;
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string json = JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Fail silently
            }
        }

        private static void LoadFeatures()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    cachedFlags = JsonSerializer.Deserialize<FeatureFlags>(json) ?? new FeatureFlags();
                }
                else
                {
                    cachedFlags = new FeatureFlags();
                    SaveFeatures(cachedFlags);
                }
            }
            catch
            {
                cachedFlags = new FeatureFlags();
            }
        }

        public static void ResetToDefaults()
        {
            cachedFlags = new FeatureFlags();
            SaveFeatures(cachedFlags);
        }
    }
}