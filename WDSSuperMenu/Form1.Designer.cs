namespace WDSSuperMenu
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private FlowLayoutPanel flowLayoutPanel;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (flowLayoutPanel != null))
            {
                flowLayoutPanel.Dispose();
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
            this.AutoSize = false; // we're setting size manually now
            this.AutoScroll = true; // allow scrolling if it's still too big
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);
            this.AutoScroll = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            this.flowLayoutPanel = new FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Dock = DockStyle.Fill;
            this.flowLayoutPanel.AutoScroll = true;
            this.flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel.WrapContents = false;
            this.Controls.Add(this.flowLayoutPanel);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Text = "WDS Super Menu";
            this.ResumeLayout(false);

            this.Resize += MainForm_Resize;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.Width != 1100)
            {
                this.Width = 1100;
            }
        }

        #endregion
    }
}
