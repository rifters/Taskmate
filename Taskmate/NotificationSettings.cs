using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Taskmate
{
    public class NotificationSettings
    {
        // Email Settings
        public bool EmailEnabled { get; set; }
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = "";
        
        // Store encrypted password
        private string _smtpPasswordEncrypted = "";
        
        [System.Text.Json.Serialization.JsonIgnore]
        public string SmtpPassword
        {
            get => DecryptString(_smtpPasswordEncrypted);
            set => _smtpPasswordEncrypted = EncryptString(value);
        }
        
        public string EmailFromAddress { get; set; } = "";
        public string EmailToAddress { get; set; } = "";

        // SMS Settings
        public bool SmsEnabled { get; set; }
        public string TwilioAccountSid { get; set; } = "";
        
        // Store encrypted token
        private string _twilioAuthTokenEncrypted = "";
        
        [System.Text.Json.Serialization.JsonIgnore]
        public string TwilioAuthToken
        {
            get => DecryptString(_twilioAuthTokenEncrypted);
            set => _twilioAuthTokenEncrypted = EncryptString(value);
        }
        
        public string TwilioFromNumber { get; set; } = "";
        public string SmsToNumber { get; set; } = "";

        // Serialization properties for encrypted data
        public string SmtpPasswordEncrypted 
        { 
            get => _smtpPasswordEncrypted; 
            set => _smtpPasswordEncrypted = value; 
        }
        
        public string TwilioAuthTokenEncrypted 
        { 
            get => _twilioAuthTokenEncrypted; 
            set => _twilioAuthTokenEncrypted = value; 
        }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "notification_settings.json");

        // Encrypt string using DPAPI (Windows Data Protection API)
        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes, 
                    null, 
                    DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        // Decrypt string using DPAPI
        private static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes, 
                    null, 
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static NotificationSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<NotificationSettings>(json) ?? new NotificationSettings();
                }
            }
            catch { }
            
            return new NotificationSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                
                // Set file permissions to current user only
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
                
                // Secure the file (Windows only)
                if (OperatingSystem.IsWindows())
                {
                    var fileInfo = new FileInfo(SettingsPath);
                    var fileSecurity = fileInfo.GetAccessControl();
                    fileSecurity.SetAccessRuleProtection(true, false); // Remove inherited permissions
                    fileInfo.SetAccessControl(fileSecurity);
                }
            }
            catch { }
        }

        public static BackupScheduleSettings LoadBackupScheduleSettings()
        {
            string path = GetBackupScheduleSettingsPath();
            
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<BackupScheduleSettings>(json) ?? new BackupScheduleSettings();
                }
                catch
                {
                    return new BackupScheduleSettings();
                }
            }
            
            return new BackupScheduleSettings();
        }

        public static void SaveBackupScheduleSettings(BackupScheduleSettings settings)
        {
            string path = GetBackupScheduleSettingsPath();
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static string GetBackupScheduleSettingsPath()
        {
            return Path.Combine(GetDataDirectory(), "backup_schedule.json");
        }
    }
}