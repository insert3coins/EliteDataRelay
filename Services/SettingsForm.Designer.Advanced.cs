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

            grp.Controls.Add(_chkFastStart);
            foreach (Control c in grp.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            tab.Controls.Add(grp);
        }
    }
}
