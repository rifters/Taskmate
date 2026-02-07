using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Taskmate
{
    /// <summary>
    /// Generates professional PDF reports from completion data
    /// </summary>
    public class PdfReportGenerator
    {
        /// <summary>
        /// Generate Statistics PDF Report
        /// </summary>
        public static void GenerateStatisticsReport(string filePath, List<PersistentAssignment> assignments)
        {
            using (var writer = new PdfWriter(filePath))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                // Set margins
                document.SetMargins(20, 20, 20, 20);

                // Title
                var title = new Paragraph("COMPLETION STATISTICS REPORT")
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);

                // Date
                var dateInfo = new Paragraph($"Generated: {DateTime.Now:g}")
                    .SetFontSize(11)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(dateInfo);

                // Summary Section
                document.Add(CreateSummarySection(assignments));

                // Person Statistics Section
                document.Add(CreatePersonStatisticsSection(assignments));

                // Task Statistics Section
                document.Add(CreateTaskStatisticsSection(assignments));

                // Monthly Trends Section
                document.Add(CreateMonthlyTrendsSection(assignments));
            }
        }

        /// <summary>
        /// Generate Dashboard PDF Report
        /// </summary>
        public static void GenerateDashboardReport(string filePath, List<PersistentAssignment> assignments)
        {
            using (var writer = new PdfWriter(filePath))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                // Set margins
                document.SetMargins(20, 20, 20, 20);

                // Title
                var title = new Paragraph("PERFORMANCE DASHBOARD REPORT")
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);

                // Date
                var dateInfo = new Paragraph($"Generated: {DateTime.Now:g}")
                    .SetFontSize(11)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(dateInfo);

                // Key Metrics
                document.Add(CreateKeyMetricsSection(assignments));

                // Top Performers
                document.Add(CreateTopPerformersSection(assignments));

                // Recent Activity
                document.Add(CreateRecentActivitySection(assignments));
            }
        }

        private static Div CreateSummarySection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("OVERALL STATISTICS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var total = assignments.Count;
            var complete = assignments.Count(a => a.OverallCompletionPercentage >= 100);
            var partial = assignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
            var incomplete = assignments.Count(a => a.OverallCompletionPercentage == 0);
            var avgCompletion = total > 0 ? assignments.Average(a => a.OverallCompletionPercentage) : 0;

            var table = new Table(2);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Metric").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Value").SetFontSize(12)));

            // Data rows
            table.AddCell("Total Assignments");
            table.AddCell(total.ToString());

            table.AddCell("Fully Completed");
            table.AddCell($"{complete} ({(total > 0 ? (complete / (double)total * 100) : 0):F1}%)");

            table.AddCell("Partial");
            table.AddCell($"{partial} ({(total > 0 ? (partial / (double)total * 100) : 0):F1}%)");

            table.AddCell("Incomplete");
            table.AddCell($"{incomplete} ({(total > 0 ? (incomplete / (double)total * 100) : 0):F1}%)");

            table.AddCell("Average Completion");
            table.AddCell($"{avgCompletion:F1}%");

            section.Add(table);
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreateKeyMetricsSection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("KEY METRICS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var total = assignments.Count;
            var complete = assignments.Count(a => a.OverallCompletionPercentage >= 100);
            var partial = assignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
            var incomplete = assignments.Count(a => a.OverallCompletionPercentage == 0);
            var avgCompletion = total > 0 ? assignments.Average(a => a.OverallCompletionPercentage) : 0;

            var metrics = $@"
Total Assignments: {total}
Average Completion: {avgCompletion:F1}%
Fully Completed: {complete} ({(total > 0 ? (complete / (double)total * 100) : 0):F1}%)
Partial: {partial} ({(total > 0 ? (partial / (double)total * 100) : 0):F1}%)
Incomplete: {incomplete} ({(total > 0 ? (incomplete / (double)total * 100) : 0):F1}%)";

            section.Add(new Paragraph(metrics).SetFontSize(11));
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreatePersonStatisticsSection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("PERSON STATISTICS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var table = new Table(5);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Person").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Total Tasks").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Completed").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Completion %").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetFontSize(12)));

            var people = assignments
                .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            foreach (var person in people)
            {
                var personAssignments = assignments
                    .Where(a => a.Assignments.Any(ar => ar.Person == person))
                    .SelectMany(a => a.Assignments.Where(ar => ar.Person == person))
                    .ToList();

                if (personAssignments.Count > 0)
                {
                    int totalTasks = personAssignments.Sum(a => a.TaskCount);
                    int completedTasks = personAssignments.Sum(a => a.CompletedCount);
                    double completionRate = totalTasks > 0 ? (completedTasks / (double)totalTasks * 100) : 0;
                    string status = completionRate >= 100 ? "Complete" : completionRate > 0 ? "Partial" : "Incomplete";

                    table.AddCell(person);
                    table.AddCell(totalTasks.ToString());
                    table.AddCell(completedTasks.ToString());
                    table.AddCell($"{completionRate:F1}%");
                    table.AddCell(status);
                }
            }

            section.Add(table);
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreateTaskStatisticsSection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("TASK STATISTICS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var table = new Table(4);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Task").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Times Assigned").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Times Completed").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Completion %").SetFontSize(12)));

            var tasks = assignments
                .SelectMany(a => a.Assignments.SelectMany(ar => ar.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            foreach (var task in tasks.Take(20)) // Limit to 20 tasks per page
            {
                int timesAssigned = 0;
                int timesCompleted = 0;

                foreach (var assignment in assignments)
                {
                    foreach (var person in assignment.Assignments)
                    {
                        if (person.Tasks.Contains(task, StringComparison.OrdinalIgnoreCase))
                        {
                            timesAssigned++;
                            if (person.CompletedTasks.Contains(task))
                                timesCompleted++;
                        }
                    }
                }

                if (timesAssigned > 0)
                {
                    double completionRate = (timesCompleted / (double)timesAssigned * 100);
                    table.AddCell(task);
                    table.AddCell(timesAssigned.ToString());
                    table.AddCell(timesCompleted.ToString());
                    table.AddCell($"{completionRate:F1}%");
                }
            }

            section.Add(table);
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreateMonthlyTrendsSection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("MONTHLY TRENDS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var table = new Table(5);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Month").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Complete").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Partial").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Incomplete").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Avg %").SetFontSize(12)));

            var monthlyData = assignments
                .GroupBy(a => a.Timestamp.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in monthlyData)
            {
                int complete = group.Count(a => a.OverallCompletionPercentage >= 100);
                int partial = group.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
                int incomplete = group.Count(a => a.OverallCompletionPercentage == 0);
                double avgCompletion = group.Average(a => a.OverallCompletionPercentage);

                table.AddCell(group.Key);
                table.AddCell(complete.ToString());
                table.AddCell(partial.ToString());
                table.AddCell(incomplete.ToString());
                table.AddCell($"{avgCompletion:F1}%");
            }

            section.Add(table);
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreateTopPerformersSection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("TOP PERFORMERS")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var table = new Table(2);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Person").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Completion %").SetFontSize(12)));

            var people = assignments
                .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                .Distinct()
                .ToList();

            var personStats = new List<(string, double)>();
            foreach (var person in people)
            {
                var personAssignments = assignments
                    .Where(a => a.Assignments.Any(ar => ar.Person == person))
                    .SelectMany(a => a.Assignments.Where(ar => ar.Person == person))
                    .ToList();

                if (personAssignments.Count > 0)
                {
                    int total = personAssignments.Sum(a => a.TaskCount);
                    int completed = personAssignments.Sum(a => a.CompletedCount);
                    double rate = total > 0 ? (completed / (double)total * 100) : 0;
                    personStats.Add((person, rate));
                }
            }

            foreach (var stat in personStats.OrderByDescending(p => p.Item2).Take(10))
            {
                table.AddCell(stat.Item1);
                table.AddCell($"{stat.Item2:F1}%");
            }

            section.Add(table);
            section.SetMarginBottom(20);

            return section;
        }

        private static Div CreateRecentActivitySection(List<PersistentAssignment> assignments)
        {
            var section = new Div();

            var heading = new Paragraph("RECENT ACTIVITY")
                .SetFontSize(14)
                .SetMarginBottom(10);
            section.Add(heading);

            var table = new Table(4);
            table.SetWidth(UnitValue.CreatePercentValue(100));

            // Header row
            table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Tag").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("People").SetFontSize(12)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Completion %").SetFontSize(12)));

            var recentActivity = assignments
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .ToList();

            foreach (var activity in recentActivity)
            {
                table.AddCell(activity.Timestamp.ToString("g"));
                table.AddCell(activity.Tag ?? "N/A");
                table.AddCell(activity.Assignments.Count.ToString());
                table.AddCell($"{activity.OverallCompletionPercentage:F1}%");
            }

            section.Add(table);

            return section;
        }
    }
}
