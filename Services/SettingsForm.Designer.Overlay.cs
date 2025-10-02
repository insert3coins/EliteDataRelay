using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeOverlayTab(TabPage overlayTabPage)
        {
            // Enable Left Overlay CheckBox
            _chkEnableLeftOverlay = new CheckBox
            {
                Text = "Enable info overlay (CMDR, Ship, Balance)",
                Location = new Point(15, 20),
                AutoSize = true
            };

            // Overlay GroupBox
            _grpOverlaySettings = new GroupBox
            {
                Text = "Overlay Functionality",
                Location = new Point(12, 12),
                Size = new Size(440, 240),
            };

            // Enable Right Overlay CheckBox
            _chkEnableRightOverlay = new CheckBox
            {
                Text = "Enable cargo overlay",
                Location = new Point(15, 45),
                AutoSize = true
            };
            _chkEnableRightOverlay.CheckedChanged += OnEnableRightOverlayCheckedChanged;

            // Enable Ship Icon Overlay CheckBox
            _chkEnableShipIconOverlay = new CheckBox
            {
                Text = "Enable ship icon overlay",
                Location = new Point(15, 70),
                AutoSize = true
            };

            _chkEnableMiningOverlay = new CheckBox
            {
                Text = "Enable mining session overlay",
                Location = new Point(15, 95),
                AutoSize = true
            };

            _chkShowSessionOnOverlay = new CheckBox
            {
                Text = "Show session stats on cargo overlay",
                Location = new Point(15, 180),
                AutoSize = true
            };
            _chkShowSessionOnOverlay.CheckedChanged += OnShowSessionCheckedChanged;

            // Reposition Overlays Button
            _btnRepositionOverlays = new Button
            {
                Text = "Reposition Overlays",
                Location = new Point(15, 205),
                Size = new Size(160, 23)
            };
            _btnRepositionOverlays.Click += OnRepositionOverlaysClicked;

            // Add functionality controls to the overlay settings group
            _grpOverlaySettings.Controls.Add(_chkEnableLeftOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableRightOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableShipIconOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableMiningOverlay);
            _grpOverlaySettings.Controls.Add(_chkShowSessionOnOverlay);
            _grpOverlaySettings.Controls.Add(_btnRepositionOverlays);

            // Font GroupBox
            var grpFont = new GroupBox { Text = "Appearance: Font", Location = new Point(12, 255), Size = new Size(410, 80) };
            var lblCurrentFontHeader = new Label { Text = "Current Font:", Location = new Point(15, 25), AutoSize = true };
            _lblCurrentFont = new Label { Text = "Consolas, 11pt", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(100, 25), AutoSize = true };
            var btnChangeFont = new Button { Text = "Change Font...", Location = new Point(15, 45), Size = new Size(100, 23) };
            btnChangeFont.Click += OnChangeFontClicked;
            grpFont.Controls.Add(lblCurrentFontHeader);
            grpFont.Controls.Add(_lblCurrentFont);
            grpFont.Controls.Add(btnChangeFont);

            // Colors GroupBox
            var grpColors = new GroupBox { Text = "Appearance: Colors & Opacity", Location = new Point(12, 340), Size = new Size(410, 140) };
            var lblTextColor = new Label { Text = "Text Color:", Location = new Point(15, 25), AutoSize = true };
            _pnlTextColor = new Panel { Location = new Point(130, 25), Size = new Size(23, 23), BorderStyle = BorderStyle.FixedSingle };
            var btnChangeTextColor = new Button { Text = "...", Location = new Point(160, 25), Size = new Size(23, 23) };
            btnChangeTextColor.Click += OnChangeTextColorClicked;
            var lblBackColor = new Label { Text = "Background Color:", Location = new Point(15, 60), AutoSize = true };
            _pnlBackColor = new Panel { Location = new Point(130, 60), Size = new Size(23, 23), BorderStyle = BorderStyle.FixedSingle };
            var btnChangeBackColor = new Button { Text = "...", Location = new Point(160, 60), Size = new Size(23, 23) };
            btnChangeBackColor.Click += OnChangeBackColorClicked;
            var lblOpacity = new Label { Text = "Background Opacity:", Location = new Point(15, 95), AutoSize = true };
            _trackBarOpacity = new TrackBar { Location = new Point(125, 90), Size = new Size(200, 45), Minimum = 10, Maximum = 100, TickFrequency = 10, Value = 85 };
            _trackBarOpacity.Scroll += OnOpacityTrackBarScroll;
            _lblOpacityValue = new Label { Text = "85%", Location = new Point(330, 95), AutoSize = true };
            grpColors.Controls.Add(lblTextColor);
            grpColors.Controls.Add(_pnlTextColor);
            grpColors.Controls.Add(btnChangeTextColor);
            grpColors.Controls.Add(lblBackColor);
            grpColors.Controls.Add(_pnlBackColor);
            grpColors.Controls.Add(btnChangeBackColor);
            grpColors.Controls.Add(lblOpacity);
            grpColors.Controls.Add(_trackBarOpacity);
            grpColors.Controls.Add(_lblOpacityValue);

            // Reset Button
            _btnResetOverlaySettings = new Button { Text = "Reset All Overlay Settings", Location = new Point(12, 485), Size = new Size(180, 23) };
            _btnResetOverlaySettings.Click += OnResetOverlaySettingsClicked;

            // Add controls to the Overlay tab
            overlayTabPage.Controls.Add(_grpOverlaySettings);
            overlayTabPage.Controls.Add(grpFont);
            overlayTabPage.Controls.Add(grpColors);
            overlayTabPage.Controls.Add(_btnResetOverlaySettings);
        }
    }
}