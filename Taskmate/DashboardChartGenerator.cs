using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Taskmate
{
    /// <summary>
    /// Generates OxyPlot charts for the Performance Dashboard
    /// </summary>
    public class DashboardChartGenerator
    {
        /// <summary>
        /// Creates a line chart showing completion trends over time
        /// </summary>
        public static PlotModel CreateCompletionTrendChart(List<PersistentAssignment> assignments)
        {
            var model = new PlotModel { Title = "Completion Trend (Monthly)" };

            // Group by month and calculate averages
            var monthlyData = assignments
                .GroupBy(a => a.Timestamp.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Month = g.Key,
                    AvgCompletion = g.Average(a => a.OverallCompletionPercentage)
                })
                .ToList();

            var lineSeries = new LineSeries
            {
                Title = "Avg Completion %",
                Color = OxyColor.FromRgb(76, 175, 80),
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 5
            };

            foreach (var data in monthlyData)
            {
                lineSeries.Points.Add(new DataPoint(
                    OxyPlot.Axes.DateTimeAxis.ToDouble(DateTime.Parse(data.Month + "-01")),
                    data.AvgCompletion));
            }

            model.Series.Add(lineSeries);

            // X-axis: Time
            var xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "MMM yyyy",
                Title = "Month"
            };
            model.Axes.Add(xAxis);

            // Y-axis: Percentage
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = 100,
                Title = "Completion %"
            };
            model.Axes.Add(yAxis);

            return model;
        }

        /// <summary>
        /// Creates a bar chart showing person performance
        /// </summary>
        public static PlotModel CreatePersonPerformanceChart(List<PersistentAssignment> assignments)
        {
            var model = new PlotModel { Title = "Top 10 Performers" };

            // Calculate person stats
            var people = assignments
                .SelectMany(a => a.Assignments.Select(ar => ar.Person))
                .Distinct()
                .ToList();

            var personStats = new List<(string Name, double Rate)>();
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

            var barSeries = new BarSeries { Title = "Completion Rate %" };
            int index = 0;
            foreach (var stat in personStats.OrderByDescending(p => p.Rate).Take(10))
            {
                barSeries.Items.Add(new BarItem { Value = stat.Rate });
                index++;
            }

            model.Series.Add(barSeries);

            // Y-axis: People (CategoryAxis required for BarSeries)
            var yAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Person" };
            foreach (var stat in personStats.OrderByDescending(p => p.Rate).Take(10))
            {
                yAxis.Labels.Add(stat.Name);
            }
            model.Axes.Add(yAxis);

            // X-axis: Percentage (LinearAxis)
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 100,
                Title = "Completion %"
            };
            model.Axes.Add(xAxis);

            return model;
        }

        /// <summary>
        /// Creates a pie chart showing completion status distribution
        /// </summary>
        public static PlotModel CreateCompletionStatusChart(List<PersistentAssignment> assignments)
        {
            var model = new PlotModel { Title = "Completion Status Distribution" };

            int complete = assignments.Count(a => a.OverallCompletionPercentage >= 100);
            int partial = assignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100);
            int incomplete = assignments.Count(a => a.OverallCompletionPercentage == 0);

            var pieSeries = new PieSeries
            {
                StartAngle = 0,
                AngleIncrement = 1
            };

            pieSeries.Slices.Add(new PieSlice($"Complete ({complete})", complete) { Fill = OxyColor.FromRgb(76, 175, 80) });
            pieSeries.Slices.Add(new PieSlice($"Partial ({partial})", partial) { Fill = OxyColor.FromRgb(255, 152, 0) });
            pieSeries.Slices.Add(new PieSlice($"Incomplete ({incomplete})", incomplete) { Fill = OxyColor.FromRgb(244, 67, 54) });

            model.Series.Add(pieSeries);

            return model;
        }
    }
}
