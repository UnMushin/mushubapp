using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Mushub.Services
{
    public static class UpdateService
    {
        private const string RepoOwner = "UnMushin";
        private const string RepoName = "mushubapp";

        public static async Task<(bool HasUpdate, string LatestVersion)> CheckForUpdateAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mushub", "1.0"));

            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var release = JObject.Parse(json);

            var tagName = release["tag_name"]?.ToString() ?? "v0.0.0";
            var latestVersion = tagName.TrimStart('v');

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var currentStr = $"{currentVersion?.Major}.{currentVersion?.Minor}.{currentVersion?.Build}";

            var hasUpdate = CompareVersions(latestVersion, currentStr) > 0;
            return (hasUpdate, tagName);
        }

        private static int CompareVersions(string v1, string v2)
        {
            try
            {
                var ver1 = new Version(v1);
                var ver2 = new Version(v2);
                return ver1.CompareTo(ver2);
            }
            catch { return 0; }
        }
    }
}
