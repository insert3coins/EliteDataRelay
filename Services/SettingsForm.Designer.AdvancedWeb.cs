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
                Size = new Size(520, 150),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            var chkEnable = new CheckBox { Text = "Enable web overlay server", Location = new Point(15, 25), AutoSize = true };
            chkEnable.Checked = AppConfiguration.EnableWebOverlayServer;
            chkEnable.CheckedChanged += (s, e) => AppConfiguration.EnableWebOverlayServer = chkEnable.Checked;

            var lblPort = new Label { Text = "Port:", Location = new Point(15, 55), AutoSize = true };
            var nudPort = new NumericUpDown { Location = new Point(60, 52), Minimum = 1024, Maximum = 65535, Value = AppConfiguration.WebOverlayPort, Width = 80 };
            nudPort.ValueChanged += (s, e) => AppConfiguration.WebOverlayPort = (int)nudPort.Value;

            // Web overlay background opacity
            var lblOpacity = new Label { Text = "Background Opacity:", Location = new Point(15, 85), AutoSize = true };
            var trkOpacity = new TrackBar { AutoSize = false, Location = new Point(125, 80), Size = new Size(180, 16), Minimum = 10, Maximum = 100, TickFrequency = 10, TickStyle = TickStyle.None, Value = Math.Max(10, Math.Min(100, AppConfiguration.WebOverlayOpacity)) };
            var lblOpacityValue = new Label { AutoSize = false, Size = new Size(40, 14), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(tab.Font.FontFamily, 7f, FontStyle.Regular), ForeColor = Color.FromArgb(107, 114, 128) };
            lblOpacityValue.Text = $"{trkOpacity.Value}%";
            // position value label next to slider
            lblOpacityValue.Location = new Point(trkOpacity.Right + 8, trkOpacity.Top + ((trkOpacity.Height - lblOpacityValue.Height) / 2));
            trkOpacity.Scroll += (s, e) =>
            {
                AppConfiguration.WebOverlayOpacity = trkOpacity.Value;
                lblOpacityValue.Text = $"{trkOpacity.Value}%";
                lblOpacityValue.Location = new Point(trkOpacity.Right + 8, trkOpacity.Top + ((trkOpacity.Height - lblOpacityValue.Height) / 2));
            };

            grpWeb.Controls.Add(chkEnable);
            grpWeb.Controls.Add(lblPort);
            grpWeb.Controls.Add(nudPort);
            grpWeb.Controls.Add(lblOpacity);
            grpWeb.Controls.Add(trkOpacity);
            grpWeb.Controls.Add(lblOpacityValue);
            foreach (Control c in grpWeb.Controls) c.ForeColor = Color.FromArgb(31, 41, 55);
            tab.Controls.Add(grpWeb);

            // Verbose logging toggle (local to Advanced Web tab)
            var chkVerbose = new CheckBox
            {
                Text = Properties.Strings.Settings_VerboseLogging_Label,
                Location = new Point(12 + 15, grpWeb.Bottom + 12),
                AutoSize = true,
                Checked = AppConfiguration.VerboseLogging
            };
            chkVerbose.CheckedChanged += (s, e) => AppConfiguration.VerboseLogging = chkVerbose.Checked;
            tab.Controls.Add(chkVerbose);

            // Diagnostics window button
            var btnDiagnostics = new Button
            {
                Text = Properties.Strings.Settings_OpenDiagnostics_Button,
                Location = new Point(chkVerbose.Left, chkVerbose.Bottom + 8),
                Size = new Size(150, 23)
            };
            btnDiagnostics.Click += (s, e) =>
            {
                using (var diag = new DiagnosticsForm())
                {
                    diag.ShowDialog();
                }
            };
            tab.Controls.Add(btnDiagnostics);
        }
    }
}

