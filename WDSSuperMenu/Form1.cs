using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace WDSSuperMenu
{
    public class RegistryEntry
    {
        public string ProductName { get; set; }
        public string ProductIcon { get; set; }
        public string Version { get; set; }
        public RegistryEntry(string productName, string productIcon, string version)
        {
            ProductName = productName;
            ProductIcon = productIcon;
            Version = version;
        }
    }
    public partial class Form1 : Form
    {
        private int pictureBoxSize = 32;
        private readonly Dictionary<string, RegistryEntry> registryIconCache;
        public Form1()
        {
            InitializeComponent();
            registryIconCache = BuildRegistryIconCache();
            ScanForWDSFolders();
        }

        private Dictionary<string, RegistryEntry> BuildRegistryIconCache()
        {
            var cache = new Dictionary<string, RegistryEntry>();
            try
            {
                using (RegistryKey productsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Installer\Products"))
                {
                    if (productsKey == null)
                    {
                        LogToFile("Failed to open registry key HKLM\\SOFTWARE\\Classes\\Installer\\Products");
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
                                object productVersionObject = productKey.GetValue("Version");
                                string productVersion = string.Empty;
                                if (productVersionObject != null && productVersionObject is int intValue)
                                {
                                    var versionHex = $"{intValue:X}";
                                    productVersion = ParseVersion(versionHex);
                                }

                                if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(productIcon) ||
                                    !File.Exists(productIcon) ||
                                    !string.Equals(Path.GetExtension(productIcon), ".exe", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                cache[productName.ToLowerInvariant()] = new RegistryEntry(productName, productIcon, productVersion);
                                LogToFile($"Cached icon for ProductName: {productName}, ProductIcon: {productIcon}");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Failed to process registry subkey {productSubKeyName}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to build registry icon cache: {ex}");
            }
            return cache;
        }

        public static string ParseVersion(string hexValue)
        {
            // Convert hex string to uint
            uint value = Convert.ToUInt32(hexValue, 16);

            // Extract major, minor, and patch versions
            int major = (int)(value >> 24) & 0xFF; // First byte
            int minor = (int)(value >> 16) & 0xFF; // Second byte
            int patch = (int)(value & 0xFFFF);     // Last two bytes

            // Format as version string
            return $"{major}.{minor:D2}.{patch}";
        }

        private void ScanForWDSFolders()
        {
            string folderName = "WDS";
            flowLayoutPanel.SuspendLayout();
            int maxGroupPanelWidth = 0;
            int totalHeight = 0;

            // Define the desired button order
            var desiredButtonOrder = new List<string> { "Scenario Game", "Campaign Game", "Scenario Editor", "Campaign Editor" };

            // Calculate the maximum button width for EXE buttons, including icon
            int maxButtonWidth = 0;
            foreach (var buttonName in desiredButtonOrder)
            {
                var tempButton = BuildButton(buttonName, "", GetDefaultIcon(), null);
                tempButton.AutoSize = true; // Ensure autosize for measurement
                tempButton.PerformLayout();
                int width = tempButton.PreferredSize.Width;
                maxButtonWidth = Math.Max(maxButtonWidth, width);
                LogToFile($"Calculated width for temp button '{buttonName}': {width}px");
            }
            LogToFile($"Maximum button width for EXE buttons: {maxButtonWidth}px");

            try
            {
                var werwe = RegistryAppFinder.FindUniqueParentDirectories();
                foreach (var rootTHings in werwe)
                {
                    try
                    {
                        string[] subdirectories = Directory.GetDirectories(rootTHings);
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
                                Margin = new Padding(20, 6, 6, 6)
                            };

                            // Add icon or placeholder and label to groupPanel
                            int iconSize = groupIcon != null && groupIcon.Width > 32 ? 64 : 32;
                            var pictureBox = new PictureBox
                            {
                                Size = new Size(iconSize, iconSize),
                                Margin = new Padding(0, 3, 8, 3)
                            };

                            if (groupIcon != null)
                            {
                                try
                                {
                                    pictureBox.Image = groupIcon.ToBitmap().GetThumbnailImage(iconSize, iconSize, null, IntPtr.Zero);
                                    LogToFile($"Group icon for {Path.GetFileName(subdir)} set to {iconSize}x{iconSize}, native size: {groupIcon.Width}x{groupIcon.Height}");
                                }
                                catch (Exception ex)
                                {
                                    LogToFile($"Failed to set group icon for {Path.GetFileName(subdir)}: {ex}");
                                }
                            }
                            else
                            {
                                LogToFile($"No icon found for {Path.GetFileName(subdir)}, using placeholder {iconSize}x{iconSize}");
                            }

                            groupPanel.Controls.Add(pictureBox);

                            var subDirName = Path.GetFileName(subdir);
                            var versionLabel = GetVersionFromRegistry(subDirName);
                            groupPanel.Controls.Add(new Label
                            {
                                Text = $"{subDirName}{(string.IsNullOrEmpty(versionLabel) ? "" : $" ({GetVersionFromRegistry(subDirName)})")}",
                                Width = 250,
                                AutoSize = false,
                                Margin = new Padding(3)
                            });

                            // Collect all buttons for this group
                            var buttons = new List<(string Name, Control Control)>();

                            // Add Saves and Manuals buttons (unchanged width)
                            if (Directory.Exists(savesPath))
                            {
                                buttons.Add(("Saves", BuildButton("> Saves", savesPath, null, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag))));
                            }

                            if (Directory.Exists(manualsPath))
                            {
                                buttons.Add(("Manuals", BuildButton("> Manuals", manualsPath, null, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag))));
                            }

                            // Add EXE buttons with fixed width
                            foreach (var exePath in exeFiles)
                            {
                                string exeName = Path.GetFileName(exePath).ToLowerInvariant();
                                var appName = BuildAppName(exeName);
                                if (string.IsNullOrEmpty(appName))
                                    continue;

                                Icon exeIcon = null;
                                try
                                {
                                    if (File.Exists(exePath))
                                        exeIcon = Icon.ExtractAssociatedIcon(exePath);
                                }
                                catch (Exception ex)
                                {
                                    LogToFile($"Failed to extract icon from {exePath}: {ex}");
                                }

                                var button = BuildButton(appName, exePath, exeIcon, (s, e) =>
                                    LaunchApplication((string)((Button)s).Tag));
                                button.AutoSize = false; // Disable autosize for fixed width
                                button.Size = new Size(maxButtonWidth, 30); // Set consistent width
                                buttons.Add((appName, button));
                                LogToFile($"Created button '{appName}' with width: {button.Width}px");
                            }

                            // Add Saves and Manuals buttons first
                            foreach (var button in buttons.Where(b => b.Name == "Saves" || b.Name == "Manuals"))
                            {
                                groupPanel.Controls.Add(button.Control);
                            }

                            // Sort EXE buttons and add placeholders for missing ones
                            var exeButtons = buttons.Where(b => desiredButtonOrder.Contains(b.Name)).ToList();
                            int entriesAdded = buttons.Count(b => b.Name == "Saves" || b.Name == "Manuals");

                            foreach (var desiredName in desiredButtonOrder)
                            {
                                var matchingButton = exeButtons.FirstOrDefault(b => b.Name == desiredName);
                                if (matchingButton.Control != null)
                                {
                                    groupPanel.Controls.Add(matchingButton.Control);
                                    entriesAdded++;
                                }
                                else
                                {
                                    // Add a placeholder (Label) to maintain alignment
                                    var placeholder = new Label
                                    {
                                        Size = new Size(maxButtonWidth, 30), // Match max button width
                                        Margin = new Padding(4), // Match button's margin
                                        Text = "" // Empty text for placeholder
                                    };
                                    groupPanel.Controls.Add(placeholder);
                                    LogToFile($"Added placeholder for '{desiredName}' with width: {maxButtonWidth}px");
                                }
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
                        LogToFile($"Error processing subdirectory {rootTHings}: {ex}");
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

        private string GetVersionFromRegistry(string subdirName)
        {
            try
            {
                string subdirLower = subdirName.ToLowerInvariant();
                foreach (var entry in registryIconCache)
                {
                    if (entry.Key.Contains(subdirLower))
                    {
                        string productVersion = entry.Value.Version;
                        return productVersion;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to retrieve version for {subdirName}: {ex}");
            }
            return null;
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
                        string productIcon = entry.Value.ProductIcon;
                        return Icon.ExtractAssociatedIcon(productIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Failed to retrieve icon for {subdirName}: {ex}");
            }
            return null;
        }

        // Helper to provide a default icon for width calculation
        private Icon GetDefaultIcon()
        {
            try
            {
                // Use a system icon (e.g., from shell32.dll) for width calculation
                return Icon.ExtractAssociatedIcon(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\shell32.dll");
            }
            catch
            {
                return null;
            }
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
                    button.Image = icon.ToBitmap().GetThumbnailImage(16, 16, null, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    LogToFile($"Failed to set icon for button {text}: {ex}");
                }
            }

            if (onClick != null)
            {
                button.Click += onClick;
                ToolTip toolTip = new ToolTip();
                toolTip.SetToolTip(button, $"Open {text} at {tag}");
            }
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
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"EXE not found at: {filePath}");
                    return;
                }

                if (!string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"File at {filePath} is not an executable (.exe).");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty,
                    UseShellExecute = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        MessageBox.Show($"Failed to start process for {filePath}: No process was created.");
                        LogToFile($"No process created for {filePath}");
                        return;
                    }

                    process.WaitForExit(1000);
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        MessageBox.Show($"Application {filePath} exited immediately with code {process.ExitCode}.");
                        LogToFile($"Application {filePath} exited with code {process.ExitCode}");
                    }
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 740)
            {
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
                            Verb = "runas"
                        };
                        Process.Start(psiElevated);
                    }
                    catch (Exception exElevated)
                    {
                        MessageBox.Show($"Failed to launch {filePath} with elevation: {exElevated.Message}");
                        LogToFile($"Elevation failed for {filePath}: {exElevated}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch '{filePath}':\n{ex.Message}\nDetails: {ex}");
                LogToFile($"Failed to launch {filePath}: {ex}");
            }
        }

        private static void LogToFile(string message)
        {
#if DEBUG
            try
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Suppress logging errors
            }
#endif
        }
    }
}