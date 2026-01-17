using System;
using System.IO;

namespace Taskmate.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EnableSessionTimeout {
            get {
                return ((bool)(this["EnableSessionTimeout"]));
            }
            set {
                this["EnableSessionTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int SessionTimeoutMinutes {
            get {
                return ((int)(this["SessionTimeoutMinutes"]));
            }
            set {
                this["SessionTimeoutMinutes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EnableAuditLog {
            get {
                return ((bool)(this["EnableAuditLog"]));
            }
            set {
                this["EnableAuditLog"] = value;
            }
        }
        
        public static void SaveBackupScheduleSettings(BackupScheduleSettings settings)
        {
            // Example implementation: Save to user settings or a config file.
            // Adjust as needed for your application's persistence mechanism.
            // For demonstration, let's assume you serialize to a file.
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Taskmate",
                "backup_schedule.json");
        
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            File.WriteAllText(path, json);
        }

        public static BackupScheduleSettings LoadBackupScheduleSettings()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Taskmate",
                "backup_schedule.json");
            
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return System.Text.Json.JsonSerializer.Deserialize<BackupScheduleSettings>(json) ?? new BackupScheduleSettings();
                }
                catch
                {
                    return new BackupScheduleSettings();
                }
            }
            
            return new BackupScheduleSettings();
        }
    }
}
