using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Taskmate.Utilities;

namespace Taskmate
{
    /// <summary>
    /// Manages persistent assignment history with caching, async I/O, and thread safety.
    /// 
    /// Features:
    /// - In-memory caching (30-second TTL) to improve performance
    /// - Thread-safe operations with lock statements
    /// - Async/await support for non-blocking file I/O
    /// - Comprehensive error logging via Logger utility
    /// - Performance metrics tracking
    /// - Organized file storage by year/month folders
    /// 
    /// Usage:
    /// <code>
    /// // Sync usage (blocking)
    /// var assignments = AssignmentHistoryManager.GetAllAssignments();
    /// await AssignmentHistoryManager.SaveAssignmentAsync(assignment);
    /// 
    /// // Async usage (non-blocking)
    /// var assignments = await AssignmentHistoryManager.GetAllAssignmentsAsync();
    /// </code>
    /// </summary>
    public static class AssignmentHistoryManager
    {
        private static readonly string HistoryFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "History");

        // Caching fields
        private static List<PersistentAssignment>? _cachedAssignments;
        private static DateTime _cacheTime = DateTime.MinValue;
        private const int CACHE_DURATION_MS = 30000; // 30 seconds
        private static readonly object _cacheLock = new object();

        static AssignmentHistoryManager()
        {
            Directory.CreateDirectory(HistoryFolder);
        }

