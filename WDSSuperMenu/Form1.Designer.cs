namespace WDSSuperMenu
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private FlowLayoutPanel flowLayoutPanel;
        private MenuStrip menuStrip;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private TabControl tabControl;
        private Dictionary<string, FlowLayoutPanel> seriesTabPanels = new Dictionary<string, FlowLayoutPanel>();

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (flowLayoutPanel != null)
                {
                    flowLayoutPanel.Dispose();
                }
                if (menuStrip != null)
                {
                    menuStrip.Dispose();
                }
                if (tabControl != null)
                {
                    tabControl.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip = new MenuStrip();
            this.helpToolStripMenuItem = new ToolStripMenuItem();
            this.checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.aboutToolStripMenuItem = new ToolStripMenuItem();

            this.flowLayoutPanel = new FlowLayoutPanel();
            this.tabControl = new TabControl();

            this.menuStrip.SuspendLayout();
            this.SuspendLayout();

            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new ToolStripItem[] {
                this.helpToolStripMenuItem});
            this.menuStrip.Location = new Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new Size(600, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";

            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.checkForUpdatesToolStripMenuItem,
                this.toolStripSeparator1,
                this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";

            // 
            // checkForUpdatesToolStripMenuItem
            // 
            this.checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            this.checkForUpdatesToolStripMenuItem.Size = new Size(180, 22);
            this.checkForUpdatesToolStripMenuItem.Text = "Check for &Updates";
            this.checkForUpdatesToolStripMenuItem.Click += new EventHandler(this.CheckForUpdatesMenuItem_Click);

            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(177, 6);

            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new EventHandler(this.aboutToolStripMenuItem_Click);

            // 
            // tabControl
            // 
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Name = "tabControl";
            this.tabControl.TabIndex = 1;

            // Form settings
            this.AutoSize = false; // we're setting size manually now
            this.AutoScroll = true; // allow scrolling if it's still too big
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip;

            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Dock = DockStyle.Fill;
            this.flowLayoutPanel.AutoScroll = true;
            this.flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel.WrapContents = false;

            // Add controls to form - TabControl will be added in InitializeTabControl()
            // Note: flowLayoutPanel is kept for backward compatibility but removed in InitializeTabControl()
            this.Controls.Add(this.flowLayoutPanel);
            this.Controls.Add(this.menuStrip);
            // Don't add tabControl here - it's added in InitializeTabControl()

            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Text = "WDS Super Menu";

            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}