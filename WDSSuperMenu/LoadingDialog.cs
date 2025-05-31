namespace WDSSuperMenu
{


    public partial class Form1
    {
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

    }
}