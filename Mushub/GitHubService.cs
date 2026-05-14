using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Mushub.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mushub.Services
{
    public static class GitHubService
    {
        private static HttpClient _client = new();
        private static string _token = "";
        private static string _gistId = "";
        private const string GistFilename = "mushub_tasks.json";

        public static void Configure(string token, string gistId)
        {
            _token = token;
            _gistId = gistId;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mushub", "1.0"));
            if (!string.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public static async Task<List<TodoItem>> LoadTasksAsync()
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("Token GitHub non configuré. Rendez-vous dans Paramètres.");

            // If no Gist ID, try to find or create one
            if (string.IsNullOrEmpty(_gistId))
            {
                _gistId = await FindOrCreateGistAsync();
                // Save the newly created/found gist ID
                var settings = DataService.LoadSettings();
                settings.GistId = _gistId;
                DataService.SaveSettings(settings);
            }

            var response = await _client.GetAsync($"https://api.github.com/gists/{_gistId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var gist = JObject.Parse(json);

            var fileContent = gist["files"]?[GistFilename]?["content"]?.ToString();
            if (string.IsNullOrEmpty(fileContent))
                return new List<TodoItem>();

            var data = JsonConvert.DeserializeObject<GistData>(fileContent);
            return data?.Tasks ?? new List<TodoItem>();
        }

        public static async Task SaveTasksAsync(List<TodoItem> tasks)
        {
            if (string.IsNullOrEmpty(_token))
                throw new InvalidOperationException("Token GitHub non configuré.");

            if (string.IsNullOrEmpty(_gistId))
                _gistId = await FindOrCreateGistAsync();

            var data = new GistData { Tasks = tasks, LastModified = DateTime.Now };
            var content = JsonConvert.SerializeObject(data, Formatting.Indented);

            var payload = new
            {
                files = new Dictionary<string, object>
                {
                    [GistFilename] = new { content }
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PatchAsync($"https://api.github.com/gists/{_gistId}", httpContent);
            response.EnsureSuccessStatusCode();
        }

        private static async Task<string> FindOrCreateGistAsync()
        {
            // Search existing gists for mushub
            var response = await _client.GetAsync("https://api.github.com/gists?per_page=100");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var gists = JArray.Parse(json);

            foreach (var gist in gists)
            {
                var files = gist["files"] as JObject;
                if (files?.ContainsKey(GistFilename) == true)
                    return gist["id"]?.ToString() ?? "";
            }

            // Create a new gist
            return await CreateGistAsync();
        }

        private static async Task<string> CreateGistAsync()
        {
            var initialData = new GistData { Tasks = new List<TodoItem>(), LastModified = DateTime.Now };
            var content = JsonConvert.SerializeObject(initialData, Formatting.Indented);

            var payload = new
            {
                description = "Mushub — Todo List synchronisée",
                @public = false,
                files = new Dictionary<string, object>
                {
                    [GistFilename] = new { content }
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("https://api.github.com/gists", httpContent);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var gist = JObject.Parse(responseJson);
            return gist["id"]?.ToString() ?? throw new Exception("Impossible de créer le Gist.");
        }
    }
}
