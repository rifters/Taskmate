using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class TagManager
    {
        private static readonly string TagsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "tags.json");

        private static List<string> cachedTags = GetDefaultTags();

        static TagManager()
        {
            LoadTags();
        }

        public static List<string> GetAllTags()
        {
            return new List<string>(cachedTags);
        }

        public static void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            tag = tag.Trim();
            
            if (!cachedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                cachedTags.Add(tag);
                SaveTags();
            }
        }

        public static void RemoveTag(string tag)
        {
            var existingTag = cachedTags.FirstOrDefault(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (existingTag != null)
            {
                cachedTags.Remove(existingTag);
                SaveTags();
            }
        }

        public static void RenameTag(string oldTag, string newTag)
        {
            if (string.IsNullOrWhiteSpace(newTag))
                return;

            newTag = newTag.Trim();
            var existingTag = cachedTags.FirstOrDefault(t => t.Equals(oldTag, StringComparison.OrdinalIgnoreCase));
            
            if (existingTag != null && !cachedTags.Contains(newTag, StringComparer.OrdinalIgnoreCase))
            {
                int index = cachedTags.IndexOf(existingTag);
                cachedTags[index] = newTag;
                SaveTags();
                
                // Update existing assignments with this tag
                UpdateAssignmentTags(oldTag, newTag);
            }
        }

        private static void UpdateAssignmentTags(string oldTag, string newTag)
        {
            try
            {
                var assignments = AssignmentHistoryManager.GetAllAssignments();
                foreach (var assignment in assignments.Where(a => a.Tag.Equals(oldTag, StringComparison.OrdinalIgnoreCase)))
                {
                    assignment.Tag = newTag;
                    // Re-save the assignment
                    AssignmentHistoryManager.DeleteAssignment(assignment.Id);
                    AssignmentHistoryManager.SaveAssignment(assignment);
                }
            }
            catch
            {
                // Don't fail tag rename if assignment update fails
            }
        }

        private static void LoadTags()
        {
            try
            {
                if (File.Exists(TagsFilePath))
                {
                    string json = File.ReadAllText(TagsFilePath);
                    var loadedTags = JsonSerializer.Deserialize<List<string>>(json);
                    if (loadedTags != null && loadedTags.Count > 0)
                    {
                        cachedTags = loadedTags;
                    }
                    else
                    {
                        cachedTags = GetDefaultTags();
                        SaveTags();
                    }
                }
                else
                {
                    cachedTags = GetDefaultTags();
                    SaveTags();
                }
            }
            catch
            {
                cachedTags = GetDefaultTags();
            }
        }

        private static void SaveTags()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TagsFilePath)!);
                string json = JsonSerializer.Serialize(cachedTags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(TagsFilePath, json);
            }
            catch
            {
                // Fail silently
            }
        }

        private static List<string> GetDefaultTags()
        {
            return new List<string>
            {
                "General",
                "Morning Shift",
                "Evening Shift",
                "Night Shift",
                "Weekend",
                "Weekday",
                "Special Event"
            };
        }

        public static void EnsureDefaultTags()
        {
            var defaults = GetDefaultTags();
            foreach (var tag in defaults)
            {
                AddTag(tag);
            }
        }
    }
}