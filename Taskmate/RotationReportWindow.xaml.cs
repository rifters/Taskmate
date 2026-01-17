using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class RotationReportWindow : Window
    {
        public class ReportItem
        {
            public string Person { get; set; } = string.Empty;
            public string Task { get; set; } = string.Empty;
            public int Count { get; set; }
            public DateTime LastDate { get; set; }
        }

        public RotationReportWindow(List<string> people, List<string> tasks)
        {
            InitializeComponent();
            LoadReport(people, tasks);
        }

        private void LoadReport(List<string> people, List<string> tasks)
        {
            var report = RotationTracker.GetFullRotationReport(people, tasks, 30);
            var items = new List<ReportItem>();

            foreach (var person in report.Keys)
            {
                foreach (var task in report[person].Keys)
                {
                    items.Add(new ReportItem
                    {
                        Person = person,
                        Task = task,
                        Count = report[person][task],
                        LastDate = DateTime.Now // Would need to track this in RotationTracker for accuracy
                    });
                }
            }

            dgReport.ItemsSource = items.OrderBy(i => i.Person).ThenBy(i => i.Task).ToList();
        }

        private void btnClearOld_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will delete rotation data older than 90 days. Continue?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RotationTracker.ClearOldRecords(90);
                MessageBox.Show("Old data cleared successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}