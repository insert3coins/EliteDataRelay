using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EliteDataRelay.Services

{
    public static class UpdateCheckService
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/insert3coins/EliteDataRelay/releases/latest";
        private static bool _updateCheckPerformed = false;

        /// <summary>
        /// Checks for a new version of the application on GitHub.
        /// </summary>
        /// <param name="owner">The window that will own the update notification dialog.</param>
        public static async Task CheckForUpdatesAsync(IWin32Window owner)
        {
            // Only check for updates once per application run.
            if (_updateCheckPerformed)
            {
                return;
            }
            _updateCheckPerformed = true;

            try
            {
                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header.
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EliteDataRelay", "1.0"));

                    var response = await client.GetStringAsync(GITHUB_API_URL);
                    var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                    if (release == null || string.IsNullOrEmpty(release.TagName))
                    {
                        Logger.Verbose("[UpdateCheck] Could not parse release information from GitHub.");
                        return;
                    }

                    // The tag name is expected to be in a format like "v0.31.1"
                    var latestVersionStr = release.TagName.TrimStart('v');
                    if (Version.TryParse(latestVersionStr, out var latestVersion))
                    {
                        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                        if (latestVersion > currentVersion)
                        {
                            var result = MessageBox.Show(owner,
                                $"A new version ({latestVersion}) is available!\n\nWould you like to go to the download page?",
                                "Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);

                            if (result == DialogResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo(release.HtmlUrl) { UseShellExecute = true });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail silently. We don't want to bother the user if the update check fails.
                Logger.Info($"[UpdateCheck] Failed to check for updates: {ex.Message}");
            }
        }

        // A simple class to deserialize the GitHub API response.
        private class GitHubRelease
        {
            [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [System.Text.Json.Serialization.JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}
