# Feature Toggles Verification Checklist

**Purpose:** Verify all new features are properly hidden behind Advanced Features toggles

---

## ?? Toggle Status

### Existing Toggles (Before This Session)
- ? `UseTaskWeighting` - Task difficulty multipliers
- ? `UsePersonAvailability` - Availability scheduling
- ? `UseRoles` - Role-based assignment
- ? `UseConstraints` - Task constraints
- ? `UseHistory` - Assignment history
- ? `UseTaskTimeEstimates` - Task duration
- ? `UseAutoRotation` - Auto rotation system
- ? `UseTaskCategories` - Task categories
- ? `UseBulkEditMode` - Bulk editing
- ? `UseAssignmentTemplates` - Save templates
- ? `UseAssignmentScheduler` - Assignment scheduler
- ? `UsePerformanceAnalytics` - Analytics dashboard
- ? `UseAssignmentNotes` - Assignment notes
- ? `UseNotifications` - Desktop notifications
- ? `UseMobileExport` - Mobile export
- ? `UseCompletionTracking` - Task completion
- ? `UseTagging` - Tag system

### New Toggles (This Session) ?
- ? `UseScheduledReports` - Report scheduling
- ? `UseEmailReports` - Email delivery

---

## ?? Feature Toggle Verification

### Performance Dashboard
**Toggle:** `UsePerformanceAnalytics`

**Locations to Check:**
- [ ] MainWindow.xaml - Menu item visibility
- [ ] MainWindow.xaml.cs - `UpdateMenusBasedOnFeatures()` line ~1336
- [ ] HistoryBrowserWindow - Dashboard button visibility

**Expected Behavior:**
- When disabled: "?? Dashboard" button in History Browser is hidden
- When enabled: Button visible and clickable

---

### Completion Statistics Window
**Toggle:** `UsePerformanceAnalytics`

**Locations to Check:**
- [ ] HistoryBrowserWindow.xaml.cs - `btnDashboard_Click()` handler
- [ ] Should not open if toggle disabled

**Expected Behavior:**
- When disabled: Cannot access from History Browser
- When enabled: Opens normally

---

### Excel Export
**Toggle:** Should be available when `UsePerformanceAnalytics` enabled

**Locations to Check:**
- [ ] CompletionStatisticsWindow.xaml - Button visibility
- [ ] PerformanceDashboardWindow.xaml - Button visibility
- [ ] MainWindow.xaml.cs - Feature check?

**Expected Behavior:**
- When disabled: Export buttons should be hidden or disabled
- When enabled: All export buttons visible

---

### PDF Export (NEW!)
**Toggle:** Should be available when `UsePerformanceAnalytics` enabled

**Locations to Check:**
- [ ] CompletionStatisticsWindow.xaml - Button visibility (line ~60)
- [ ] PerformanceDashboardWindow.xaml - Button visibility
- [ ] Code-behind - Feature gate needed?

**Expected Behavior:**
- When disabled: "?? Export to PDF" button hidden
- When enabled: Button visible and functional

---

### Report Scheduling
**Toggle:** `UseScheduledReports`

**Locations to Check:**
- [ ] MainWindow.xaml - Menu item visibility (line ~148)
- [ ] MainWindow.xaml.cs - `UpdateMenusBasedOnFeatures()` (verify line)
- [ ] MainWindow.xaml.cs - `btnReportScheduler_Click()` feature check

**Expected Behavior:**
- When disabled: "?? Report Schedule..." menu hidden, cannot access
- When enabled: Menu visible, clicking opens ReportScheduleWindow
- App initialization: `App.xaml.cs` checks toggle before initializing scheduler

---

### Email Reports
**Toggle:** `UseEmailReports`

**Locations to Check:**
- [ ] MainWindow.xaml - Menu item visibility (line ~128)
- [ ] MainWindow.xaml.cs - `UpdateMenusBasedOnFeatures()` (verify)
- [ ] MainWindow.xaml.cs - `btnEmailSettings_Click()` feature check

**Expected Behavior:**
- When disabled: "?? Email Settings..." menu hidden
- When enabled: Menu visible, clicking opens EmailSettingsWindow

---

## ?? Known Issues / Needs Verification

### 1. **Export Buttons Without Feature Gate**
**Issue:** Export buttons (PDF, Excel, CSV, Clipboard) may not be behind feature toggles

**Locations to Verify:**
- [ ] CompletionStatisticsWindow.xaml - buttons (lines 58-62)
- [ ] PerformanceDashboardWindow.xaml - buttons (lines ~?)
- Check if these should be hidden when `UsePerformanceAnalytics` is disabled

**Action Required:** May need to add Grid.Visibility binding to feature toggle

**Code Pattern:**
```xml
<Button Content="?? Export PDF" 
        Visibility="{Binding UsePerformanceAnalytics, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

---

### 2. **Statistics Window Accessibility**
**Issue:** How does Statistics window open? Check if it needs feature gate.

**Locations to Verify:**
- [ ] MainWindow.xaml - "?? Stats" button
- [ ] Who can open CompletionStatisticsWindow?
- [ ] Should it be behind `UsePerformanceAnalytics` toggle?

---

### 3. **History Browser Dashboard Button**
**Issue:** Verify dashboard button properly checks feature toggle

**Locations to Verify:**
- [ ] HistoryBrowserWindow.xaml - Button visibility
- [ ] HistoryBrowserWindow.xaml.cs - Click handler has feature check?

---

### 4. **Scheduler Initialization**
**Verification:** App.xaml.cs properly initializes scheduler only when enabled

**Location:**
- [ ] App.xaml.cs line ~32-37 - Scheduler initialization
- [ ] Code checks `features.UseScheduledReports` before calling `ScheduledReportManager.Initialize()`

---

## ?? Fix Checklist

After testing, if you find features NOT properly gated:

### For Menu Items
Add to `UpdateMenusBasedOnFeatures()` in MainWindow.xaml.cs:
```csharp
// Report Scheduler
mnuReportScheduler.Visibility = features.UseScheduledReports ? Visibility.Visible : Visibility.Collapsed;

// Email Settings  
// (add if not present)
```

### For Export Buttons
Add Visibility binding to XAML:
```xml
<Button Visibility="{Binding UsePerformanceAnalytics, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

Or in code-behind:
```csharp
private void CheckFeatureAccess()
{
    if (!features.UsePerformanceAnalytics)
    {
        MessageBox.Show("Enable Analytics in Settings ? Advanced Features", 
            "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
    }
}
```

---

## ? Testing Steps

1. **Open AdvancedFeaturesWindow** (Settings ? Advanced Features)
2. **Disable all toggles** for new features:
   - [ ] Uncheck `UseScheduledReports`
   - [ ] Uncheck `UseEmailReports`
   - [ ] Uncheck `UsePerformanceAnalytics`
   - [ ] Click Save
3. **Restart app**
4. **Verify hidden:**
   - [ ] "?? Dashboard" button in History Browser hidden
   - [ ] "?? Report Schedule..." menu hidden
   - [ ] "?? Email Settings..." menu hidden
   - [ ] Export buttons hidden (if applicable)
5. **Re-enable toggles** one by one
6. **Verify visible:** Each feature appears when enabled

---

## ?? Notes

- Feature toggles stored in `AppData/TaskAssigner/features.json`
- Changes take effect on app restart
- Default: All new features DISABLED for safety
- Users must explicitly enable in Settings

---

**Status:** ? Needs Testing  
**Priority:** HIGH - Verify feature gates work correctly  
**Owner:** Testing phase
