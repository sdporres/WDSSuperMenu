using System.ComponentModel;
using System.Diagnostics;
using WDSSuperMenu.Core;

namespace WDSSuperMenu
{


    public partial class Form1 : Form
    {
        private Dictionary<string, RegistryProductEntry> registryIconCache;
        private Dictionary<string, string> appNameCache = new Dictionary<string, string>();

        public Form1()
        {
            InitializeComponent();

            // Hide form and prevent any rendering
            Opacity = 0;
            Visible = false;
            SuspendLayout();

            // Start the async initialization - don't call InitializeTabControl here
            LoadDataAsync();
        }

        private void InitializeTabControl()
        {
            // Create the TabControl if it doesn't exist
            if (tabControl == null)
            {
                tabControl = new TabControl
                {
                    Dock = DockStyle.Fill,
                    Name = "tabControl"
                };
            }

            // Clear any existing tabs
            tabControl.TabPages.Clear();
            seriesTabPanels.Clear();

            // Remove the existing flowLayoutPanel from controls and add tabControl instead
            if (this.Controls.Contains(flowLayoutPanel))
            {
                this.Controls.Remove(flowLayoutPanel);
            }

            if (!this.Controls.Contains(tabControl))
            {
                this.Controls.Add(tabControl);
            }

            // Create tabs for each series
            foreach (var series in SeriesCatalog.SeriesTitles.Keys.OrderBy(x => x))
            {
                var tabPage = new TabPage(series)
                {
                    Name = $"tab_{series.Replace(" ", "").Replace("&", "And")}"
                };

                var flowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Name = $"flow_{series.Replace(" ", "").Replace("&", "And")}"
                };

                tabPage.Controls.Add(flowPanel);
                tabControl.TabPages.Add(tabPage);
                seriesTabPanels[series] = flowPanel;
            }

            // Add an "All Games" tab at the beginning
            var allGamesTab = new TabPage("All Games")
            {
                Name = "tabAllGames"
            };

            var allGamesFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Name = "flowAllGames"
            };

            allGamesTab.Controls.Add(allGamesFlow);
            tabControl.TabPages.Insert(0, allGamesTab);
            seriesTabPanels["All Games"] = allGamesFlow;

            // Ensure the TabControl is brought to front
            tabControl.BringToFront();

            var lastTabName = Properties.Settings.Default.LastSelectedTabName;
            if (!string.IsNullOrEmpty(lastTabName))
            {
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    if (tabControl.TabPages[i].Text == lastTabName)
                    {
                        tabControl.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Add event handler to save tab selection
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                Properties.Settings.Default.LastSelectedTabName = tabControl.SelectedTab.Text;
                Properties.Settings.Default.Save();
            }
        }

