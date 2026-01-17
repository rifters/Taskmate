using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Taskmate
{
    public class SessionTimeoutManager
    {
        private readonly Window _window;
        private DispatcherTimer? _timer;  // <-- Add ? to make it nullable
        private readonly int _timeoutMinutes;
        private DateTime _lastActivity;
        private bool _isLocked = false;

        public bool IsEnabled { get; set; }
        public event EventHandler? SessionTimedOut;

        public SessionTimeoutManager(Window window, int timeoutMinutes = 30)
        {
            _window = window;
            _timeoutMinutes = timeoutMinutes;
            _lastActivity = DateTime.Now;

            // Check if enabled in settings
            IsEnabled = Properties.Settings.Default.EnableSessionTimeout;

            if (!IsEnabled)
                return;

            // Timer checks every minute
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _timer.Tick += CheckTimeout;
            _timer.Start();

            // Track user activity
            _window.PreviewKeyDown += OnUserActivity;
            _window.PreviewMouseMove += OnUserActivity;
            _window.PreviewMouseDown += OnUserActivity;
        }

        private void OnUserActivity(object sender, EventArgs e)
        {
            if (!_isLocked)
                _lastActivity = DateTime.Now;
        }

        private void CheckTimeout(object? sender, EventArgs e)
        {
            if (_isLocked || !IsEnabled)
                return;

            var inactiveTime = DateTime.Now - _lastActivity;
            
            if (inactiveTime.TotalMinutes >= _timeoutMinutes)
            {
                LockSession();
            }
        }

        private void LockSession()
        {
            _isLocked = true;
            _timer?.Stop();  // <-- Add ? for null-conditional

            AuditLogger.Log("SESSION_TIMEOUT", Environment.UserName, 
                $"Session timed out after {_timeoutMinutes} minutes of inactivity");

            SessionTimedOut?.Invoke(this, EventArgs.Empty);

            var result = MessageBox.Show(
                $"⏱️ Session Timeout\n\n" +
                $"Your session has timed out due to {_timeoutMinutes} minutes of inactivity.\n\n" +
                "For security reasons, the application will now close.\n\n" +
                "Click OK to exit.",
                "Session Timed Out",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Application.Current.Shutdown();
        }

        public void ResetTimeout()
        {
            _lastActivity = DateTime.Now;
            _isLocked = false;
        }

        public void Stop()
        {
            _timer?.Stop();  // <-- Add ? for null-conditional
        }
    }
}