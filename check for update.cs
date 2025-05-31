using System.ComponentModel;
using System.Diagnostics;
using WDSSuperMenu.Core;

namespace WDSSuperMenu
{
    public partial class Form1 : Form
    {
        private Dictionary<string, RegistryProductEntry> registryIconCache;
        private List<string> appNameCache = new List<string>();

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
                var iconCacheTask = Task.Run(() => RegistryTool.BuildRegistryIconCache());

                await Task.WhenAll(iconCacheTask);

                // Update UI on UI thread
                await Task.Run(() =>
                {
                    Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        registryIconCache = iconCacheTask.Result;
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

                // Check for updates after form is loaded (if enabled)
                if (Properties.Settings.Default.AutoCheckUpdates)
                {
                    _ = CheckForUpdatesAsync();
                }
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                // Don't check more than once per day
                if (DateTime.Now - Properties.Settings.Default.LastUpdateCheck < TimeSpan.FromDays(1))
                {
                    Logger.LogToFile("Skipping update check - already checked today");
                    return;
                }

                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();
                
                // Update last check time
                Properties.Settings.Default.LastUpdateCheck = DateTime.Now;
                Properties.Settings.Default.Save();

                if (updateInfo.IsUpdateAvailable && 
                    updateInfo.Version != Properties.Settings.Default.SkippedVersion)
                {
                    // Show update notification on UI thread
                    this.Invoke((MethodInvoker)delegate
                    {
                        ShowUpdateNotification(updateInfo);
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Error during automatic update check: {ex}");
                // Don't show error to user for automatic checks
            }
        }

        private void ShowUpdateNotification(UpdateInfo updateInfo)
        {
            using (var updateForm = new UpdateNotificationForm(updateInfo))
            {
                var result = updateForm.ShowDialog(this);
                
                if (result == DialogResult.OK)
                {
                    // User chose to download - the form handles opening the URL
                    Logger.LogToFile($"User chose to download update {updateInfo.Version}");
                }
                else if (result == DialogResult.Ignore)
                {
                    // User chose to skip this version - already handled in the form
                    Logger.LogToFile($"User chose to skip version {updateInfo.Version}");
                }
                else
                {
                    // User chose remind later - do nothing
                    Logger.LogToFile("User chose to be reminded later about update");
                }
            }
        }

        // Manual update check (called from menu)
        private async void CheckForUpdatesMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Show checking message
                var originalText = this.Text;
                this.Text = "WDS Super Menu - Checking for updates...";
                
                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();
                
                this.Text = originalText;
                
                if (updateInfo.IsUpdateAvailable)
                {
                    ShowUpdateNotification(updateInfo);
                }
                else
                {
                    MessageBox.Show(this, "You are using the latest version of WDS Super Menu.", 
                        "No Updates Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                this.Text = this.Text.Replace(" - Checking for updates...", "");
                Logger.LogToFile($"Error during manual update check: {ex}");
                MessageBox.Show(this, "Failed to check for updates. Please try again later.", 
                    "Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ScanForWDSFolders()
        {
            flowLayoutPanel.SuspendLayout();
            int maxGroupPanelWidth = 0;
            int totalHeight = 0;

            int maxButtonWidth = 0;
            foreach (var buttonName in ExeNameMappings.OrderBy(x => x.Value.Order).Select(x => x.Value.Name))
            {
                // Create a more accurate temporary button for measurement
                var tempButton = new Button
                {
                    Text = buttonName,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Margin = new Padding(4),
                    MinimumSize = new Size(100, 30),
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0) // Account for padding
                };

                // Add space for icon if present
                int textWidth = TextRenderer.MeasureText(buttonName, tempButton.Font).Width;
                int iconWidth = 20; // 16px icon + padding
                int totalWidth = textWidth + iconWidth + tempButton.Margin.Horizontal + tempButton.Padding.Horizontal + 10; // extra padding

                maxButtonWidth = Math.Max(maxButtonWidth, Math.Max(totalWidth, tempButton.MinimumSize.Width));
                Logger.LogToFile($"Calculated width for temp button '{buttonName}': {totalWidth}px");
                tempButton.Dispose();
            }
            Logger.LogToFile($"Maximum button width for EXE buttons: {maxButtonWidth}px");

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
                                    Logger.LogToFile($"Group icon for {Path.GetFileName(subdir)} set to {iconSize}x{iconSize}, native size: {groupIcon.Width}x{groupIcon.Height}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogToFile($"Failed to set group icon for {Path.GetFileName(subdir)}: {ex}");
                                }
                            }
                            else
                            {
                                Logger.LogToFile($"No icon found for {Path.GetFileName(subdir)}, using placeholder {iconSize}x{iconSize}");
                            }

                            groupPanel.Controls.Add(pictureBox);

                            var settingsButton = new Button
                            {
                                Image = Properties.Resources.export_settings,
                                Width = 32,
                                Height = 32,
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
                                Height = 32,
                                AutoSize = false,
                                TextAlign = ContentAlignment.MiddleLeft,
                                Margin = new Padding(3)
                            });

                            var buttons = new List<(string Name, Control Control)>();

                            if (Directory.Exists(savesPath))
                            {
                                buttons.Add(("Saves", BuildButton("Saves", savesPath, Properties.Resources.icons8_folder_64, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag))));
                            }

                            if (Directory.Exists(manualsPath))
                            {
                                buttons.Add(("Manuals", BuildButton("Manuals", manualsPath, Properties.Resources.icons8_pdf_64, (s, e) =>
                                    Process.Start("explorer.exe", (string)((Button)s).Tag))));
                            }

                            foreach (var exePath in exeFiles)
                            {
                                string exeName = Path.GetFileName(exePath).ToLowerInvariant();
                                var appButtonLabel = BuildAppName(exeName);
                                if (string.IsNullOrEmpty(appButtonLabel))
                                    continue;

                                var appName = Path.GetFileNameWithoutExtension(exeName);
                                if (appButtonLabel.Equals("Scenario Game") && !appNameCache.Contains(appName))
                                {
                                    appNameCache.Add(appName);
                                    settingsButton.Click += (s, e) =>
                                    {
                                        // Show confirmation dialog
                                        DialogResult result = MessageBox.Show(
                                            $"This will copy the settings from '{subDirName}' to all other games.\n\n" +
                                            "This action cannot be undone. Are you sure you want to continue?",
                                            "Confirm Applying Settings",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question);

                                        if (result == DialogResult.Yes)
                                        {
                                            //Task.Run(() => 
                                            CopySettingsAsync(appName, subDirName);//);
                                        }
                                    };

                                    var toolTip = new ToolTip();
                                    toolTip.SetToolTip(settingsButton, $"Apply {subDirName} settings to all games");
                                }

                                Icon exeIcon = null;
                                try
                                {
                                    if (File.Exists(exePath))
                                        exeIcon = Icon.ExtractAssociatedIcon(exePath);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogToFile($"Failed to extract icon from {exePath}: {ex}");
                                }

                                var button = BuildButton(appButtonLabel, exePath, exeIcon, (s, e) =>
                                    LaunchApplication((string)((Button)s).Tag));
                                button.AutoSize = false;
                                button.Size = new Size(maxButtonWidth, 30);
                                buttons.Add((appButtonLabel, button));
                                Logger.LogToFile($"Created button '{appButtonLabel}' with width: {button.Width}px");
                            }

                            foreach (var button in buttons.Where(b => b.Name == "Saves" || b.Name == "Manuals"))
                            {
                                groupPanel.Controls.Add(button.Control);
                            }

                            int entriesAdded = 0;
                            foreach (var desiredName in ExeNameMappings.OrderBy(x => x.Value.Order).Select(x => x.Value.Name))
                            {
                                var matchingButton = buttons.FirstOrDefault(b => b.Name == desiredName);
                                if (matchingButton.Control != null)
                                {
                                    groupPanel.Controls.Add(matchingButton.Control);
                                    entriesAdded++;
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
                        Logger.LogToFile($"Error processing subdirectory {parentDirectory}: {ex}");
                    }
                }

                // Calculate the total width more accurately
                int iconWidth = 64; // PictureBox width
                int settingsButtonWidth = 30; // Settings button width
                int labelWidth = 250; // Label width
                int savesManualButtonWidth = 80; // Approximate width for "Saves"/"Manuals" buttons
                int buttonCount = ExeNameMappings.Count; // Number of exe buttons
                int totalMargins = 12 + 8 + 3 + 4 + (buttonCount * 8); // All margins combined

                int calculatedWidth = iconWidth + settingsButtonWidth + labelWidth +
                                     (2 * savesManualButtonWidth) + // Max 2 saves/manual buttons
                                     (buttonCount * maxButtonWidth) +
                                     totalMargins + 70; // Extra padding

                this.Width = Math.Min(calculatedWidth, Screen.PrimaryScreen.WorkingArea.Width - 100);
                Logger.LogToFile($"Calculated form width: {calculatedWidth}px, actual width: {this.Width}px");

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
                Logger.LogToFile($"Failed to retrieve version for {subdirName}: {ex}");
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
                Logger.LogToFile($"Failed to retrieve icon for {subdirName}: {ex}");
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
                MinimumSize = new Size(100, 32),
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

                    int imageWidth = button.Image?.Width ?? 0;
                    int textWidth = TextRenderer.MeasureText(button.Text, button.Font).Width;
                    int availableWidth = button.Width - button.Margin.Horizontal;
                    int contentWidth = imageWidth + textWidth + 4; // 4px spacing between image and text
                    int leftPadding = Math.Max(8, (availableWidth - contentWidth) / 2);

                    button.Padding = new Padding(leftPadding, 0, 8, 0);
                }
                catch (Exception ex)
                {
                    Logger.LogToFile($"Failed to set icon for button {text}: {ex}");
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
            exeName