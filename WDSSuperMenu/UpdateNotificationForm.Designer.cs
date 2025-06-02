namespace WDSSuperMenu
{
    partial class UpdateNotificationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "UpdateNotificationForm";

            this.Size = new Size(520, 420);
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


            int buttonY = 330; // instead of 330
                               // Buttons

            skipVersionButton = new Button
            {
                Text = "Skip Version",
                Location = new Point(160, buttonY),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Ignore
            };
            skipVersionButton.Click += SkipVersionButton_Click;

            remindLaterButton = new Button
            {
                Text = "Remind Later",
                Location = new Point(250, buttonY),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel
            };

            downloadButton = new Button
            {
                Text = "Download Update",
                Location = new Point(350, buttonY),
                Size = new Size(120, 30),
                DialogResult = DialogResult.OK
            };
            downloadButton.Click += DownloadButton_Click;

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                titleLabel, versionLabel, releaseDateLabel, notesLabel,
                releaseNotesTextBox, autoCheckCheckBox, downloadButton,
                remindLaterButton, skipVersionButton
            });
        }

        #endregion
    }
}