        /// <summary>
        /// Invalidates the cache, forcing a reload on next access
        /// </summary>
        private static void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedAssignments = null;
                _cacheTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Saves an assignment to disk and invalidates the cache.
        /// Files are organized by year/month folders for better organization.
        /// </summary>
        /// <param name="assignment">The assignment to save. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when assignment is null</exception>
        /// <exception cref="IOException">Thrown when file I/O fails</exception>
        public static void SaveAssignment(PersistentAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment), "Assignment cannot be null");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Organize by year/month folders
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                Directory.CreateDirectory(folderPath);

                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(assignment, options);
                File.WriteAllText(filePath, json);

                // Invalidate cache after save
                InvalidateCache();
                
                stopwatch.Stop();
                Logger.LogPerformance($"SaveAssignment ({assignment.Tag})", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError($"SaveAssignment failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Async version of SaveAssignment - performs non-blocking file I/O.
        /// Recommended for UI operations to prevent blocking the UI thread.
        /// </summary>
        /// <param name="assignment">The assignment to save. Must not be null.</param>
        /// <returns>A task that completes when the assignment is saved</returns>
        /// <exception cref="ArgumentNullException">Thrown when assignment is null</exception>
        public static async Task SaveAssignmentAsync(PersistentAssignment assignment)
        {
            if (assignment == null)
                throw new ArgumentNullException(nameof(assignment), "Assignment cannot be null");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Organize by year/month folders
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                Directory.CreateDirectory(folderPath);

                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(assignment, options);
                await File.WriteAllTextAsync(filePath, json);

                // Invalidate cache after save
                InvalidateCache();
                
                stopwatch.Stop();
                Logger.LogPerformance($"SaveAssignmentAsync ({assignment.Tag})", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError($"SaveAssignmentAsync failed: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all assignments from cache or disk, ordered by most recent first.
        /// Results are cached for 30 seconds to improve performance on repeated calls.
        /// </summary>
        /// <param name="forceRefresh">If true, bypasses cache and reloads from disk</param>
        /// <returns>List of all assignments ordered by timestamp descending</returns>
        public static List<PersistentAssignment> GetAllAssignments(bool forceRefresh = false)
        {
            lock (_cacheLock)
            {
                // Return cached data if valid
                if (!forceRefresh && _cachedAssignments != null &&
                    (DateTime.Now - _cacheTime).TotalMilliseconds < CACHE_DURATION_MS)
                {
                    return _cachedAssignments;
                }

                // Load from disk
                var assignments = new List<PersistentAssignment>();

                try
                {
                    if (!Directory.Exists(HistoryFolder))
                        return assignments;

                    foreach (var folder in Directory.GetDirectories(HistoryFolder))
                    {
                        foreach (var file in Directory.GetFiles(folder, "*.json"))
                        {
                            try
                            {
                                string json = File.ReadAllText(file);
                                var assignment = JsonSerializer.Deserialize<PersistentAssignment>(json);
                                if (assignment != null)
                                    assignments.Add(assignment);
                            }
                            catch (Exception ex)
                            {
                                LogError($"Failed to deserialize {file}: {ex.Message}", ex);
                            }
                        }
                    }

                    assignments = assignments.OrderByDescending(a => a.Timestamp).ToList();
                }
                catch (Exception ex)
                {
                    LogError($"GetAllAssignments failed: {ex.Message}", ex);
                }


                // Update cache
                _cachedAssignments = assignments;
                _cacheTime = DateTime.Now;

                return assignments;
            }
        }

        /// <summary>
        /// Async version of GetAllAssignments - performs non-blocking file I/O.
        /// Uses same caching mechanism as sync version.
        /// Recommended for UI operations to prevent blocking the UI thread.
        /// </summary>
        /// <param name="forceRefresh">If true, bypasses cache and reloads from disk</param>
        /// <returns>Task containing list of all assignments ordered by timestamp descending</returns>
        public static async Task<List<PersistentAssignment>> GetAllAssignmentsAsync(bool forceRefresh = false)
        {
            var stopwatch = Stopwatch.StartNew();
            lock (_cacheLock)
            {
                // Return cached data if valid
                if (!forceRefresh && _cachedAssignments != null &&
                    (DateTime.Now - _cacheTime).TotalMilliseconds < CACHE_DURATION_MS)
                {
                    stopwatch.Stop();
                    Logger.LogPerformance("GetAllAssignmentsAsync (cached)", stopwatch.ElapsedMilliseconds, _cachedAssignments.Count);
                    return _cachedAssignments;
                }
            }

            // Load from disk asynchronously (outside lock to prevent blocking)
            var assignments = new List<PersistentAssignment>();

            try
            {
                if (!Directory.Exists(HistoryFolder))
                    return assignments;

                foreach (var folder in Directory.GetDirectories(HistoryFolder))
                {
                    foreach (var file in Directory.GetFiles(folder, "*.json"))
                    {
                        try
                        {
                            string json = await File.ReadAllTextAsync(file);
                            var assignment = JsonSerializer.Deserialize<PersistentAssignment>(json);
                            if (assignment != null)
                                assignments.Add(assignment);
                        }
                        catch (Exception ex)
                        {
                            LogError($"Failed to deserialize {file}: {ex.Message}", ex);
                        }
                    }
                }

                assignments = assignments.OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                LogError($"GetAllAssignmentsAsync failed: {ex.Message}", ex);
            }

            // Update cache (thread-safe)
            lock (_cacheLock)
            {
                _cachedAssignments = assignments;
                _cacheTime = DateTime.Now;
            }

            stopwatch.Stop();
            Logger.LogPerformance("GetAllAssignmentsAsync (disk)", stopwatch.ElapsedMilliseconds, assignments.Count);
            return assignments;
        }

        public static List<PersistentAssignment> GetAssignmentsByDateRange(DateTime start, DateTime end)
        {
            return GetAssignments(a => a.Timestamp >= start && a.Timestamp <= end);
        }

        /// <summary>
        /// Async version of GetAssignmentsByDateRange
        /// </summary>
        public static async Task<List<PersistentAssignment>> GetAssignmentsByDateRangeAsync(DateTime start, DateTime end)
        {
            var assignments = await GetAssignmentsAsync(a => a.Timestamp >= start && a.Timestamp <= end);
            return assignments;
        }

        public static List<PersistentAssignment> GetAssignmentsByTag(string tag)
        {
            return GetAssignments(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Async version of GetAssignmentsByTag
        /// </summary>
        public static async Task<List<PersistentAssignment>> GetAssignmentsByTagAsync(string tag)
        {
            return await GetAssignmentsAsync(a => a.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Generic method to retrieve assignments with optional filtering predicate.
        /// Consolidates GetAssignmentsByDateRange, GetAssignmentsByTag, and SearchAssignments functionality.
        /// </summary>
        /// <param name="predicate">Optional filter predicate. If null, returns all assignments.</param>
        /// <returns>Filtered list of assignments</returns>
        /// <example>
        /// <code>
        /// // Get assignments from specific date range
        /// var items = GetAssignments(a => a.Timestamp >= start && a.Timestamp <= end);
        /// 
        /// // Get assignments by tag
        /// var tagged = GetAssignments(a => a.Tag == "Cooks");
        /// 
        /// // Get all assignments
        /// var all = GetAssignments();
        /// </code>
        /// </example>
        public static List<PersistentAssignment> GetAssignments(Func<PersistentAssignment, bool>? predicate = null)
        {
            var all = GetAllAssignments();
            return predicate != null ? all.Where(predicate).ToList() : all;
        }

        /// <summary>
        /// Async version of GetAssignments - performs non-blocking filtering
        /// </summary>
        /// <param name="predicate">Optional filter predicate. If null, returns all assignments.</param>
        /// <returns>Task containing filtered list of assignments</returns>
        public static async Task<List<PersistentAssignment>> GetAssignmentsAsync(Func<PersistentAssignment, bool>? predicate = null)
        {
            var all = await GetAllAssignmentsAsync();
            return predicate != null ? all.Where(predicate).ToList() : all;
        }

        public static List<PersistentAssignment> SearchAssignments(string searchTerm)
        {
            return GetAssignments(a =>
                a.Tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.Assignments.Any(p => p.Person.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Async version of SearchAssignments
        /// </summary>
        public static async Task<List<PersistentAssignment>> SearchAssignmentsAsync(string searchTerm)
        {
            return await GetAssignmentsAsync(a =>
                a.Tag.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.Assignments.Any(p => p.Person.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        public static Dictionary<string, int> GetPersonTaskCount(string personName, DateTime start, DateTime end)
        {
            var taskCounts = new Dictionary<string, int>();
            var assignments = GetAssignmentsByDateRange(start, end);

            foreach (var assignment in assignments)
            {
                var personAssignment = assignment.Assignments
                    .FirstOrDefault(a => a.Person.Equals(personName, StringComparison.OrdinalIgnoreCase));

                if (personAssignment != null)
                {
                    var tasks = personAssignment.Tasks.Split(new[] { ", " }, StringSplitOptions.None);
                    foreach (var task in tasks)
                    {
                        if (taskCounts.ContainsKey(task))
                            taskCounts[task]++;
                        else
                            taskCounts[task] = 1;
                    }
                }
            }

            return taskCounts;
        }


        /// <summary>
        /// Deletes a single assignment by ID and invalidates the cache.
        /// Permanently removes the assignment file from disk.
        /// </summary>
        /// <param name="id">The unique ID of the assignment to delete. Must not be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown when id is null or empty</exception>
        /// <exception cref="IOException">Thrown when file deletion fails</exception>
        public static void DeleteAssignment(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id), "Assignment ID cannot be null or empty");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var assignment = GetAllAssignments().FirstOrDefault(a => a?.Id == id);
                if (assignment != null)
                {
                    string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                    string folderPath = Path.Combine(HistoryFolder, yearMonth);
                    string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                    string filePath = Path.Combine(folderPath, fileName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        InvalidateCache();
                    }
                }
                
                stopwatch.Stop();
                Logger.LogPerformance($"DeleteAssignment ({id})", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError($"DeleteAssignment failed for id {id}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Async version of DeleteAssignment
        /// </summary>
        public static async Task DeleteAssignmentAsync(string id)
        {
            try
            {
                var assignments = await GetAllAssignmentsAsync();
                var assignment = assignments.FirstOrDefault(a => a.Id == id);
                if (assignment != null)
                {
                    string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                    string folderPath = Path.Combine(HistoryFolder, yearMonth);
                    string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                    string filePath = Path.Combine(folderPath, fileName);

                    if (File.Exists(filePath))
                    {
                        await Task.Run(() => File.Delete(filePath));
                        InvalidateCache();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"DeleteAssignmentAsync failed for id {id}: {ex.Message}", ex);
                throw;
            }
        }

        public static void DeleteMultipleAssignments(List<string> ids)
        {
            foreach (var id in ids)
            {
                DeleteAssignment(id);
            }
        }

        /// <summary>
        /// Async version of DeleteMultipleAssignments
        /// </summary>
        public static async Task DeleteMultipleAssignmentsAsync(List<string> ids)
        {
            foreach (var id in ids)
            {
                await DeleteAssignmentAsync(id);
            }
        }

        public static void DeleteAssignmentsByDateRange(DateTime startDate, DateTime endDate)
        {
            var assignmentsToDelete = GetAssignmentsByDateRange(startDate, endDate);
            var ids = assignmentsToDelete.Select(a => a.Id).ToList();
            DeleteMultipleAssignments(ids);
        }

        /// <summary>
        /// Retrieves all unique tags from assignments, sorted alphabetically.
        /// Useful for populating filter dropdowns. Filters out null or whitespace tags.
        /// </summary>
        /// <returns>Sorted list of unique tag strings</returns>
        public static List<string> GetAllTags()
        {
            return GetAllAssignments()
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Tag))
                .Select(a => a.Tag)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        /// <summary>
        /// Updates completion status for an assignment and saves it back to file
        /// </summary>
        public static void UpdateAssignmentCompletion(PersistentAssignment assignment)
        {
            if (assignment == null) return;

            try
            {
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                // Update the timestamp to track when completion was last updated
                assignment.CompletionUpdatedAt = DateTime.Now;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(assignment, options);
                File.WriteAllText(filePath, json);

                InvalidateCache();
            }
            catch (Exception ex)
            {
                LogError($"UpdateAssignmentCompletion failed: {ex.Message}", ex);
                throw new Exception($"Failed to update assignment completion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Async version of UpdateAssignmentCompletion - non-blocking file I/O
        /// </summary>
        public static async Task UpdateAssignmentCompletionAsync(PersistentAssignment assignment)
        {
            if (assignment == null) return;

            try
            {
                string yearMonth = assignment.Timestamp.ToString("yyyy-MM");
                string folderPath = Path.Combine(HistoryFolder, yearMonth);
                string fileName = $"{assignment.Timestamp:yyyyMMdd_HHmmss}_{assignment.Tag}.json";
                string filePath = Path.Combine(folderPath, fileName);

                // Update the timestamp to track when completion was last updated
                assignment.CompletionUpdatedAt = DateTime.Now;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(assignment, options);
                await File.WriteAllTextAsync(filePath, json);

                InvalidateCache();
            }
            catch (Exception ex)
            {
                LogError($"UpdateAssignmentCompletionAsync failed: {ex.Message}", ex);
                throw new Exception($"Failed to update assignment completion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get completion statistics for all assignments
        /// </summary>
        public static Dictionary<string, object> GetCompletionStatistics()
        {
            var allAssignments = GetAllAssignments();
            
            return new Dictionary<string, object>
            {
                ["TotalAssignments"] = allAssignments.Count,
                ["CompleteAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage >= 100),
                ["PartialAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage > 0 && a.OverallCompletionPercentage < 100),
                ["IncompleteAssignments"] = allAssignments.Count(a => a.OverallCompletionPercentage == 0),
                ["AverageCompletion"] = allAssignments.Count > 0 ? allAssignments.Average(a => a.OverallCompletionPercentage) : 0
            };
        }

        private static void LogError(string message, Exception ex)
        {
            Logger.LogError(message, ex);
        }
    }
}