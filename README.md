# Taskmate

Taskmate is a comprehensive WPF-based Windows task manager designed to help you organize, assign, and track tasks efficiently. Whether you're managing personal projects, team assignments, or household chores, Taskmate provides powerful features to streamline task management and ensure fair distribution of work.

## Features

### Task Planning & Grouping
- Create and manage tasks with detailed information including notes, time estimates, and categories
- Organize tasks into groups for better structure and management
- Import and export task lists for easy sharing and backup
- Support for task templates to quickly set up recurring task sets
- Bulk editing capabilities for efficient task management

### Task Assignment & Scheduling
- Smart task assignment with configurable constraints and weights
- Automatic fair distribution based on capacity and availability
- Role-based assignment to match tasks with appropriate people
- Scheduled assignments with recurring patterns (daily, weekly, monthly)
- Task rotation tracking to ensure equitable distribution over time
- Conflict detection and swap functionality for flexible reassignment

### Notifications
- **Email notifications** via MailKit - Send task assignments and reminders via email
- **SMS notifications** via Twilio - Deliver urgent notifications through text messages
- **Windows toast notifications** - Native desktop notifications for immediate alerts
- Configurable notification preferences per user
- Notification history and audit logging

### Analytics & History
- Comprehensive analytics dashboard showing task distribution and trends
- Detailed assignment history browser with filtering and search
- Fairness metrics to track workload balance across team members
- Top contributor reports and task frequency analysis
- Statistical views showing assignment patterns over customizable time periods
- Export analytics data to CSV for external analysis

### Data Export & Backup
- Export assignments to CSV format
- Mobile-friendly QR code export for easy access on the go
- Print preview functionality for physical records
- Automated backup scheduling with configurable frequency (daily, weekly, monthly)
- Backup retention policies to manage storage
- Backup location: `%AppData%\Taskmate\backup_schedule.json`

### Themes & Customization
- Light and Dark theme support
- System theme integration (automatically follows Windows theme)
- Customizable color schemes
- Persistent user preferences

### Security & Session Management
- Session timeout management for enhanced security
- Audit logging for accountability and tracking
- Secure credential storage for notification services
- User preference isolation

## Requirements

- **Operating System**: Windows 10 (build 17763) or later
- **.NET Framework**: .NET 10.0
- **Windows SDK**: 10.0.17763.0 or later
- **Dependencies**: Automatically restored via NuGet (see Dependencies section)

## Getting Started

### Clone the Repository
```bash
git clone https://github.com/rifters/Taskmate.git
cd Taskmate
```

### Restore Dependencies
```bash
dotnet restore
```

### Build the Application
```bash
dotnet build
```

### Run the Application
```bash
dotnet run --project Taskmate/Taskmate.csproj
```

Or open `Taskmate.slnx` in Visual Studio and press F5 to build and run.

## Configuration

### Notification Services
Configure email and SMS notifications through the Notification Settings window:
- **Email**: Requires SMTP server details and credentials (uses MailKit)
- **SMS**: Requires Twilio account SID and Auth Token
- **Toast**: Enabled by default on Windows 10+

### Backup Schedule
The backup schedule configuration is stored at:
```
%AppData%\Taskmate\backup_schedule.json
```

You can configure:
- Backup frequency (daily, weekly, monthly)
- Preferred backup time
- Retention period for old backups
- Notification preferences on backup completion

### Themes
Switch between Light, Dark, or System themes via the Settings window. Theme preference is automatically saved and applied on application restart.

### Security & Session Settings
Configure session timeout duration through the Security Settings window to automatically lock the application after a period of inactivity.

## Dependencies

Taskmate uses the following NuGet packages:

- **Microsoft.Toolkit.Uwp.Notifications** (7.1.3) - Windows toast notifications
- **MailKit** (4.8.0) - Email notification delivery
- **Twilio** (7.6.0) - SMS notification delivery
- **QRCoder** (1.6.0) - QR code generation for mobile export

## Contributing

We appreciate your interest in Taskmate! Suggestions and contributions are welcome. If you have ideas for improvements or encounter any issues, please feel free to open an issue or submit a pull request on GitHub.

## License

Please refer to the repository for license information.
