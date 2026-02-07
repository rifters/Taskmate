using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Taskmate
{
    /// <summary>
    /// Generates professional Excel reports from completion statistics
    /// </summary>
    public class ExcelReportGenerator
    {
        /// <summary>
        /// Creates an Excel workbook with completion statistics
        /// </summary>
        public static void GenerateCompletionStatisticsExcel(string filePath, List<PersistentAssignment> assignments)
        {
            using (var workbook = new XLWorkbook())
            {
                // Overall Statistics Sheet
                CreateOverallStatisticsSheet(workbook, assignments);

                // Person Statistics Sheet
                CreatePersonStatisticsSheet(workbook, assignments);

                // Task Statistics Sheet
                CreateTaskStatisticsSheet(workbook, assignments);

                // Monthly Trends Sheet
                CreateMonthlyTrendsSheet(workbook, assignments);

                // Save workbook
                workbook.SaveAs(filePath);
            }
        }

        private static void CreateOverallStatisticsSheet(XLWorkbook workbook, List<PersistentAssignment> assignments)
        {
            var ws = workbook.Worksheets.Add("Overall Statistics");

            int totalAssignments = assignments.Count;
            int complete = assignments.Count(a => a.OverallCompletionPercentage >= 100);
            int partial = assignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
            int incomplete = assignments.Count(a => a.OverallCompletionPercentage == 0);
            double avgCompletion = assignments.Count > 0 ? assignments.Average(a => a.OverallCompletionPercentage) : 0;

            // Header
            var headerCell = ws.Cell(1, 1);
            headerCell.Value = "Completion Statistics Report";
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:g}";

            // Data
            int row = 4;
            ws.Cell(row, 1).Value = "Metric";
            ws.Cell(row, 2).Value = "Count";
            ws.Cell(row, 3).Value = "Percentage";
            var headerRow = ws.Row(row);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;
            ws.Cell(row, 1).Value = "Total Assignments";
            ws.Cell(row, 2).Value = totalAssignments;
            ws.Cell(row, 3).Value = "100%";

            row++;
            ws.Cell(row, 1).Value = "Fully Completed";
            ws.Cell(row, 2).Value = complete;
            ws.Cell(row, 3).Value = $"{(totalAssignments > 0 ? (complete / (double)totalAssignments * 100) : 0):F1}%";
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;

            row++;
            ws.Cell(row, 1).Value = "Partial";
            ws.Cell(row, 2).Value = partial;
            ws.Cell(row, 3).Value = $"{(totalAssignments > 0 ? (partial / (double)totalAssignments * 100) : 0):F1}%";
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;

            row++;
            ws.Cell(row, 1).Value = "Incomplete";
            ws.Cell(row, 2).Value = incomplete;
            ws.Cell(row, 3).Value = $"{(totalAssignments > 0 ? (incomplete / (double)totalAssignments * 100) : 0):F1}%";
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.Red;

            row++;
            ws.Cell(row, 1).Value = "Average Completion";
            ws.Cell(row, 2).Value = $"{avgCompletion:F1}%";
            ws.Cell(row, 2).Style.Font.Bold = true;

            // Adjust columns
            ws.Column(1).Width = 25;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 15;
        }

        private static void CreatePersonStatisticsSheet(XLWorkbook workbook, List<PersistentAssignment> assignments)
        {
            var ws = workbook.Worksheets.Add("Person Statistics");

            // Header
            ws.Cell(1, 1).Value = "Person Statistics";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 12;

            int row = 3;
            ws.Cell(row, 1).Value = "Person";
            ws.Cell(row, 2).Value = "Total Assignments";
            ws.Cell(row, 3).Value = "Total Tasks";
            ws.Cell(row, 4).Value = "Completed";
            ws.Cell(row, 5).Value = "Completion %";
            ws.Cell(row, 6).Value = "Status";

            var headerRow = ws.Row(row);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

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

                    ws.Cell(row, 1).Value = person;
                    ws.Cell(row, 2).Value = personAssignments.Count;
                    ws.Cell(row, 3).Value = totalTasks;
                    ws.Cell(row, 4).Value = completedTasks;
                    ws.Cell(row, 5).Value = completionRate;
                    ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";
                    ws.Cell(row, 6).Value = status;

                    // Color code
                    if (completionRate >= 100)
                        ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.Green;
                    else if (completionRate > 0)
                        ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.Yellow;
                    else
                        ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.Red;

                    row++;
                }
            }

            // Adjust columns
            ws.Column(1).Width = 20;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 15;
            ws.Column(5).Width = 15;
            ws.Column(6).Width = 15;
        }

        private static void CreateTaskStatisticsSheet(XLWorkbook workbook, List<PersistentAssignment> assignments)
        {
            var ws = workbook.Worksheets.Add("Task Statistics");

            // Header
            ws.Cell(1, 1).Value = "Task Statistics";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 12;

            int row = 3;
            ws.Cell(row, 1).Value = "Task";
            ws.Cell(row, 2).Value = "Times Assigned";
            ws.Cell(row, 3).Value = "Times Completed";
            ws.Cell(row, 4).Value = "Completion %";
            ws.Cell(row, 5).Value = "Status";

            var headerRow = ws.Row(row);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            var tasks = assignments
                .SelectMany(a => a.Assignments.SelectMany(ar => ar.Tasks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            foreach (var task in tasks)
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
                    string status = timesCompleted == timesAssigned ? "Always Done" : timesCompleted == 0 ? "Never Done" : "Sometimes Done";

                    ws.Cell(row, 1).Value = task;
                    ws.Cell(row, 2).Value = timesAssigned;
                    ws.Cell(row, 3).Value = timesCompleted;
                    ws.Cell(row, 4).Value = completionRate;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "0.0\"%\"";
                    ws.Cell(row, 5).Value = status;

                    // Color code
                    if (completionRate >= 100)
                        ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Green;
                    else if (completionRate > 0)
                        ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Yellow;
                    else
                        ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Red;

                    row++;
                }
            }

            // Adjust columns
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 18;
            ws.Column(4).Width = 15;
            ws.Column(5).Width = 15;
        }

        private static void CreateMonthlyTrendsSheet(XLWorkbook workbook, List<PersistentAssignment> assignments)
        {
            var ws = workbook.Worksheets.Add("Monthly Trends");

            // Header
            ws.Cell(1, 1).Value = "Monthly Trends";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 12;

            int row = 3;
            ws.Cell(row, 1).Value = "Month";
            ws.Cell(row, 2).Value = "Complete";
            ws.Cell(row, 3).Value = "Partial";
            ws.Cell(row, 4).Value = "Incomplete";
            ws.Cell(row, 5).Value = "Avg Completion %";

            var headerRow = ws.Row(row);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

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

                ws.Cell(row, 1).Value = group.Key;
                ws.Cell(row, 2).Value = complete;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.Green;
                ws.Cell(row, 3).Value = partial;
                ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.Yellow;
                ws.Cell(row, 4).Value = incomplete;
                ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.Red;
                ws.Cell(row, 5).Value = avgCompletion;
                ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";

                row++;
            }

            // Adjust columns
            ws.Column(1).Width = 15;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 12;
            ws.Column(5).Width = 18;
        }
    }
}
