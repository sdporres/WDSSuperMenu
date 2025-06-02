using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WDSSuperMenu.Core;

namespace WDSSuperMenu
{
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }

    public static class UpdateChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string UPDATE_CHECK_URL = "https://api.github.com/repos/sdporres/WDSSuperMenu/releases/latest";

        static UpdateChecker()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WDS-Super-Menu-UpdateChecker");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                Logger.LogToFile("Checking for updates...");

                var response = await httpClient.GetStringAsync(UPDATE_CHECK_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null)
                {
                    Logger.LogToFile("Failed to parse update response");
                    return new UpdateInfo { IsUpdateAvailable = false };
                }

                var currentVersion = ParseVersion(GetCurrentVersion());
                var latestVersion = ParseVersion(release.tag_name);

                bool isUpdateAvailable = IsNewerVersion(latestVersion, currentVersion);

                Logger.LogToFile($"Current version: {currentVersion}, Latest version: {latestVersion}, Update available: {isUpdateAvailable}");

                return new UpdateInfo
                {
                    Version = release.tag_name,
                    DownloadUrl = GetWindowsDownloadUrl(release),
                    ReleaseNotes = release.body ?? "No release notes available.",
                    ReleaseDate = release.published_at,
                    IsUpdateAvailable = isUpdateAvailable
                };
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error checking for updates: {ex}");
                return new UpdateInfo { IsUpdateAvailable = false };
            }
        }

        public static string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = $"{assembly.GetName().Version.Major}.{assembly.GetName().Version.Minor}.{assembly.GetName().Version.Build}" ?? "1.0.0";
            return version;
        }

        private static Version ParseVersion(string versionString)
        {
            // Remove 'v' prefix if present
            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionString = versionString.Substring(1);

            if (Version.TryParse(versionString, out Version version))
                return version;

            return new Version(0, 0, 0, 0);
        }

        private static bool IsNewerVersion(Version latest, Version current)
        {
            return latest > current;
        }

        private static string GetWindowsDownloadUrl(GitHubRelease release)
        {
            // Look for Windows-specific asset (e.g., .exe, .msi, or .zip)
            foreach (var asset in release.assets ?? Array.Empty<GitHubAsset>())
            {
                if (asset.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    asset.name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                    asset.name.Contains("windows", StringComparison.OrdinalIgnoreCase))
                {
                    return asset.browser_download_url;
                }
            }

            // Fallback to release page
            return release.html_url;
        }

        public static void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    // GitHub API response models
    internal class GitHubRelease
    {
        public string tag_name { get; set; } = string.Empty;
        public string html_url { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
        public DateTime published_at { get; set; }
        public GitHubAsset[]? assets { get; set; }
    }

    internal class GitHubAsset
    {
        public string name { get; set; } = string.Empty;
        public string browser_download_url { get; set; } = string.Empty;
    }
}
