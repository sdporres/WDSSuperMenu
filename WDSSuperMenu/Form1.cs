using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace WDSSuperMenu
{
    public partial class Form1 : Form
    {
        private int pictureBoxSize = 32;
        private readonly Dictionary<string, string> registryIconCache;
        public Form1()
        {
            InitializeComponent();
            registryIconCache = BuildRegistryIconCache();
            ScanForWDSFolders();
        }

        private Dictionary<string, string> BuildRegistryIconCache()
        {
            var cache = new Dictionary<string, string>();
            try
            {
                using (RegistryKey productsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Installer\Products"))
                {
                    if (productsKey == null)
                    {
                        LogError("Failed to open registry key HKLM\\SOFTWARE\\Classes\\Installer\\Products");
                        return cache;
                    }

                    foreach (string productSubKeyName in productsKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey productKey = productsKey.OpenSubKey(productSubKeyName))
                            {
                                if (productKey == null)
                                    continue;

                                string productName = productKey.GetValue("ProductName")?.ToString();
                                string productIcon = productKey.GetValue("ProductIcon")?.ToString();

                                if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(productIcon) ||
                                    !File.Exists(productIcon) ||
                                    !string.Equals(Path.GetExtension(productIcon), ".exe", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                cache[productName.ToLowerInvariant()] = productIcon;
                                LogError($"Cached icon for ProductName: {productName}, ProductIcon: {productIcon}");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Failed to process registry subkey {productSubKeyName}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to build registry icon cache: {ex}");
            }
            return cache;
        }

        private void ScanForWDSFolders()
        {
            string folderName = "WDS";
            flowLayoutPanel.SuspendLayout();
            int maxGroupPanelWidth = 0;
            int totalHeight = 0;

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                            continue;

                        string wdsPath = Path.Combine(drive.RootDirectory.FullName, folderName);
                        if (!Directory.Exists(wdsPath))
                            continue;

                        string[] subdirectories = Directory.GetDirectories(wdsPath);
                        foreach (string subdir in subdirectories)
                        {
                            string savesPath = Path.Combine(subdir, "saves");
                            string manualsPath = Path.Combine(subdir, "manuals");
                            string[] exeFiles = Directory.GetFiles(subdir, "*.exe", SearchOption.TopDirectoryOnly);

                            // Get icon for the group panel from cache
                            Icon groupIcon = GetIconFromRegistry(Path.GetFileName(subdir));

                            // Create UI panel for this subdirectory
                            var groupPanel = new FlowLayoutPanel
                            {
                                FlowDirection = FlowDirection.LeftToRight,
                                AutoSize = true,
                                WrapContents = false,
                                Margin = new Padding(6)
                            };

                            // Add icon or placeholder and label to groupPanel
                            int iconSize = groupIcon != null && groupIcon.Width > 32 ? 64 : 32; // Fallback to 32x32 if icon is small or missing
                            var pictureBox = new PictureBox
                            {
                                Size = new Size(iconSize, iconSize),
                                Margin = new Padding(0, 3, 8, 3) // Consistent margin for alignment
                            };

                            if (groupIcon != null)
                            {
                                try
                                {
                                    pictureBox.Image = groupIcon.ToBitmap().GetThumbnailImage(iconSize, iconSize, null, IntPtr.Zero);
                                    LogError($"Group icon for {Path.GetFileName(subdir)} set to {iconSize}x{iconSize}, native size: {groupIcon.Width}x{groupIcon.Height}");
                                }
                                catch (Exception ex)
                                {
                                    LogError($"Failed to set group icon for {Path.GetFileName(subdir)}: {ex}");
                                }
                            }
                            else
                            {
                                LogError($"No icon found for {Path.GetFileName(subdir)}, using placeholder {iconSize}x{iconSize}");
                            }

                            groupPanel.Controls.Add(pictureBox);

                            groupPanel.Controls.Add(new Label
                            {
                                Text = Path.GetFileName(subdir),
                                Width = 200,
                                AutoSize = false,
                                Margin = new Padding(3)
                            });

                            int entriesAdded = 0;

                            if (Directory.Exists(savesPath))
                            {
                                groupPanel.Controls.Add(BuildButton("> Saves", savesPath, null, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag)));
                                entriesAdded++;
                            }

                            if (Directory.Exists(manualsPath))
                            {
                                groupPanel.Controls.Add(BuildButton("> Manuals", manualsPath, null, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag)));
                                entriesAdded++;
                            }

                            foreach (var exePath in exeFiles)
                            {
                                string exeName = Path.GetFileName(exePath).ToLowerInvariant();
                                var appName = BuildAppName(exeName);
                                if (string.IsNullOrEmpty(appName))
                                    continue;

                                // Get icon from the .exe itself
                                Icon exeIcon = null;
                                try
                                {
                                    if (File.Exists(exePath))
                                        exeIcon = Icon.ExtractAssociatedIcon(exePath);
                                }
                                catch (Exception ex)
                                {
                                    LogError($"Failed to extract icon from {exePath}: {ex}");
                                }

                                groupPanel.Controls.Add(BuildButton(appName, exePath, exeIcon, (s, e) =>
                                    LaunchApplication((string)((Button)s).Tag)));
                                entriesAdded++;
                            }

                            if (entriesAdded > 0)
                            {
                                flowLayoutPanel.Controls.Add(groupPanel);
                                groupPanel.PerformLayout();
                                maxGroupPanelWidth = Math.Max(maxGroupPanelWidth, groupPanel.PreferredSize.Width);
                                totalHeight += groupPanel.PreferredSize.Height;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing drive {drive.Name}: {ex}");
                        continue;
                    }
                }

                this.Width = Math.Min(maxGroupPanelWidth + 50, Screen.PrimaryScreen.WorkingArea.Width - 100);
                this.Height = Math.Min(totalHeight + 100, Screen.PrimaryScreen.WorkingArea.Height - 100);
            }
            finally
            {
                flowLayoutPanel.ResumeLayout(true);
            }
        }

        private Icon GetIconFromRegistry(string subdirName)
        {
            try
            {
                string subdirLower = subdirName.ToLowerInvariant();
                foreach (var entry in registryIconCache)
                {
                    if (entry.Key.Contains(subdirLower))
                    {
                        string productIcon = entry.Value;
                        return Icon.ExtractAssociatedIcon(productIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to retrieve icon for {subdirName}: {ex}");
            }
            return null; // No icon found
        }

        private static Button BuildButton(string text, string tag, Icon icon, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Tag = tag,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(4),
                MinimumSize = new Size(100, 30),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                ImageAlign = ContentAlignment.MiddleLeft
            };

            if (icon != null)
            {
                try
                {
                    // Convert Icon to Bitmap for the button (16x16 for consistency)
                    button.Image = icon.ToBitmap().GetThumbnailImage(16, 16, null, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to set icon for button {text}: {ex}");
                }
            }

            button.Click += onClick;
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(button, $"Open {text} at {tag}");
            return button;
        }

        private static readonly Dictionary<string, string> ExeNameMappings = new()
        {
            { "camp", "Campaign Editor" },
            { "edit", "Scenario Editor" },
            { "start", "Campaign Game" }
        };

        private static string BuildAppName(string exeName)
        {
            exeName = exeName.ToLowerInvariant();
            if (exeName.Contains("demo") || Path.GetFileNameWithoutExtension(exeName).Length <= 4)
                return "Scenario Game";

            foreach (var mapping in ExeNameMappings)
            {
                if (exeName.Contains(mapping.Key))
                    return mapping.Value;
            }
            return string.Empty;
        }

        private void LaunchApplication(string filePath)
        {
            try
            {
                // Verify file exists and is likely an executable
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"EXE not found at: {filePath}");
                    return;
                }

                // Ensure the file has a .exe extension
                if (!string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"File at {filePath} is not an executable (.exe).");
                    return;
                }

                // Set up the process start info
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty, // Set to the executable's directory
                    UseShellExecute = true, // Use shell for compatibility
                                            // Optionally add: Arguments = "", if specific arguments are needed
                };

                // Attempt to start the process
                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        MessageBox.Show($"Failed to start process for {filePath}: No process was created.");
                        LogError($"No process created for {filePath}");
                        return;
                    }

                    // Optionally wait for a short time to detect immediate crashes
                    process.WaitForExit(1000); // Wait up to 1 second
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        MessageBox.Show($"Application {filePath} exited immediately with code {process.ExitCode}.");
                        LogError($"Application {filePath} exited with code {process.ExitCode}");
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 740) // ERROR_ELEVATION_REQUIRED
            {
                // Handle case where elevation is required
                DialogResult result = MessageBox.Show(
                    $"The application {filePath} requires administrative privileges. Retry with elevation?",
                    "Elevation Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        var psiElevated = new ProcessStartInfo
                        {
                            FileName = filePath,
                            WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty,
                            UseShellExecute = true,
                            Verb = "runas" // Request elevation
                        };
                        Process.Start(psiElevated);
                    }
                    catch (Exception exElevated)
                    {
                        MessageBox.Show($"Failed to launch {filePath} with elevation: {exElevated.Message}");
                        LogError($"Elevation failed for {filePath}: {exElevated}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch '{filePath}':\n{ex.Message}\nDetails: {ex}");
                LogError($"Failed to launch {filePath}: {ex}");
            }
        }

        private static void LogError(string message)
        {
            try
            {
                File.AppendAllText("error.log", $"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Suppress logging errors to avoid secondary failures
            }
        }
    }
}
