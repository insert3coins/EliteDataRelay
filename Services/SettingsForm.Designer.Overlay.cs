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
                Size = new Size(520, 270),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            // Enable Right Overlay CheckBox
            _chkEnableRightOverlay = new CheckBox
            {
                Text = "Enable cargo overlay",
                Location = new Point(15, 45),
                AutoSize = true
            };

            _chkEnableSessionOverlay = new CheckBox
            {
                Text = "Enable session overlay",
                Location = new Point(15, 70),
                AutoSize = true
            };

            // Enable Ship Icon Overlay CheckBox
            _chkEnableShipIconOverlay = new CheckBox
            {
                Text = "Enable ship icon overlay",
                Location = new Point(15, 95),
                AutoSize = true
            };

            // Enable Exploration Overlay CheckBox
            _chkEnableExplorationOverlay = new CheckBox
            {
                Text = "Enable exploration overlay",
                Location = new Point(15, 120),
                AutoSize = true
            };

            // Show Traffic: Exploration Overlay
            _chkTrafficExplorationOverlay = new CheckBox
            {
                Text = "Show traffic on exploration overlay",
                Location = new Point(15, 145 + 25 + 25),
                AutoSize = true
            };


            // Reposition Overlays Button
            _btnRepositionOverlays = new Button
            {
                Text = "Reposition Overlays",
                // Place after the traffic toggles to avoid overlap
                Location = new Point(15, 145 + 25 + 25 + 25 + 25),
                Size = new Size(160, 23)
            };
            _btnRepositionOverlays.Click += OnRepositionOverlaysClicked;

            // Add functionality controls to the overlay settings group
            _grpOverlaySettings.Controls.Add(_chkEnableLeftOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableRightOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableSessionOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableShipIconOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableExplorationOverlay);
            _grpOverlaySettings.Controls.Add(_chkTrafficExplorationOverlay);
            _grpOverlaySettings.Controls.Add(_btnRepositionOverlays);
            foreach (Control c in _grpOverlaySettings.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }

            // Dynamically size the overlay functionality group to content
            int overlayContentBottom = 0;
            foreach (Control c in _grpOverlaySettings.Controls)
            {
                if (c.Bottom > overlayContentBottom) overlayContentBottom = c.Bottom;
            }
            _grpOverlaySettings.Height = overlayContentBottom + 16; // padding

            // Font GroupBox
            var grpFont = new GroupBox { Text = "Appearance: Font", Location = new Point(12, _grpOverlaySettings.Bottom + 12), Size = new Size(520, 80), BackColor = Color.Transparent, ForeColor = Color.FromArgb(31, 41, 55) };
            var lblCurrentFontHeader = new Label { Text = "Current Font:", Location = new Point(15, 25), AutoSize = true };
            _lblCurrentFont = new Label { Text = "Consolas, 11pt", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(100, 25), AutoSize = true };
            var btnChangeFont = new Button { Text = "Change Font...", Location = new Point(15, 45), Size = new Size(100, 23) };
            btnChangeFont.Click += OnChangeFontClicked;
            grpFont.Controls.Add(lblCurrentFontHeader);
            grpFont.Controls.Add(_lblCurrentFont);
            grpFont.Controls.Add(btnChangeFont);
            foreach (Control c in grpFont.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            // Adjust font group height to its content
            int fontBottom = 0;
            foreach (Control c in grpFont.Controls)
            {
                if (c.Bottom > fontBottom) fontBottom = c.Bottom;
            }
            grpFont.Height = fontBottom + 16;

            // Colors GroupBox
            var grpColors = new GroupBox { Text = "Appearance: Colors && Opacity", Location = new Point(12, grpFont.Bottom + 12), Size = new Size(520, 175), BackColor = Color.Transparent, ForeColor = Color.FromArgb(31, 41, 55) };
            var lblTextColor = new Label { Text = "Text Color:", Location = new Point(15, 25), AutoSize = true };
            _pnlTextColor = new Panel { Location = new Point(130, 25), Size = new Size(23, 23), BorderStyle = BorderStyle.FixedSingle };
            var btnChangeTextColor = new Button { Text = "...", Location = new Point(160, 25), Size = new Size(23, 23) };
            btnChangeTextColor.Click += OnChangeTextColorClicked;
            var lblBackColor = new Label { Text = "Background Color:", Location = new Point(15, 60), AutoSize = true };
            _pnlBackColor = new Panel { Location = new Point(130, 60), Size = new Size(23, 23), BorderStyle = BorderStyle.FixedSingle };
            var btnChangeBackColor = new Button { Text = "...", Location = new Point(160, 60), Size = new Size(23, 23) };
            btnChangeBackColor.Click += OnChangeBackColorClicked;
            var lblBorderColor = new Label { Text = "Border Color:", Location = new Point(15, 95), AutoSize = true };
            _pnlBorderColor = new Panel { Location = new Point(130, 95), Size = new Size(23, 23), BorderStyle = BorderStyle.FixedSingle };
            var btnChangeBorderColor = new Button { Text = "...", Location = new Point(160, 95), Size = new Size(23, 23) };
            btnChangeBorderColor.Click += OnChangeBorderColorClicked;

            // Per-overlay border toggles
            int bx = 200; int by = 95; int bdy = 18;
            _chkShowBorderInfo = new CheckBox { Text = "Info", Location = new Point(bx, by), AutoSize = true };
            _chkShowBorderCargo = new CheckBox { Text = "Cargo", Location = new Point(bx + 60, by), AutoSize = true };
            _chkShowBorderSession = new CheckBox { Text = "Session", Location = new Point(bx + 140, by), AutoSize = true };
            _chkShowBorderShipIcon = new CheckBox { Text = "Ship Icon", Location = new Point(bx, by + bdy), AutoSize = true };
            _chkShowBorderExploration = new CheckBox { Text = "Exploration", Location = new Point(bx + 120, by + bdy), AutoSize = true };
            _chkShowBorderInfo.CheckedChanged += OnShowOverlayBordersChanged;
            _chkShowBorderCargo.CheckedChanged += OnShowOverlayBordersChanged;
            _chkShowBorderSession.CheckedChanged += OnShowOverlayBordersChanged;
            _chkShowBorderShipIcon.CheckedChanged += OnShowOverlayBordersChanged;
            _chkShowBorderExploration.CheckedChanged += OnShowOverlayBordersChanged;
            // Place opacity row below the per-overlay checkboxes
            int opacityY = by + bdy + 24; // add extra spacing after second row
            var lblOpacity = new Label { Text = "Background Opacity:", Location = new Point(15, opacityY), AutoSize = true };
            _trackBarOpacity = new TrackBar { AutoSize = false, Location = new Point(125, opacityY), Size = new Size(180, 16), Minimum = 10, Maximum = 100, TickFrequency = 10, TickStyle = TickStyle.None, Value = 85 };
            _trackBarOpacity.Scroll += OnOpacityTrackBarScroll;
            _lblOpacityValue = new Label { Text = "85%", AutoSize = false, Size = new Size(34, 14), TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font.FontFamily, 7f, FontStyle.Regular), ForeColor = Color.FromArgb(107, 114, 128), BackColor = Color.Transparent };
            grpColors.Controls.Add(lblTextColor);
            grpColors.Controls.Add(_pnlTextColor);
            grpColors.Controls.Add(btnChangeTextColor);
            grpColors.Controls.Add(lblBackColor);
            grpColors.Controls.Add(_pnlBackColor);
            grpColors.Controls.Add(btnChangeBackColor);
            grpColors.Controls.Add(lblBorderColor);
            grpColors.Controls.Add(_pnlBorderColor);
            grpColors.Controls.Add(btnChangeBorderColor);
            grpColors.Controls.Add(_chkShowBorderInfo);
            grpColors.Controls.Add(_chkShowBorderCargo);
            grpColors.Controls.Add(_chkShowBorderSession);
            grpColors.Controls.Add(_chkShowBorderShipIcon);
            grpColors.Controls.Add(_chkShowBorderExploration);
            grpColors.Controls.Add(lblOpacity);
            grpColors.Controls.Add(_trackBarOpacity);
            grpColors.Controls.Add(_lblOpacityValue);
            // Place the percentage label just to the right of the slider, vertically centered, minimal height
            var percentTop = _trackBarOpacity.Top + ((_trackBarOpacity.Height - _lblOpacityValue.Height) / 2);
            _lblOpacityValue.Location = new Point(_trackBarOpacity.Right + 8, percentTop);
            foreach (Control c in grpColors.Controls)
            {
                c.ForeColor = Color.FromArgb(31, 41, 55);
            }
            // Adjust colors group height to its content
            int colorsBottom = 0;
            foreach (Control c in grpColors.Controls)
            {
                if (c.Bottom > colorsBottom) colorsBottom = c.Bottom;
            }
            grpColors.Height = colorsBottom + 16;

            // Reset Button
            _btnResetOverlaySettings = new Button { Text = "Reset All Overlay Settings", Location = new Point(12, grpColors.Bottom + 12), Size = new Size(200, 28) };
            _btnResetOverlaySettings.Click += OnResetOverlaySettingsClicked;

            // Add controls to the Overlay tab
            overlayTabPage.Controls.Add(_grpOverlaySettings);
            overlayTabPage.Controls.Add(grpFont);
            overlayTabPage.Controls.Add(grpColors);
            overlayTabPage.Controls.Add(_btnResetOverlaySettings);
        }
    }
}
