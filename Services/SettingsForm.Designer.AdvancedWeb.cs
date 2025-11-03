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

            // Web Overlay settings removed
        }
    }
}
