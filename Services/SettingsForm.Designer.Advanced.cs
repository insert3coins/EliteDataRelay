using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeAdvancedTab(TabPage tab)
        {
            tab.BackColor = Color.White;

            var grp = new GroupBox
            {
                Text = Properties.Strings.Settings_AdvancedGroup_Title,
                Location = new Point(12, 12),
                Size = new Size(520, 80),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            _chkFastStart = new CheckBox
            {
                Text = Properties.Strings.Settings_FastStart_Label,
                Location = new Point(15, 25),
                AutoSize = true,
                Checked = AppConfiguration.FastStartSkipJournalHistory
            };
            _chkFastStart.CheckedChanged += (s, e) => AppConfiguration.FastStartSkipJournalHistory = _chkFastStart.Checked;

            grp.Controls.Add(_chkFastStart);

            // Verbose logging toggle
            var chkVerbose = new CheckBox
            {
                Text = Properties.Strings.Settings_VerboseLogging_Label,
                Location = new Point(15, 50),
                AutoSize = true,
                Checked = AppConfiguration.VerboseLogging
            };
            chkVerbose.CheckedChanged += (s, e) => AppConfiguration.VerboseLogging = chkVerbose.Checked;
            grp.Controls.Add(chkVerbose);

            // Diagnostics window button
            var btnDiagnostics = new Button
            {
                Text = Properties.Strings.Settings_OpenDiagnostics_Button,
                Location = new Point(15, 75),
                Size = new Size(150, 23)
            };
            btnDiagnostics.Click += (s, e) =>
            {
                using (var diag = new DiagnosticsForm())
                {
                    diag.ShowDialog();
                }
            };
            grp.Controls.Add(btnDiagnostics);
            foreach (Control c in grp.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            tab.Controls.Add(grp);
        }
    }
}

