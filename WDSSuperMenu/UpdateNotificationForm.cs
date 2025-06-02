using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
