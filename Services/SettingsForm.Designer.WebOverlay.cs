using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeWebOverlayTab(TabPage tab)
        {
            tab.BackColor = Color.White;

            var grp = new GroupBox
            {
                Text = "Browser-Source Overlay Server",
                Location = new Point(12, 12),
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

            grp.Controls.Add(chkEnable);
            grp.Controls.Add(lblPort);
            grp.Controls.Add(nudPort);
            foreach (Control c in grp.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            tab.Controls.Add(grp);
        }
    }
}
