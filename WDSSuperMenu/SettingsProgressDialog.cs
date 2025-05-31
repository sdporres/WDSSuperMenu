namespace WDSSuperMenu
{


    public partial class Form1
    {
        private class SettingsProgressDialog : Form
        {
            private Label statusLabel;
            private ProgressBar progressBar;
            private Label progressLabel;

            public SettingsProgressDialog()
            {
                InitializeProgressDialog();
            }

            private void InitializeProgressDialog()
            {
                Text = "Copying Settings";
                Size = new Size(400, 150);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                ControlBox = false;
                ShowInTaskbar = false;
                MaximizeBox = false;
                MinimizeBox = false;

                var mainLabel = new Label
                {
                    Text = "Copying settings to games...",
                    AutoSize = true,
                    Location = new Point(20, 20),
                    Font = new Font(Font, FontStyle.Bold)
                };
                Controls.Add(mainLabel);

                statusLabel = new Label
                {
                    Text = "Preparing...",
                    AutoSize = false,
                    Size = new Size(360, 20),
                    Location = new Point(20, 50),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                Controls.Add(statusLabel);

                progressBar = new ProgressBar
                {
                    Location = new Point(20, 75),
                    Size = new Size(360, 20),
                    Style = ProgressBarStyle.Blocks
                };
                Controls.Add(progressBar);

                progressLabel = new Label
                {
                    Text = "0 / 0",
                    AutoSize = true,
                    Location = new Point(20, 100),
                    ForeColor = SystemColors.GrayText
                };
                Controls.Add(progressLabel);
            }

            public void UpdateProgress(string currentGame, int current, int total)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string, int, int>(UpdateProgress), currentGame, current, total);
                    return;
                }

                statusLabel.Text = $"Copying to: {currentGame}";
                progressBar.Maximum = total;
                progressBar.Value = current;
                progressLabel.Text = $"{current} / {total}";
                Application.DoEvents();
            }
        }

    }
}