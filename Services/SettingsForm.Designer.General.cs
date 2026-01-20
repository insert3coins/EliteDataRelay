using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeGeneralTab(TabPage generalTabPage)
        {
            // Session Tracking GroupBox (moved up; legacy file output removed)
            _grpSessionTracking = new GroupBox
            {
                Text = "Session Tracking",
                Location = new Point(12, 12),
                Size = new Size(520, 55),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };
            _chkEnableSessionTracking = new CheckBox
            {
                Text = "Enable session tracking (credits, cargo, etc.)",
                Location = new Point(15, 20),
                AutoSize = true
            };
            _grpSessionTracking.Controls.Add(_chkEnableSessionTracking);
            _chkEnableSessionTracking.ForeColor = Color.FromArgb(31, 41, 55);

            // Add controls to the General tab
            generalTabPage.Controls.Add(_grpSessionTracking);

            // Screenshots group (added after output/session)
            var grpScreenshots = new GroupBox
            {
                Text = "Screenshots",
                Location = new Point(12, _grpSessionTracking.Bottom + 12),
                Size = new Size(520, 80),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };
            var chkEnableScreenshotRenamer = new CheckBox
            {
                Text = "Auto-rename screenshots (system/body/timestamp)",
                Location = new Point(15, 25),
                AutoSize = true
            };
            chkEnableScreenshotRenamer.CheckedChanged += (s, e) => AppConfiguration.EnableScreenshotRenamer = chkEnableScreenshotRenamer.Checked;
            var lblFormat = new Label { Text = "Format:", Location = new Point(15, 50), AutoSize = true };
            var txtFormat = new TextBox { Location = new Point(70, 47), Size = new Size(430, 20) };
            txtFormat.TextChanged += (s, e) => AppConfiguration.ScreenshotRenameFormat = txtFormat.Text;
            // initialize values
            chkEnableScreenshotRenamer.Checked = AppConfiguration.EnableScreenshotRenamer;
            txtFormat.Text = AppConfiguration.ScreenshotRenameFormat;
            grpScreenshots.Controls.Add(chkEnableScreenshotRenamer);
            grpScreenshots.Controls.Add(lblFormat);
            grpScreenshots.Controls.Add(txtFormat);
            foreach (Control c in grpScreenshots.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            generalTabPage.Controls.Add(grpScreenshots);

            // EDSM upload
            var grpEdsm = new GroupBox
            {
                Text = "EDSM Upload",
                Location = new Point(12, grpScreenshots.Bottom + 12),
                Size = new Size(520, 110),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };
            var lblCommander = new Label { Text = "Commander Name:", Location = new Point(15, 25), AutoSize = true };
            _txtEdsmCommander = new TextBox { Location = new Point(150, 22), Size = new Size(330, 20) };
            var lblApiKey = new Label { Text = "API Key:", Location = new Point(15, 55), AutoSize = true };
            _txtEdsmApiKey = new TextBox { Location = new Point(150, 52), Size = new Size(330, 20), UseSystemPasswordChar = true };
            var lblHint = new Label
            {
                Text = "Saved locally; used to send your current system to EDSM.",
                Location = new Point(15, 80),
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 108, 125)
            };
            grpEdsm.Controls.Add(lblCommander);
            grpEdsm.Controls.Add(_txtEdsmCommander);
            grpEdsm.Controls.Add(lblApiKey);
            grpEdsm.Controls.Add(_txtEdsmApiKey);
            grpEdsm.Controls.Add(lblHint);
            generalTabPage.Controls.Add(grpEdsm);

            // Advanced moved to its own tab
        }
    }
}
