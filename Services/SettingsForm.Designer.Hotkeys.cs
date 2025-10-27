using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeHotkeysTab(TabPage hotkeysTabPage)
        {
            // Enable Hotkeys CheckBox
            _chkEnableHotkeys = new CheckBox
            {
                Text = "Enable global hotkeys",
                Location = new Point(15, 20),
                AutoSize = true
            };
            _chkEnableHotkeys.CheckedChanged += OnEnableHotkeysCheckedChanged;

            // Hotkeys GroupBox
            _grpHotkeys = new GroupBox
            {
                Text = "Hotkeys",
                Location = new Point(12, 12),
                Size = new Size(520, 155),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            // Hotkey Labels and TextBoxes
            var lblStart = new Label { Text = "Start Monitoring:", Location = new Point(15, 50), AutoSize = true };
            _txtStartHotkey = CreateHotkeyInput(new Point(140, 47));
            _txtStartHotkey.Tag = "Start";

            var lblStop = new Label { Text = "Stop Monitoring:", Location = new Point(15, 75), AutoSize = true };
            _txtStopHotkey = CreateHotkeyInput(new Point(140, 72));
            _txtStopHotkey.Tag = "Stop";

            var lblShow = new Label { Text = "Show Overlay:", Location = new Point(15, 100), AutoSize = true };
            _txtShowOverlayHotkey = CreateHotkeyInput(new Point(140, 97));
            _txtShowOverlayHotkey.Tag = "Show";

            var lblHide = new Label { Text = "Hide Overlay:", Location = new Point(15, 125), AutoSize = true };
            _txtHideOverlayHotkey = CreateHotkeyInput(new Point(140, 122));
            _txtHideOverlayHotkey.Tag = "Hide";

            _grpHotkeys.Controls.Add(_chkEnableHotkeys);
            _grpHotkeys.Controls.Add(lblStart);
            _grpHotkeys.Controls.Add(_txtStartHotkey);
            _grpHotkeys.Controls.Add(lblStop);
            _grpHotkeys.Controls.Add(_txtStopHotkey);
            _grpHotkeys.Controls.Add(lblShow);
            _grpHotkeys.Controls.Add(_txtShowOverlayHotkey);
            _grpHotkeys.Controls.Add(lblHide);
            _grpHotkeys.Controls.Add(_txtHideOverlayHotkey);
            foreach (Control c in _grpHotkeys.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }

            // Add controls to the Hotkeys tab
            hotkeysTabPage.Controls.Add(_grpHotkeys);
        }
    }
}
