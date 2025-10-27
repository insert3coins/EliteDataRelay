using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeAdvancedWebTab(TabPage tab)
        {
            tab.BackColor = Color.White;
            tab.ForeColor = Color.FromArgb(17, 24, 39);

            // Advanced group (from InitializeAdvancedTab)
            var grpAdvanced = new GroupBox
            {
                Text = "Advanced",
                Location = new Point(12, 12),
                Size = new Size(520, 80),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            _chkFastStart = new CheckBox
            {
                Text = "Fast start: skip journal history at startup",
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = AppConfiguration.FastStartSkipJournalHistory
            };
            _chkFastStart.CheckedChanged += (s, e) => AppConfiguration.FastStartSkipJournalHistory = _chkFastStart.Checked;
            grpAdvanced.Controls.Add(_chkFastStart);
            foreach (Control c in grpAdvanced.Controls) c.ForeColor = Color.FromArgb(31, 41, 55);
            tab.Controls.Add(grpAdvanced);

            // Web Overlay group (from InitializeWebOverlayTab)
            var grpWeb = new GroupBox
            {
                Text = "Browser-Source Overlay Server",
                Location = new Point(12, grpAdvanced.Bottom + 12),
                Size = new Size(520, 110),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            var chkEnable = new CheckBox { Text = "Enable web overlay server", Location = new Point(15, 25), AutoSize = true };
            chkEnable.Checked = AppConfiguration.EnableWebOverlayServer;
            chkEnable.CheckedChanged += (s, e) => AppConfiguration.EnableWebOverlayServer = chkEnable.Checked;

            var lblPort = new Label { Text = "Port:", Location = new Point(15, 55), AutoSize = true };
            var nudPort = new NumericUpDown { Location = new Point(60, 52), Minimum = 1024, Maximum = 65535, Value = AppConfiguration.WebOverlayPort, Width = 80 };
            nudPort.ValueChanged += (s, e) => AppConfiguration.WebOverlayPort = (int)nudPort.Value;

            grpWeb.Controls.Add(chkEnable);
            grpWeb.Controls.Add(lblPort);
            grpWeb.Controls.Add(nudPort);
            foreach (Control c in grpWeb.Controls) c.ForeColor = Color.FromArgb(31, 41, 55);
            tab.Controls.Add(grpWeb);
        }
    }
}
