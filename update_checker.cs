using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.Windows.Forms;
using WDSSuperMenu.Core;

namespace WDSSuperMenu.Core
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
        private const string UPDATE_CHECK_URL = "https://api.github.com/repos/yourusername/wds-super-menu/releases/latest";
        // Alternative: Use your own server endpoint
        // private const string UPDATE_CHECK_URL = "https://yourserver.com/api/wds-super-menu/latest";

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

                var currentVersion = GetCurrentVersion();
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

        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version ?? new Version(1, 0, 0, 0);
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

namespace WDSSuperMenu
{
    public partial class UpdateNotificationForm : Form
    {
        private readonly UpdateInfo updateInfo;
        private Label titleLabel;
        private Label versionLabel;
        private Label releaseDateLabel;
        private TextBox releaseNotesTextBox;
        private Button downloadButton;
        private Button remindLaterButton;
        private Button skipVersionButton;
        private CheckBox autoCheckCheckBox;

        public UpdateNotificationForm(UpdateInfo updateInfo)
        {
            this.updateInfo = updateInfo;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 400);
            this.Text = "Update Available";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Information;

            // Title
            titleLabel = new Label
            {
                Text = "A new version of WDS Super Menu is available!",
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(450, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Version info
            versionLabel = new Label
            {
                Text = $"New Version: {updateInfo.Version}",
                Location = new Point(20, 55),
                Size = new Size(450, 20)
            };

            releaseDateLabel = new Label
            {
                Text = $"Released: {updateInfo.ReleaseDate:MMM dd, yyyy}",
                Location = new Point(20, 80),
                Size = new Size(450, 20)
            };

            // Release notes
            var notesLabel = new Label
            {
                Text = "Release Notes:",
                Location = new Point(20, 110),
                Size = new Size(100, 20)
            };

            releaseNotesTextBox = new TextBox
            {
                Text = updateInfo.ReleaseNotes,
                Location = new Point(20, 135),
                Size = new Size(450, 150),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            // Auto-check option
            autoCheckCheckBox = new CheckBox
            {
                Text = "Automatically check for updates on startup",
                Location = new Point(20, 300),
                Size = new Size(300, 25),
                Checked = Properties.Settings.Default.AutoCheckUpdates
            };

            // Buttons
            downloadButton = new Button
            {
                Text = "Download Update",
                Location = new Point(190, 330),
                Size = new Size(120, 30),
                DialogResult = DialogResult.OK
            };
            downloadButton.Click += DownloadButton_Click;

            remindLaterButton = new Button
            {
                Text = "Remind Later",
                Location = new Point(320, 330),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            skipVersionButton = new Button
            {
                Text = "Skip Version",
                Location = new Point(410, 330),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Ignore
            };
            skipVersionButton.Click += SkipVersionButton_Click;

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                titleLabel, versionLabel, releaseDateLabel, notesLabel,
                releaseNotesTextBox, autoCheckCheckBox, downloadButton,
                remindLaterButton, skipVersionButton
            });
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = updateInfo.DownloadUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open download link: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SkipVersionButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SkippedVersion = updateInfo.Version;
            Properties.Settings.Default.Save();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.AutoCheckUpdates = autoCheckCheckBox.Checked;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }
    }
}