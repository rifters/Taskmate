using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Taskmate
{
    public static class NotificationManager
    {
        private const string AppId = "Taskmate.TaskAssigner";
        
        private static DateTime _lastNotificationTime = DateTime.MinValue;
        private static int _notificationCount = 0;
        private const int MAX_NOTIFICATIONS_PER_MINUTE = 5;

        public static void ShowAssignmentNotification(int peopleCount, int taskCount)
        {
            // Rate limiting
            if ((DateTime.Now - _lastNotificationTime).TotalMinutes < 1)
            {
                _notificationCount++;
                if (_notificationCount > MAX_NOTIFICATIONS_PER_MINUTE)
                {
                    System.Diagnostics.Debug.WriteLine("Notification rate limit exceeded");
                    return;
                }
            }
            else
            {
                _notificationCount = 1;
                _lastNotificationTime = DateTime.Now;
            }
            
            string title = "✅ Assignment Complete!";
            string message = $"Distributed {taskCount} tasks among {peopleCount} people";
            
            // Toast notification (Windows 10+ only)
            if (AreNotificationsSupported() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                try
                {
                    ShowToastNotificationInternal(title, message);
                }
                catch { }
            }
            
            // Email & SMS (async, don't wait)
            Task.Run(async () =>
            {
                var settings = NotificationSettings.Load();
                
                if (settings.EmailEnabled)
                {
                    await EmailNotificationService.SendEmailAsync(title, message, settings);
                }
                
                if (settings.SmsEnabled)
                {
                    await SmsNotificationService.SendSmsAsync($"{title}\n{message}", settings);
                }
            });
        }

        public static void ShowTemplateLoadedNotification(string templateName)
        {
            string title = "📋 Template Loaded";
            string message = $"Successfully loaded template: {templateName}";
            
            if (AreNotificationsSupported() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                try
                {
                    ShowToastNotificationInternal(title, message);
                }
                catch { }
            }
            
            // Email & SMS
            Task.Run(async () =>
            {
                var settings = NotificationSettings.Load();
                
                if (settings.EmailEnabled)
                {
                    await EmailNotificationService.SendEmailAsync(title, message, settings);
                }
                
                if (settings.SmsEnabled)
                {
                    await SmsNotificationService.SendSmsAsync($"{title}\n{message}", settings);
                }
            });
        }

        public static void ShowRotationAlert(string personName, string taskName, int timesAssigned)
        {
            string title = "⚠️ Rotation Alert";
            string message = $"{personName} has been assigned '{taskName}' {timesAssigned} times recently";
            
            if (AreNotificationsSupported() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                try
                {
                    ShowToastNotificationInternal(title, message);
                }
                catch { }
            }
            
            // Email & SMS
            Task.Run(async () =>
            {
                var settings = NotificationSettings.Load();
                
                if (settings.EmailEnabled)
                {
                    await EmailNotificationService.SendEmailAsync(title, message, settings);
                }
                
                if (settings.SmsEnabled)
                {
                    await SmsNotificationService.SendSmsAsync($"{title}\n{message}", settings);
                }
            });
        }

        public static void ShowCustomNotification(string title, string message)
        {
            if (AreNotificationsSupported() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                try
                {
                    ShowToastNotificationInternal(title, message);
                }
                catch { }
            }
            
            // Email & SMS
            Task.Run(async () =>
            {
                var settings = NotificationSettings.Load();
                
                if (settings.EmailEnabled)
                {
                    await EmailNotificationService.SendEmailAsync(title, message, settings);
                }
                
                if (settings.SmsEnabled)
                {
                    await SmsNotificationService.SendSmsAsync($"{title}\n{message}", settings);
                }
            });
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        private static void ShowToastNotificationInternal(string title, string message)
        {
            var content = new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddAttributionText("Task Assigner")
                .GetToastContent();
            
            ShowToast(content);
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
        private static void ShowToast(ToastContent content)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(content.GetContent());

                var toast = new ToastNotification(doc);
                toast.ExpirationTime = DateTimeOffset.Now.AddHours(1);

                ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
            }
            catch { }
        }

        public static bool AreNotificationsSupported()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return version.Major >= 10 && version.Build >= 17763;
            }
            catch
            {
                return false;
            }
        }
    }
}