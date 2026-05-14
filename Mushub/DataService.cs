using System;
using System.Collections.Generic;
using System.IO;
using Mushub.Models;
using Newtonsoft.Json;

namespace Mushub.Services
{
    public static class DataService
    {
        private static string _appDataPath = "";
        private static string _tasksFile = "";
        private static string _tilesFile = "";
        private static string _settingsFile = "";

        public static void Initialize()
        {
            _appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mushub");

            Directory.CreateDirectory(_appDataPath);

            _tasksFile = Path.Combine(_appDataPath, "tasks.json");
            _tilesFile = Path.Combine(_appDataPath, "tiles.json");
            _settingsFile = Path.Combine(_appDataPath, "settings.json");

            // Init GitHub service with saved settings
            var settings = LoadSettings();
            if (!string.IsNullOrEmpty(settings.GitHubToken))
                GitHubService.Configure(settings.GitHubToken, settings.GistId ?? "");
        }

        public static List<TodoItem> LoadLocalTasks()
        {
            try
            {
                if (!File.Exists(_tasksFile)) return new List<TodoItem>();
                var json = File.ReadAllText(_tasksFile);
                return JsonConvert.DeserializeObject<List<TodoItem>>(json) ?? new List<TodoItem>();
            }
            catch { return new List<TodoItem>(); }
        }

        public static void SaveLocalTasks(List<TodoItem> tasks)
        {
            try
            {
                var json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
                File.WriteAllText(_tasksFile, json);
            }
            catch { }
        }

        public static List<HubTile> LoadHubTiles()
        {
            try
            {
                if (!File.Exists(_tilesFile)) return new List<HubTile>();
                var json = File.ReadAllText(_tilesFile);
                return JsonConvert.DeserializeObject<List<HubTile>>(json) ?? new List<HubTile>();
            }
            catch { return new List<HubTile>(); }
        }

        public static void SaveHubTiles(List<HubTile> tiles)
        {
            try
            {
                var json = JsonConvert.SerializeObject(tiles, Formatting.Indented);
                File.WriteAllText(_tilesFile, json);
            }
            catch { }
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFile)) return new AppSettings();
                var json = File.ReadAllText(_settingsFile);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsFile, json);
            }
            catch { }
        }
    }
}
