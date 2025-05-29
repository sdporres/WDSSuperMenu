using System.ComponentModel;
using System.Diagnostics;

namespace WDSSuperMenu
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ScanForWDSFolders();
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

                            var groupPanel = new FlowLayoutPanel
                            {
                                FlowDirection = FlowDirection.LeftToRight,
                                AutoSize = true,
                                WrapContents = false,
                                Margin = new Padding(6)
                            };
                            groupPanel.Controls.Add(new Label { Text = Path.GetFileName(subdir), Width = 200 });

                            int entriesAdded = 0;

                            if (Directory.Exists(savesPath))
                            {
                                groupPanel.Controls.Add(BuildButton("> Saves", savesPath, (s, e) => Process.Start("explorer.exe", (string)((Button)s).Tag)));
                                entriesAdded++;
                            }

                            if (Directory.Exists(manualsPath))
                            {
                                groupPanel.Controls.Add(BuildButton("> Manuals", manualsPath, (s, e) => Process.Start("explorer.exe", (string)((Button)s).Tag)));
                                entriesAdded++;
                            }

                            foreach (var exePath in exeFiles)
                            {
                                string exeName = Path.GetFileName(exePath).ToLowerInvariant();
                                var appName = BuildAppName(exeName);
                                if (string.IsNullOrEmpty(appName))
                                    continue;

                                groupPanel.Controls.Add(BuildButton(appName, exePath, (s, e) =>
                                {
                                    var filePath = (string)((Button)s).Tag;
                                    LaunchApplication(filePath);
                                }));
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

        private static Button BuildButton(string text, string tag, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Tag = tag,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(4),
                MinimumSize = new Size(100, 30) // Ensure consistent button size
            };
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