        private async void LoadDataAsync()
        {
            // Create and show loading dialog
            using (var loadingDialog = new LoadingDialog())
            {
                loadingDialog.Show();
                Application.DoEvents(); // Ensure dialog is rendered

                // Load series data first (this is the key change)
                await SeriesCatalog.LoadSeriesDataAsync().ConfigureAwait(false);

                // Run heavy work in the background
                var iconCacheTask = Task.Run(() => RegistryTool.BuildRegistryIconCache());

                await Task.WhenAll(iconCacheTask).ConfigureAwait(false);

                // Update UI on UI thread
                this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    registryIconCache = iconCacheTask.Result;

                    // Now initialize the tab control after series data is loaded
                    InitializeTabControl();

                    ScanForWDSFolders();
                    // Force complete layout
                    flowLayoutPanel.PerformLayout();
                    this.PerformLayout();
                    ResumeLayout(true);
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
                    _ = CheckForUpdatesAsync().ConfigureAwait(false);
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
                                Margin = new Padding(12, 6, 6, 6),
                            };

                            var settingsButton = new Button
                            {
                                Image = Properties.Resources.export_settings,
                                AutoSize = true,
                                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                Margin = new Padding(4),
                                Size = new Size(40, 40),
                                TextImageRelation = TextImageRelation.Overlay,
                                ImageAlign = ContentAlignment.MiddleCenter,
                                Tag = "Settings",
                                Anchor = AnchorStyles.None
                            };

                            var buttons = new List<(string Name, Control Control)>();
                            buttons.Add(("Settings", settingsButton));

                            var subDirName = Path.GetFileName(subdir);
                            var versionLabel = GetVersionFromRegistry(subDirName);


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
                            string seriesName = SeriesCatalog.FindSeriesForGame(subDirName);

                            foreach (var exePath in exeFiles)
                            {

                                string exeName = Path.GetFileName(exePath).ToLowerInvariant();
                                var appButtonLabel = BuildAppName(exeName);
                                if (string.IsNullOrEmpty(appButtonLabel))
                                    continue;

                                var appName = Path.GetFileNameWithoutExtension(exeName);
                                if (appButtonLabel.Equals("Scenario Game"))
                                {
                                    var mainButton = BuildButton($"{subDirName}{(string.IsNullOrEmpty(versionLabel) ? "" : $" ({versionLabel})")}", exePath, groupIcon, (s, e) =>
                                        LaunchApplication((string)((Button)s).Tag));
                                    mainButton.AutoSize = false;
                                    mainButton.Size = new Size(250, 40);
                                    mainButton.TextAlign = ContentAlignment.MiddleCenter;
                                    buttons.Add((appButtonLabel, mainButton));
                                    Logger.LogToFile($"Created button '{appButtonLabel}' with width: {mainButton.Width}px");

                                    appNameCache.TryAdd(appName, seriesName);
                                    settingsButton.Click += (s, e) =>
                                    {
                                        var otherGamesInSeries = appNameCache.Where(kv => kv.Value == seriesName && kv.Key != appName)
                                            .Select(kv => kv.Key)
                                            .ToList();

                                        if (otherGamesInSeries.Count == 0)
                                        {
                                            MessageBox.Show(
                                                $"No other games found in the same series as '{seriesName}'.",
                                                "No Games to Apply Settings To",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information);
                                            return;
                                        }

                                        DialogResult result = MessageBox.Show(
                                                 $"This will copy the settings from '{subDirName}' to {otherGamesInSeries.Count} other games in the '{seriesName}' series:\n\n" +
                                                 $"{string.Join(", ", otherGamesInSeries.Take(5))}" +
                                                 $"{(otherGamesInSeries.Count > 5 ? $"\n...and {otherGamesInSeries.Count - 5} more" : "")}\n\n" +
                                                 "This action cannot be undone. Are you sure you want to continue?",
                                                 "Confirm Applying Settings",
                                                 MessageBoxButtons.YesNo,
                                                 MessageBoxIcon.Question);

                                        if (result == DialogResult.Yes)
                                        {
                                            CopySettingsToSeriesAsync(appName, subDirName, otherGamesInSeries);
                                        }
                                    };

                                    var toolTip = new ToolTip();
                                    string seriesDisplayName = string.IsNullOrEmpty(seriesName) ? "Other Games" : seriesName;
                                    toolTip.SetToolTip(settingsButton, $"Apply {subDirName} settings to all games in {seriesDisplayName} series");

                                    continue;
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

                            int entriesAdded = 0;

                            foreach (var button in buttons.Where(b => b.Name == "Scenario Game"))
                            {
                                groupPanel.Controls.Add(button.Control);
                                entriesAdded++;
                            }

                            foreach (var button in buttons.Where(b => b.Name == "Settings"))
                            {
                                groupPanel.Controls.Add(button.Control);
                            }

                            foreach (var button in buttons.Where(b => b.Name == "Saves" || b.Name == "Manuals"))
                            {
                                groupPanel.Controls.Add(button.Control);
                                entriesAdded++;
                            }

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
                                // Add to "All Games" tab
                                var allGamesPanel = seriesTabPanels["All Games"];
                                var allGamesGroupPanel = CloneGroupPanel(groupPanel);
                                allGamesPanel.Controls.Add(allGamesGroupPanel);

                                // Add to specific series tab if found
                                if (!string.IsNullOrEmpty(seriesName) && seriesTabPanels.ContainsKey(seriesName))
                                {
                                    var seriesPanel = seriesTabPanels[seriesName];
                                    seriesPanel.Controls.Add(groupPanel);
                                    Logger.LogToFile($"Added {subDirName} to {seriesName} tab");
                                }
                                else
                                {
                                    // If no series found, create an "Other Games" tab
                                    if (!seriesTabPanels.ContainsKey("Other Games"))
                                    {
                                        var otherGamesTab = new TabPage("Other Games")
                                        {
                                            Name = "tabOtherGames"
                                        };

                                        var otherGamesFlow = new FlowLayoutPanel
                                        {
                                            Dock = DockStyle.Fill,
                                            AutoScroll = true,
                                            FlowDirection = FlowDirection.TopDown,
                                            WrapContents = false,
                                            Name = "flowOtherGames"
                                        };

                                        otherGamesTab.Controls.Add(otherGamesFlow);
                                        tabControl.TabPages.Add(otherGamesTab);
                                        seriesTabPanels["Other Games"] = otherGamesFlow;
                                    }

                                    seriesTabPanels["Other Games"].Controls.Add(groupPanel);
                                    Logger.LogToFile($"Added {subDirName} to Other Games tab (no series match found)");
                                }

                                groupPanel.PerformLayout();
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        Logger.LogToFile($"Error processing subdirectory {parentDirectory}: {ex}");
                    }
                }

                // Remove empty tabs
                var tabsToRemove = new List<TabPage>();
                for (int i = 1; i < tabControl.TabPages.Count; i++) // Skip "All Games" tab
                {
                    var tabPage = tabControl.TabPages[i];
                    var flowPanel = tabPage.Controls[0] as FlowLayoutPanel;
                    if (flowPanel?.Controls.Count == 0)
                    {
                        tabsToRemove.Add(tabPage);
                    }
                }

                foreach (var tab in tabsToRemove)
                {
                    tabControl.TabPages.Remove(tab);
                    var key = seriesTabPanels.FirstOrDefault(x => x.Value == tab.Controls[0]).Key;
                    if (key != null)
                    {
                        seriesTabPanels.Remove(key);
                    }
                }

                // Calculate form size (keep existing logic but adjust for tabs)
                int iconWidth = 64;
                int settingsButtonWidth = 30;
                int labelWidth = 250;
                int savesManualButtonWidth = 80;
                int buttonCount = ExeNameMappings.Count;
                int totalMargins = 12 + 8 + 3 + 4 + (buttonCount * 8);

                int calculatedWidth = iconWidth + settingsButtonWidth + labelWidth +
                                     (2 * savesManualButtonWidth) +
                                     (buttonCount * maxButtonWidth) +
                                     totalMargins + 60;

                this.Width = Math.Min(calculatedWidth, Screen.PrimaryScreen.WorkingArea.Width - 100);
                Logger.LogToFile($"Calculated form width: {calculatedWidth}px, actual width: {this.Width}px");

                // Adjust height for tab control
                int maxTabHeight = 0;
                foreach (var panel in seriesTabPanels.Values)
                {
                    int panelHeight = panel.Controls.Cast<Control>().Sum(c => c.Height + c.Margin.Vertical);
                    maxTabHeight = Math.Max(maxTabHeight, panelHeight);
                }

                this.Height = Math.Min(maxTabHeight + 150, Screen.PrimaryScreen.WorkingArea.Height - 100); // +150 for tab headers and margins

                this.Location = new Point(
                         (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                         (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
                     );
            }
            finally
            {
                // Resume layout for all panels
                foreach (var panel in seriesTabPanels.Values)
                {
                    panel.ResumeLayout(true);
                }
            }
        }

        private FlowLayoutPanel CloneGroupPanel(FlowLayoutPanel original)
        {
            var clone = new FlowLayoutPanel
            {
                FlowDirection = original.FlowDirection,
                AutoSize = original.AutoSize,
                AutoSizeMode = original.AutoSizeMode,
                WrapContents = original.WrapContents,
                Margin = original.Margin
            };

            // Clone all controls
            foreach (Control control in original.Controls)
            {
                if (control is PictureBox pb)
                {
                    var clonePb = new PictureBox
                    {
                        Size = pb.Size,
                        Margin = pb.Margin,
                        Image = pb.Image
                    };
                    clone.Controls.Add(clonePb);
                }
                else if (control is Button btn)
                {
                    var cloneBtn = new Button
                    {
                        Text = btn.Text,
                        Tag = btn.Tag,
                        AutoSize = false,
                        Size = btn.Size,
                        Margin = btn.Margin,
                        Image = btn.Image,
                        TextImageRelation = btn.TextImageRelation,
                        ImageAlign = btn.ImageAlign,
                        Padding = btn.Padding,
                        Anchor = btn.Anchor
                    };

                    // Recreate event handlers based on button type/tag
                    if (btn.Tag is string tag)
                    {
                        if (Directory.Exists(tag))
                        {
                            // This is a folder button (Saves/Manuals)
                            cloneBtn.Click += (s, e) => Process.Start("explorer.exe", (string)((Button)s).Tag);

                            var toolTip = new ToolTip();
                            toolTip.SetToolTip(cloneBtn, $"Open {tag}");
                        }
                        else if (File.Exists(tag) && Path.GetExtension(tag).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            // This is an executable button
                            cloneBtn.Click += (s, e) => LaunchApplication((string)((Button)s).Tag);

                            var toolTip = new ToolTip();
                            toolTip.SetToolTip(cloneBtn, $"Open {tag}");
                        }
                        else if (tag.Equals("Settings"))
                        {
                            cloneBtn.Image = Properties.Resources.export_settings_empty;
                            cloneBtn.FlatStyle = FlatStyle.Flat;
                            cloneBtn.BackColor = this.BackColor;
                            cloneBtn.ForeColor = this.BackColor;
                            cloneBtn.Text = "";
                            cloneBtn.TabStop = false;
                            cloneBtn.Enabled = false;
                        }
                    }

                    if (cloneBtn != null)
                        clone.Controls.Add(cloneBtn);
                }
                else if (control is Label lbl)
                {
                    var cloneLbl = new Label
                    {
                        Text = lbl.Text,
                        Size = lbl.Size,
                        Margin = lbl.Margin,
                        AutoSize = lbl.AutoSize,
                        TextAlign = lbl.TextAlign
                    };
                    clone.Controls.Add(cloneLbl);
                }
            }

            return clone;
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
                ImageAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.None
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
            { "camp", new ExeNameOrder("Campaign Editor", 3) },
            { "edit", new ExeNameOrder("Scenario Editor", 2) },
            { "start", new ExeNameOrder("Campaign Game", 1) },
        };

        private static string BuildAppName(string exeName)
        {
            exeName = exeName.ToLowerInvariant();

            foreach (var mapping in ExeNameMappings)
            {
                if (exeName.Contains(mapping.Key))
                    return mapping.Value.Name;
            }

            if (RegistryReplacer.IsMainSoftware(Path.GetFileNameWithoutExtension(exeName)))
            {
                return "Scenario Game";
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
                        Logger.LogToFile($"No process created for {filePath}");
                        return;
                    }

                    process.WaitForExit(1000);
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        MessageBox.Show($"Application {filePath} exited immediately with code {process.ExitCode}.");
                        Logger.LogToFile($"Application {filePath} exited with code {process.ExitCode}");
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
                        Logger.LogToFile($"Elevation failed for {filePath}: {exElevated}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch '{filePath}':\n{ex.Message}\nDetails: {ex}");
                Logger.LogToFile($"Failed to launch {filePath}: {ex}");
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowAboutDialog();
        }

        private void ShowAboutDialog()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = UpdateChecker.GetCurrentVersion();
            var productName = "WDS Super Menu";
            var description = "A launcher for WDS game applications";
            var copyright = "MIT License";

            string aboutText = $"{productName}\n" +
                              $"Version: {version}\n\n" +
                              $"{description}\n\n" +
                              $"{copyright}";

            MessageBox.Show(aboutText, $"About {productName}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task CopySettingsToSeriesAsync(string sourceAppName, string sourceDisplayName, List<string> targetAppNames)
        {
            using (var progressDialog = new SettingsProgressDialog())
            {
                progressDialog.Show(this);
                Application.DoEvents();

                try
                {
                    // Run the settings copy operation in the background
                    var result = await Task.Run(() =>
                    {
                        int successCount = 0;
                        int failureCount = 0;
                        var failedGames = new List<string>();
                        int totalGames = appNameCache.Count - 1; // Exclude source game
                        int currentGame = 0;

                        foreach (var targetAppName in targetAppNames)
                        {
                            if (targetAppName.Equals(sourceAppName, StringComparison.OrdinalIgnoreCase))
                                continue; // Skip copying to itself

                            currentGame++;

                            // Update progress on UI thread
                            this.Invoke((MethodInvoker)delegate
                            {
                                progressDialog.UpdateProgress(targetAppName, currentGame, totalGames);
                            });

                            try
                            {
                                RegistryReplacer.CopySettings(sourceAppName, targetAppName);
                                Logger.LogToFile($"Copied settings from {sourceAppName} to {targetAppName}");
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogToFile($"Failed to copy settings from {sourceAppName} to {targetAppName}: {ex}");
                                failureCount++;
                                failedGames.Add(targetAppName);
                            }

                            // Small delay to make progress visible
                            Thread.Sleep(100);
                        }

                        return new { SuccessCount = successCount, FailureCount = failureCount, FailedGames = failedGames };
                    });

                    // Show completion message on UI thread
                    string message = $"Settings copy completed!\n\n" +
                                   $"Source: {sourceDisplayName}\n" +
                                   $"Successful: {result.SuccessCount}\n" +
                                   $"Failed: {result.FailureCount}";

                    if (result.FailedGames.Count > 0)
                    {
                        message += $"\n\nFailed games:\n{string.Join(", ", result.FailedGames)}";
                    }

                    MessageBox.Show(this, message, "Settings Copy Complete",
                                  MessageBoxButtons.OK,
                                  result.FailureCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogToFile($"Unexpected error during settings copy: {ex}");
                    MessageBox.Show(this, $"An unexpected error occurred during settings copy:\n{ex.Message}",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    progressDialog.Close();
                }
            }
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