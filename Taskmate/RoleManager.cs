using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Taskmate
{
    public static class RoleManager
    {
        private static readonly string RolesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskAssigner", "roles.json");

        private static List<string> cachedRoles = GetDefaultRoles();

        static RoleManager()
        {
            LoadRoles();
        }

        public static List<string> GetAllRoles()
        {
            return new List<string>(cachedRoles);
        }

        public static void AddRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            role = role.Trim();
            
            if (!cachedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                cachedRoles.Add(role);
                SaveRoles();
            }
        }

        private static void LoadRoles()
        {
            try
            {
                if (File.Exists(RolesFilePath))
                {
                    string json = File.ReadAllText(RolesFilePath);
                    var loadedRoles = JsonSerializer.Deserialize<List<string>>(json);
                    if (loadedRoles != null && loadedRoles.Count > 0)
                    {
                        cachedRoles = loadedRoles;
                    }
                    else
                    {
                        cachedRoles = GetDefaultRoles();
                        SaveRoles();
                    }
                }
                else
                {
                    cachedRoles = GetDefaultRoles();
                    SaveRoles();
                }
            }
            catch
            {
                cachedRoles = GetDefaultRoles();
            }
        }

        private static void SaveRoles()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RolesFilePath)!);
                string json = JsonSerializer.Serialize(cachedRoles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RolesFilePath, json);
            }
            catch { }
        }

        private static List<string> GetDefaultRoles()
        {
            return new List<string>
            {
                "General",
                "Server",
                "Cook",
                "Host",
                "Busser",
                "Dishwasher",
                "Manager"
            };
        }

        public static void EnsureDefaultRoles()
        {
            var defaults = GetDefaultRoles();
            foreach (var role in defaults)
            {
                AddRole(role);
            }
        }
    }
}