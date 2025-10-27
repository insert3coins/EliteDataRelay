using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
    {
    public partial class SettingsForm
    {
        private void InitializeComponent()
        {
            // Form Properties
            Text = "Settings";
            ClientSize = new Size(464, 680);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // Main TabControl
            var tabControl = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 605,
            };

            var generalTabPage = new TabPage("General");
            var overlayTabPage = new TabPage("Overlay");
            var hotkeysTabPage = new TabPage("Hotkeys");
            var advancedTabPage = new TabPage("Advanced");
            var webOverlayTabPage = new TabPage("Web Overlay");
            
            tabControl.TabPages.Add(generalTabPage);
            tabControl.TabPages.Add(overlayTabPage);
            tabControl.TabPages.Add(hotkeysTabPage);
            tabControl.TabPages.Add(advancedTabPage);
            tabControl.TabPages.Add(webOverlayTabPage);

            // Initialize each tab's content from the new partial classes
            InitializeGeneralTab(generalTabPage);
            InitializeOverlayTab(overlayTabPage);
            InitializeHotkeysTab(hotkeysTabPage);
            InitializeAdvancedTab(advancedTabPage);
            InitializeWebOverlayTab(webOverlayTabPage);

            // OK Button
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(296, 645) };
            _btnOk.Click += (sender, e) => SaveSettings();

            // Cancel Button
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(377, 645) };

            // Add Controls
            Controls.Add(tabControl);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private TextBox CreateHotkeyInput(Point location)
        {
            var txt = new TextBox
            {
                Location = location,
                Size = new Size(285, 20),
                ReadOnly = true,
                Text = "None"
            };
            txt.KeyDown += OnHotkeyKeyDown;
            return txt;
        }
    }
}
