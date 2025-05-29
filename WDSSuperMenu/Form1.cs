using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.Xml.Linq;

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
            var foundPaths = new List<string>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                    continue;

                string wdsPath = Path.Combine(drive.RootDirectory.FullName, folderName);

                if (!Directory.Exists(wdsPath))
                    continue;

                int maxGroupPanelWidth = 0;
                int totalHeight = 0;
                string[] subdirectories = Directory.GetDirectories(wdsPath);

                foreach (string subdir in subdirectories)
                {
                    string savesPath = Path.Combine(subdir, "saves");
                    string manualsPath = Path.Combine(subdir, "manuals");

                    string[] exeFiles = Directory.GetFiles(subdir, "*.exe", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f).ToLowerInvariant()).ToArray();


                    // Create UI panel for each subdir
                    var groupPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(6) };
                    var titleLabel = new Label { Text = Path.GetFileName(subdir), Width = 200 };

                    groupPanel.Controls.Add(titleLabel);

                    var entriesAdded = 0;


                    if (Directory.Exists(savesPath))
                    {
                        Button button = BuildButton("> Saves", savesPath, (s, e) => Process.Start("explorer.exe", (string)((Button)s).Tag));
                        groupPanel.Controls.Add(button);

                        entriesAdded++;
                    }

                    if (Directory.Exists(manualsPath))
                    {
                        var button = BuildButton("> Manuals", manualsPath, (s, e) => Process.Start("explorer.exe", (string)((Button)s).Tag));                        
                        groupPanel.Controls.Add(button);

                        entriesAdded++;
                    }

                    foreach (var exePath in exeFiles)
                    {
                        string exeName = Path.GetFileName(exePath);
                        var appName = BuildAppName(exeName);
                        if (string.IsNullOrEmpty(appName))
                            continue;

                        var button = BuildButton(appName, Path.Combine(subdir, exePath), (s, e) =>
                        {
                            var filePath = (string)((Button)s).Tag;
                            try
                            {
                                if (!File.Exists(filePath))
                                {
                                    MessageBox.Show($"EXE not found at: {filePath}");
                                    return;
                                }

                                var psi = new ProcessStartInfo
                                {
                                    FileName = filePath,
                                    WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty, // Set to the executable's directory
                                    UseShellExecute = true, // Use shell for compatibility
                                                            // Optionally add: Arguments = "", if specific arguments are needed
                                };

                                Process.Start(psi);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to launch '{filePath}':\n{ex.GetType()}\n{ex.Message}");
                            }
                        });
                        groupPanel.Controls.Add(button);

                        entriesAdded++;
                    }

                    if (entriesAdded > 0)
                    {
                        flowLayoutPanel.Controls.Add(groupPanel);
                        flowLayoutPanel.PerformLayout(); // forces layout update

                        groupPanel.PerformLayout();
                        maxGroupPanelWidth = Math.Max(maxGroupPanelWidth, groupPanel.PreferredSize.Width);
                        totalHeight += groupPanel.PreferredSize.Height;
                    }
                }

                flowLayoutPanel.PerformLayout();

                int horizontalPadding = 50;
                int verticalPadding = 100;

                this.Width = Math.Min(maxGroupPanelWidth + horizontalPadding, Screen.PrimaryScreen.WorkingArea.Width - 100);
                this.Height = Math.Min(totalHeight + verticalPadding, Screen.PrimaryScreen.WorkingArea.Height - 100);
            }
        }

        private static Button BuildButton(string text, string tag, EventHandler onClick)
        {
            var button = new Button { Text = text, Tag = tag };
            button.Click += onClick;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Margin = new Padding(4);
            return button;
        }

        private static string BuildAppName(string exeName)
        {
            if (exeName.Contains("camp"))
                return "Campaign Editor";
            if (exeName.Contains("edit"))
                return "Scenario Editor";
            if (exeName.Contains("start"))
                return "Campaign Game";
            if (Path.GetFileNameWithoutExtension(exeName).Length <= 4 || exeName.Contains("demo"))
                return "Scenario Game";

            return string.Empty;
        }
    }
}
