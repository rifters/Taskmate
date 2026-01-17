using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class TemplateManager
    {
        private static readonly string TemplatesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "Templates");

        static TemplateManager()
        {
            Directory.CreateDirectory(TemplatesFolder);
        }

        public static void SaveTemplate(AssignmentTemplate template)
        {
            try
            {
                string fileName = $"{template.Id}.json";
                string filePath = Path.Combine(TemplatesFolder, fileName);
                string json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Fail silently
            }
        }

        public static List<AssignmentTemplate> GetAllTemplates()
        {
            var templates = new List<AssignmentTemplate>();
            
            try
            {
                var files = Directory.GetFiles(TemplatesFolder, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var template = JsonSerializer.Deserialize<AssignmentTemplate>(json);
                        if (template != null)
                            templates.Add(template);
                    }
                    catch { }
                }
            }
            catch { }

            return templates.OrderByDescending(t => t.Created).ToList();
        }

        public static void DeleteTemplate(string templateId)
        {
            try
            {
                string fileName = $"{templateId}.json";
                string filePath = Path.Combine(TemplatesFolder, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch { }
        }

        public static AssignmentTemplate? GetTemplate(string templateId)
        {
            try
            {
                string fileName = $"{templateId}.json";
                string filePath = Path.Combine(TemplatesFolder, fileName);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<AssignmentTemplate>(json);
                }
            }
            catch { }
            
            return null;
        }
    }
}