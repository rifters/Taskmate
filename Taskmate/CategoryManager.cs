using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class CategoryManager
    {
        private static readonly string CategoriesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "categories.json");

        private static List<string> cachedCategories = GetDefaultCategories();

        public static List<string> GetAllCategories()
        {
            if (cachedCategories == null || cachedCategories.Count == 0)
                LoadCategories();
            
            // FIX: Add null coalescing to prevent warning
            return new List<string>(cachedCategories ?? GetDefaultCategories());
        }

        public static void AddCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return;

            category = category.Trim();
            
            if (!cachedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                cachedCategories.Add(category);
                SaveCategories();
            }
        }

        public static void RemoveCategory(string category)
        {
            var existing = cachedCategories.FirstOrDefault(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                cachedCategories.Remove(existing);
                SaveCategories();
            }
        }

        private static void LoadCategories()
        {
            try
            {
                if (File.Exists(CategoriesPath))
                {
                    string json = File.ReadAllText(CategoriesPath);
                    var loaded = JsonSerializer.Deserialize<List<string>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        cachedCategories = loaded;
                    }
                    else
                    {
                        cachedCategories = GetDefaultCategories();
                        SaveCategories();
                    }
                }
                else
                {
                    cachedCategories = GetDefaultCategories();
                    SaveCategories();
                }
            }
            catch
            {
                cachedCategories = GetDefaultCategories();
            }
        }

        private static void SaveCategories()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CategoriesPath)!);
                string json = JsonSerializer.Serialize(cachedCategories, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CategoriesPath, json);
            }
            catch { }
        }

        private static List<string> GetDefaultCategories()
        {
            return new List<string>
            {
                "General",
                "Cleaning",
                "Cooking",
                "Customer Service",
                "Opening Tasks",
                "Closing Tasks",
                "Maintenance",
                "Restocking"
            };
        }

        public static void EnsureDefaultCategories()
        {
            var defaults = GetDefaultCategories();
            foreach (var category in defaults)
            {
                AddCategory(category);
            }
        }
    }
}