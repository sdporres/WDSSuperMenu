using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WDSSuperMenu
{
    public class RegistryProductEntry
    {
        public string ProductName { get; set; }
        public string ProductIcon { get; set; }
        public string Version { get; set; }
        public RegistryProductEntry(string productName, string productIcon, string version)
        {
            ProductName = productName;
            ProductIcon = productIcon;
            Version = version;
        }
    }

    public partial class Form1 : Form
    {
        private Dictionary<string, RegistryProductEntry> registryIconCache;
        private Dictionary<string, Dictionary<string, string>> registryOptionsCache;

        public Form1()
        {
            InitializeComponent();
            // Hide form and prevent any rendering
            Opacity = 0;
            Visible = false;
            SuspendLayout();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            // Create and show loading dialog
            using (var loadingDialog = new LoadingDialog())
            {
                loadingDialog.Show();
                Application.DoEvents(); // Ensure dialog is rendered

                // Run heavy work in the background
                var iconCacheTask = Task.Run(() => BuildRegistryIconCache());
                var optionsCacheTask = Task.Run(() => BuildRegistryOptionsCache());

                await Task.WhenAll(iconCacheTask, optionsCacheTask);

                // Update UI on UI thread
                await Task.Run(() =>
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        registryIconCache = iconCacheTask.Result;
                        registryOptionsCache = optionsCacheTask.Result;
                        ScanForWDSFolders();
                        // Force complete layout
                        flowLayoutPanel.PerformLayout();
                        this.PerformLayout();
                        ResumeLayout(true);
                    });
                });

                // Show form only after all rendering is complete
                this.Invoke((MethodInvoker)delegate
                {
                    Opacity = 1;
                    Visible = true;
                    loadingDialog.Close();
                });
            }
        }

        private class LoadingDialog : Form
        {
            public LoadingDialog()
            {
                Text = "Loading";
                Size = new Size(200, 100);
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                ControlBox = false;
                ShowInTaskbar = false;

                var label = new Label
                {
                    Text = "Loading, please wait...",
                    AutoSize = true,
                    Location = new Point(20, 30)
                };
                Controls.Add(label);
            }
        }

        private Dictionary<string, RegistryProductEntry> BuildRegistryIconCache()
        {
            var cache = new Dictionary<string, RegistryProductEntry>();
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

                                cache[productName.ToLowerInvariant()] = new RegistryProductEntry(productName, productIcon, productVersion);
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

        private Dictionary<string, Dictionary<string, string>> BuildRegistryOptionsCache()
        {
            var cache = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                using (RegistryKey wdsKey = Registry.CurrentUser.OpenSubKey(@"Software\WDS LLC"))
                {
                    if (wdsKey == null)
                    {
                        LogToFile("Failed to open registry key HKLM\\SOFTWARE\\Classes\\Installer\\Products");
                        return cache;
                    }

                    foreach (string appName in wdsKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey productKey = wdsKey.OpenSubKey(appName))
                            {
                                if (productKey == null)
                                    continue;

                                using (RegistryKey optionsKey = productKey.OpenSubKey("Options"))
                                {
                                    if (optionsKey == null)
                                        continue;

                                    cache.Add(appName, new Dictionary<string, string>());
                                    foreach (string optionsValueName in optionsKey.GetValueNames())
                                    {
                                        var optionValueData = optionsKey.GetValue(optionsValueName);
                                        if (optionValueData != null && optionValueData is int intValue)
                                        {
                                            var valueHex = $"{intValue:X}";
                                            cache[appName][optionsValueName] = valueHex;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Failed to process registry subkey {appName}: {ex}");
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
            uint value = Convert.ToUInt32(hexValue, 16);
            int major = (int)(value >> 24) & 0xFF;
            int minor = (int)(value >> 16) & 0xFF;
            int patch = (int)(value & 0xFFFF);
            return $"{major}.{minor:D2}.{patch}";
        }

        private void ScanForWDSFolders()
        {
            flowLayoutPanel.SuspendLayout();
            int maxGroupPanelWidth = 0;
            int totalHeight = 0;

            int maxButtonWidth = 0;
            foreach (var buttonName in ExeNameMappings.OrderBy(x => x.Value.Order).Select(x => x.Value.Name))
            {
                var tempButton = BuildButton(buttonName, "", GetDefaultIcon(), null);
                tempButton.AutoSize = true;
                tempButton.PerformLayout();
                int width = tempButton.PreferredSize.Width;
                maxButtonWidth = Math.Max(maxButtonWidth, width);
                LogToFile($"Calculated width for temp button '{buttonName}': {width}px");
            }
            LogToFile($"Maximum button width for EXE buttons: {maxButtonWidth}px");

            try
            {
                var parentDirectories = RegistryAppFinder.FindUniqueParentDirectories();
                foreach (var parentDirectory in parentDirectories)
                {
                    try
                    {
                        string[] subdirectories = Directory.GetDirectories(parentDirectory);
                        foreach (string subdir in subdirectories)
                        {
                            string savesPath = Path.Combine(subdir, "saves");
                            string manualsPath = Path.Combine(subdir, "manuals");
                            string[] exeFiles = Directory.GetFiles(subdir, "*.exe", SearchOption.TopDirectoryOnly);

                            Icon groupIcon = GetIconFromRegistry(Path.GetFileName(subdir));

                            var groupPanel = new FlowLayoutPanel
                            {
                                FlowDirection = FlowDirection.LeftToRight,
                                AutoSize = true,
                                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                WrapContents = false,
                                Margin = new Padding(12, 6, 6, 6)
                            };

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

                            var settingsButton = new Button
                            {
                                Image = Properties.Resources.export_settings,
                                Width = 30,
                                Height = 30,
                                Margin = new Padding(0, 4, 0, 0),
                                TextImageRelation = TextImageRelation.Overlay,
                                ImageAlign = ContentAlignment.MiddleCenter
                            };
                            groupPanel.Controls.Add(settingsButton);

                            var subDirName = Path.GetFileName(subdir);
                            var versionLabel = GetVersionFromRegistry(subDirName);
                            groupPanel.Controls.Add(new Label
                            {
                                Text = $"{subDirName}{(string.IsNullOrEmpty(versionLabel) ? "" : $" ({versionLabel})")}",
                                Width = 250,
                                Height = 30,
                                AutoSize = false,
                                TextAlign = ContentAlignment.MiddleLeft,
                                Margin = new Padding(3)
                            });

                            var buttons = new List<(string Name, Control Control)>();

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
                                button.AutoSize = false;
                                button.Size = new Size(maxButtonWidth, 30);
                                buttons.Add((appName, button));
                                LogToFile($"Created button '{appName}' with width: {button.Width}px");
                            }

                            foreach (var button in buttons.Where(b => b.Name == "Saves" || b.Name == "Manuals"))
                            {
                                groupPanel.Controls.Add(button.Control);
                            }

                            int entriesAdded = buttons.Count(b => b.Name == "Saves" || b.Name == "Manuals");

                            foreach (var desiredName in ExeNameMappings.OrderBy(x => x.Value.Order).Select(x => x.Value.Name))
                            {
                                var matchingButton = buttons.FirstOrDefault(b => b.Name == desiredName);
                                if (matchingButton.Control != null)
                                {
                                    groupPanel.Controls.Add(matchingButton.Control);
                                    entriesAdded++;
                                }
                                else
                                {
                                    var placeholder = new Label
                                    {
                                        Size = new Size(maxButtonWidth, 30),
                                        Margin = new Padding(4),
                                        Text = ""
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
                        LogToFile($"Error processing subdirectory {parentDirectory}: {ex}");
                    }
                }

                this.Width = Math.Min(maxGroupPanelWidth + 50, Screen.PrimaryScreen.WorkingArea.Width - 100);
                this.Height = Math.Min(totalHeight + 100, Screen.PrimaryScreen.WorkingArea.Height - 100);

                this.Location = new Point(
                         (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                         (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
                     );
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

        private Icon GetDefaultIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\shell32.dll");
            }
            catch
            {
                return null;
            }
        }

        private static Button BuildButton(string text, string tag, object image, EventHandler onClick)
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

            if (image != null)
            {
                try
                {
                    if (image is Icon icon)
                        button.Image = icon.ToBitmap().GetThumbnailImage(16, 16, null, IntPtr.Zero);
                    else if (image is string imagePath && File.Exists(imagePath))
                        button.Image = Icon.ExtractAssociatedIcon(imagePath).ToBitmap().GetThumbnailImage(16, 16, null, IntPtr.Zero);
                    else if (image is Bitmap bitmap)
                        button.Image = bitmap;
                    else if (image is Image img)
                        button.Image = img;

                    int groupWidth = button.Image.Width + TextRenderer.MeasureText(button.Text, button.Font).Width;
                    int padding = ((button.Width - groupWidth) / 2) + 8;
                    button.Padding = new Padding(padding, 0, 0, 0);
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
                toolTip.SetToolTip(button, $"Open {tag}");
            }
            return button;
        }

        private static readonly Dictionary<string, ExeNameOrder> ExeNameMappings = new()
        {
            { "camp", new ExeNameOrder("Campaign Editor", 4) },
            { "edit", new ExeNameOrder("Scenario Editor", 3) },
            { "start", new ExeNameOrder("Campaign Game", 2) },
            { "default", new ExeNameOrder("Scenario Game", 1)}
        };

        private static string BuildAppName(string exeName)
        {
            exeName = exeName.ToLowerInvariant();
            if (exeName.Contains("demo") || Path.GetFileNameWithoutExtension(exeName).Length <= 4)
                return ExeNameMappings["default"].Name;

            foreach (var mapping in ExeNameMappings)
            {
                if (exeName.Contains(mapping.Key))
                    return mapping.Value.Name;
            }
            return null;
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

    public class ExeNameOrder
    {
        public ExeNameOrder(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; set; }
        public int Order { get; set; }
    }
}