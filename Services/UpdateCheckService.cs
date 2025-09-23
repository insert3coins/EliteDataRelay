using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EliteDataRelay.Services
    {
    public static class UpdateCheckService
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/insert3coins/EliteDataRelay/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            Debug.WriteLine("[UpdateCheckService] Checking for updates...");
            try
            {
                using var client = new HttpClient();
                // GitHub API requires a User-Agent header.
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EliteDataRelay", GetAppVersion()));

                var response = await client.GetStringAsync(GITHUB_API_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    return;
                }

                // Remove 'v' prefix from tag name if it exists for correct parsing.
                var latestVersionString = release.TagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? release.TagName.Substring(1)
                    : release.TagName;

                if (Version.TryParse(latestVersionString, out var latestVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (currentVersion != null && latestVersion > currentVersion)
                    {
                        NotifyUserOfUpdate(latestVersion.ToString(), release.HtmlUrl);
                    }
                    Debug.WriteLine("[UpdateCheckService] Update check completed successfully.");
                }
            }
            catch (Exception ex)
            {
                // Fail silently. We don't want to bother the user if the update check fails.
                Debug.WriteLine($"[UpdateCheckService] Failed to check for updates: {ex.Message}");
            }
        }

        private static void NotifyUserOfUpdate(string newVersion, string releaseUrl)
        {
            var message = $"A new version ({newVersion}) of Elite Data Relay is available.\n\nWould you like to open the download page?";
            var result = MessageBox.Show(message, "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[UpdateCheckService] Failed to open URL: {ex.Message}");
                }
            }
        }

        private static string GetAppVersion() => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}