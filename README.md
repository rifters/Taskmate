# Task Assigner - Intelligent Task Distribution & Management Platform

![Status](https://img.shields.io/badge/Status-Active%20Development-blue)
![Platform](https://img.shields.io/badge/Platform-.NET%2010-blueviolet)
![License](https://img.shields.io/badge/License-MIT-green)

## ?? Overview

**Task Assigner** is a powerful .NET desktop application that intelligently distributes tasks among people with fairness algorithms, advanced scheduling, and comprehensive historical tracking. Perfect for restaurants, offices, households, event teams, or any environment requiring fair task assignment.

### Key Strengths
- ? **Intelligent Load Balancing** - Ensures truly fair distribution based on capacity, difficulty, or time
- ? **Constraint-Based Assignments** - Respect allergies, physical limitations, skill requirements
- ? **Rich History & Analytics** - Complete audit trail with person-specific history and trends
- ? **Flexible Scheduling** - One-time or recurring scheduled assignments
- ? **Multi-Export Formats** - CSV, Print, Mobile HTML with QR codes
- ? **Persistent Group Management** - Save entire configurations (tasks, people, constraints, settings)
- ? **Assignment Confirmation Workflow** - Review before committing to history

---

## ?? Features

### Core Features (Always Available)

#### ?? Task & People Management
- Load tasks/people from text files or manage inline
- Drag-and-drop file support
- Add/edit/delete items with ease
- Export to JSON or TXT formats
- Create and load reusable groups (*.tgroup files)

#### ?? Intelligent Assignment
- Fair random task distribution
- Automatic capacity-based load balancing
- Real-time fairness scoring (0-100%)
- Visual workload indicators (color-coded)
- One-click assignment with F5 or button click

#### ?? Dashboard & Insights
- Live last assignment timestamp
- Fairness score with quality assessment
- Overloaded/underloaded person count
- Quick stats at a glance

#### ?? Task Constraints (Optional)
- Exclude people from specific tasks
- Support for allergies, physical limitations, skill gaps
- Accessible via Edit ? Task Constraints

#### ?? Assignment History
- Permanent record of all assignments
- Browse by date, tag, or search term
- Person-specific task history
- Session history for current work
- Full audit trail with timestamps

---

### Advanced Features (Optional - Enable in Settings)

#### ?? People Management
- **Person Availability** - Mark people unavailable to exclude temporarily
- **Role-Based Assignment** - Assign roles (Server, Cook, Host, etc.)
- **Capacity Settings** - Define how much work each person can handle

#### ?? Task Properties
- **Task Difficulty/Weighting** - Easy (1x) / Medium (2x) / Hard (3x)
- **Time Estimates** - Assign task duration in minutes
- **Task Categories** - Organize into Cleaning, Cooking, Service, etc.
- **Task Notes** - Add context or instructions per task
- **Task Tagging** - Organize and filter by custom tags

#### ?? Assignment Management
- **Auto-Rotation System** - Rotate assignments fairly over time
- **Assignment Templates** - Save and reuse successful assignment patterns
- **Assignment Notes** - Add comments to assignments
- **Quick Swap** - Instantly swap all tasks between two people
- **Assignment Scheduler** - Schedule assignments to run automatically on specific dates/times
- **Bulk Edit Mode** - Edit multiple tasks/people at once

#### ?? Output & Export
- **Print Preview** - Professional bulletin board ready output
- **CSV Export** - Open in Excel/Google Sheets
- **Mobile Export** - Mobile-optimized HTML + QR code for phone access
- **Clipboard Copy** - Quick copy to paste anywhere

#### ?? Analytics & Reports (Comprehensive Suite!)
- **Performance Dashboard** - Real-time metrics with OxyPlot charts
- **Completion Statistics** - 4-tab analysis (Overall, Person, Task, Trends)
- **Rotation Reports** - Track who's been assigned what over time
- **Person Task History** - Complete history for individual people
- **Statistics Window** - Detailed assignment statistics
- **Multiple Export Formats** - PDF, Excel, CSV, Clipboard
- **Scheduled Reports** - Automated daily/weekly/monthly generation
- **Email Delivery** - Auto-send reports to recipients

#### ?? History Management (NEW!)
- **Batch Delete** - Delete multiple assignments at once with checkboxes
- **Date Range Delete** - Remove all assignments from a specific time period
- **Search & Filter** - Find assignments by tag, person, group, or date
- **Export History** - Save assignment history to CSV for analysis
- **History Reports** - Generate formatted reports from historical data

#### ?? Performance Dashboard (NEW!)
- **Real-time Metrics** - 5 key metric cards with live updates
- **Visual Charts** - Line, Pie, and Bar charts powered by OxyPlot
- **Top Performers** - Ranking table showing best completion rates
- **Problem Identification** - Identify most incomplete tasks
- **Recent Activity** - Timeline of last 20 assignments
- **Filterable** - By date range and person
- **Export** - Copy dashboard to clipboard

#### ?? Excel Export (NEW!)
- **Professional Workbooks** - Create .xlsx files with formatting
- **4 Data Sheets:**
  - Overall Statistics (metrics & percentages)
  - Person Statistics (individual performance)
  - Task Statistics (task completion analysis)
  - Monthly Trends (historical patterns)
- **Color Coding** - Green/Yellow/Red for visual clarity
- **Proper Formatting** - Fonts, colors, column widths
- **From Any Window** - Statistics or Dashboard export

#### ?? PDF Export (NEW!)
- **Universal Format** - No Excel required, opens in any PDF reader
- **Professional Reports** - Formatted tables and metrics
- **Two Report Types:**
  - Statistics PDF (complete analysis with all tabs)
  - Dashboard PDF (key metrics and recent activity)
- **Easy Sharing** - Print-friendly, email-friendly
- **From Statistics & Dashboard** - Export directly from windows

#### ?? Scheduled Reports (NEW!)
- **Automated Generation** - Daily, weekly, or monthly
- **Report Types:**
  - Statistics Reports (full analysis)
  - Dashboard Reports (summary metrics)
  - Both types combined
- **Output Formats** - Excel, Text, or CSV
- **Folder Storage** - Auto-save to configured directory
- **Execution Logging** - Track when reports generated
- **Enable/Disable** - Toggle individual schedules anytime

#### ?? Email Reports (NEW!)
- **SMTP Configuration** - Gmail, Outlook, custom servers
- **Secure Storage** - Encrypted credential management
- **HTML Templates** - Professional email formatting
- **Automatic Delivery** - Send reports on schedule
- **Connection Testing** - Verify settings before use
- **App Password Support** - Gmail 2FA compatible

#### ? Task Completion Tracking (Enhanced)
- **Desktop Notifications** - Get alerts when assignments complete
- **Persistent Notes** - Add notes/context to assignments
- **Session Timeout** - Auto-lock after inactivity (security)
- **Audit Logging** - Complete activity log for compliance

---

## ?? Quick Start

### 1. **Load Your Data**
   - **File ? Load Sample Files** (to try it out)
   - OR drag-drop your task.txt and people.txt files
   - OR use **Load Tasks** / **Load People** buttons

### 2. **Configure (Optional)**
   - Set constraints (Edit ? Task Constraints)
   - Enable advanced features (Settings ? Advanced Features)
   - Configure availability, roles, weights, etc.

### 3. **Assign Tasks**
   - Press **F5** or click **?? Assign (F5)**
   - Review the assignments on-screen
   - Can press F5 again for a new random assignment without saving

### 4. **Post Assignment** (New Workflow!)
   - Once happy with the assignment, click **?? Post**
   - This saves to permanent history
   - Assignment grid clears for next batch
   - Data becomes available in analytics and person history

### 5. **Export & Share**
   - Copy assignments to clipboard (Ctrl+C)
   - Export to CSV
   - Print for bulletin board (Ctrl+P)
   - Share mobile link via QR code

---

## ?? Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **F5** | Assign Tasks |
| **Ctrl+A** | Assign Tasks |
| **Ctrl+Z** | Undo Last Assignment |
| **Ctrl+S** | Save Group |
| **Ctrl+O** | Load Group |
| **Ctrl+C** | Copy All Assignments |
| **Ctrl+P** | Print Preview |
| **Ctrl+H** | Help |
| **Ctrl+Shift+H** | Browse All History |

---

## ?? Understanding the Dashboard

### Fairness Score
- **90%+ (Green)** - Excellent fairness, perfectly balanced
- **75-90% (Orange)** - Good fairness, acceptable distribution
- **<75% (Red)** - Fair fairness, consider re-running or adjusting

### Workload Indicators
- ?? **Green** - Underloaded (<80% of average)
- ? **Normal** - Balanced (80-100% of average)
- ?? **Yellow** - Slightly high (100-120% of average)
- ?? **Red** - Overloaded (>120% of average)

---

## ?? File Formats

### Task/People Files (.txt)
```
Item Name
Another Item
Third Item
```
One item per line, plain text.

### Group Files (.tgroup)
JSON format containing:
- Task list
- People list
- Capacity settings
- Constraints
- Task properties (weights, time, category, notes)
- Availability & roles
- All advanced settings

### History Storage
- **Location**: `AppData\Roaming\TaskAssigner\History\YYYY-MM\`
- **Format**: JSON with timestamp
- **Retention**: Permanent until manually deleted

---

## ?? Task Completion Tracking (Advanced Feature)

### Overview
Track which tasks are actually completed as work progresses. Mark tasks, view completion status, and analyze trends over time.

### Enabling the Feature
1. Go to **Settings ? Advanced Features**
2. Enable **? Task Completion Tracking**
3. Restart application for full effect

### How to Use

#### During Assignment
1. **Assign tasks** using ?? Assign (F5)
2. **Expand person's row** to see task checkboxes
3. **Check boxes** for completed tasks as work is done
4. **Auto-complete**: When all tasks checked, "IsPersonComplete" auto-checks
5. **Quick complete**: Use **"? Mark All Complete"** button
6. **Reset**: Click **"?? Reset All"** to uncheck all

#### After Posting to History
1. **Browse All History** (Ctrl+Shift+H)
2. **View completion %** in Completion column (color-coded)
3. **Filter by status** using Status dropdown
4. **Edit completion**: Select assignment ? Click **"?? Edit Completion"**
5. **View detailed stats**: Click **"?? View Stats"**

### Color Coding
- **?? Green** (100%) - All tasks completed
- **?? Yellow** (1-99%) - Some tasks completed
- **?? Red** (0%) - No tasks completed

### Statistics & Analytics
Access **Completion Statistics window** to view:
- **Overall Statistics** - Total, Complete, Partial, Incomplete counts
- **Person Statistics** - Individual completion rates and performance
- **Task Statistics** - Which tasks are most/least completed
- **Trend Analysis** - Month-by-month completion patterns

### Export Options
- **Copy to Clipboard** - Paste statistics into Excel/Word
- **CSV Export** - Download as spreadsheet
- **Detailed Reports** - Generate formatted reports

---

## ?? Analytics & Reporting Features

### Performance Dashboard
**Access:** History Browser ? Click "?? Dashboard" button

**What You Get:**
- Real-time completion metrics
- Interactive OxyPlot charts (trend, status, performance)
- Top performers ranking
- Problem task identification
- Recent activity timeline
- Date range and person filtering
- Export to clipboard

### Excel Export
**Access:** Completion Statistics Window ? Click "?? Export to Excel"

**Creates:**
- Professional Excel workbook (.xlsx)
- 4 formatted sheets with color coding
- Perfect for reports and sharing
- Works offline

### CSV Export
**Access:** Statistics Window ? Click "?? Export to CSV"

**Good for:**
- Import into other tools
- Data analysis in spreadsheet software
- Easy sharing and backup

---

## ?? Scheduled Reports (NEW!)
**Access:** Tools ? Report Schedule

**Features:**
- ? Daily, Weekly, or Monthly frequency
- ? Automatic report generation
- ? Multiple report types (Statistics/Dashboard/Both)
- ? Excel and text file output
- ? Execution logging
- ? Enable/disable schedules anytime

### Example Workflow:
1. **Settings ? Email Settings** - Configure SMTP (Gmail, etc.)
2. **Tools ? Report Schedule** - Create daily report schedule
3. **Auto-generated** - Reports save to folder every day at specified time

---

## ?? Email Reports (NEW!)
**Access:** Settings ? Email Settings

**Features:**
- ? Gmail and other SMTP providers
- ? SSL/TLS encryption support
- ? Connection testing
- ? HTML-formatted emails
- ? App password support (Gmail recommended)
- ? Secure credential storage

### Setup:
1. **Gmail Users:** Enable 2-Step Verification ? Generate App Password
2. **Settings ? Email Settings** - Enter SMTP details
3. **Test Connection** - Verify settings before saving
4. **Tools ? Report Schedule** - Add "Send Email" option

---

## ?? Configuration

### Settings ? General Settings
- Theme selection (Light/Dark/System)
- Auto-save options
- Notification preferences

### Settings ? Advanced Features
- Toggle 20+ features on/off
- Custom assignment save location
- Feature-specific configuration

### Settings ? Security Settings
- Audit logging (activity tracking)
- Session timeout
- Data privacy options

### Settings ? Backup & Restore
- Automatic scheduled backups
- Full data backup (entire history)
- Restore from backup
- GDPR data deletion

---

## ?? Use Cases

### Restaurants & Hospitality
- Daily task rotation for front/back of house
- Assign based on experience level
- Fair section/station rotation
- Opening/closing checklists

### Offices & Corporate
- Meeting room scheduling
- Task assignment for projects
- Role-based workload distribution
- Compliance & audit trails

### Households
- Chore distribution among family
- Fair rotation system
- Capacity-based assignments
- Historical fairness tracking

### Events & Volunteer Management
- Shift scheduling
- Volunteer task assignment
- Skill-based distribution
- Event history tracking

### Schools & Education
- Classroom duty assignment
- Club/organization task distribution
- Fair workload for students/teachers
- Attendance/participation tracking

---

## ??? Architecture

### Technology Stack
- **Framework**: .NET 10 (Latest)
- **UI**: WPF (Windows Presentation Foundation)
- **Data**: JSON (with file-based storage)
- **Language**: C#

### Key Components
- **Main Window** - Core assignment interface
- **History Manager** - Persistent storage & retrieval
- **Constraint Engine** - Validates assignments
- **Rotation Tracker** - Tracks long-term fairness
- **Scheduler** - Manages automated assignments
- **Backup Manager** - Data protection & recovery

---

## ?? Assignment Workflow

### New Confirmation-Based Workflow

```
1. Load Group / Tasks & People
          ?
2. Press F5 (Assign)
          ?
3. Review Assignments on Screen
          ?
4. Options:
   - Press F5 again ? New random assignment (no save)
   - Click ?? Post ? Save to history
```

**Key Benefit**: You can generate multiple assignments and choose the best one before committing to history!

---

## ?? Person Task History

**Tools ? Person Task History** provides:
- ? Complete task history for any individual
- ? Filter by date range
- ? Statistics (total tasks, max, min, avg, count)
- ? Identify overloaded/underloaded patterns
- ? Export-ready grid view

**Perfect for:**
- Manager reviews of individual workload
- Identifying fairness issues
- Training new team members
- Performance discussions

---

## ?? Smart Assignment Algorithm

### Load Balancing Steps
1. Randomize task order
2. Randomize people order
3. Calculate target tasks per person (based on capacity)
4. Distribute round-robin with constraint checking
5. Assign remaining tasks to people with capacity
6. Calculate fairness score
7. Color-code results by workload

### Constraint Respect
- Skips people excluded from specific tasks
- Respects availability settings
- Never assigns unavailable people
- Handles skill-based requirements

### Fairness Calculation
```
Fairness = 100 - (StdDev / AvgTasks) × 100
- StdDev = Standard deviation of task counts
- AvgTasks = Average tasks per person
- Result: 0-100% (higher is better)
```

---

## ?? Known Limitations & Future Enhancements

### Current Limitations
- Single-user per instance (no multi-user sync)
- Windows only (WPF limitation)
- No cloud sync (local storage only)
- Manual backup recommended (auto-backup available)

### Planned Features
- Multi-user/team collaboration
- Cloud sync option
- Mobile apps (iOS/Android)
- Advanced AI-based fairness
- REST API for integrations
- Custom scheduling rules

---

## ?? Documentation

- **In-App Help**: Press Ctrl+H or Help ? User Guide
- **Shortcuts**: Help ? Keyboard Shortcuts
- **This README**: Full feature overview

---

## ?? Contributing

Contributions welcome! This project is open-source on GitHub:
- **Repository**: https://github.com/rifters/Taskmate
- **Issues**: Report bugs or request features
- **Pull Requests**: Submit improvements

---

## ?? License

This project is licensed under the MIT License - see LICENSE file for details.

---

## ?? Support & Feedback

- **Questions?** Check Help ? User Guide
- **Bug Report?** Submit on GitHub Issues
- **Feature Request?** GitHub Issues > Feature Request
- **Email**: Via GitHub repository

---

## ?? Version History

### v2.0 (Current - Assignment Workflow & History Improvements)
- ? Added Post button for explicit assignment confirmation
- ? Created Person Task History window
- ? Improved assignment workflow (review before committing)
- ? Enhanced scheduler execution
- ? Added constraint persistence to groups
- ?? Fixed assignment path handling
- ?? Fixed XAML parsing issues

### v1.9 (Previous)
- Added Assignment Scheduler
- Added Person Availability system
- Improved UI/UX

### v1.0 - v1.8
- Core assignment features
- History and analytics
- Export functionality
- Advanced features framework

---

**Last Updated**: 2024  
**Current Version**: 2.0  
**Status**: Active Development

