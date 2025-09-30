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
            ClientSize = new Size(464, 585);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // Main TabControl
            var tabControl = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 510,
            };

            var generalTabPage = new TabPage("General");
            var overlayTabPage = new TabPage("Overlay");
            var hotkeysTabPage = new TabPage("Hotkeys");
            var twitchTabPage = new TabPage("Twitch");

            tabControl.TabPages.Add(generalTabPage);
            tabControl.TabPages.Add(overlayTabPage);
            tabControl.TabPages.Add(hotkeysTabPage);
            tabControl.TabPages.Add(twitchTabPage);
            
            #region General Tab

            // GroupBox
            _grpOutputFormat = new GroupBox
            {
                Text = "Text File Output",
                Location = new Point(12, 12),
                Size = new Size(440, 298),
            };

            // Enable/Disable CheckBox
            _chkEnableFileOutput = new CheckBox
            {
                Text = "Enable text file output",
                Location = new Point(15, 24),
                AutoSize = true
            };
            _chkEnableFileOutput.CheckedChanged += OnEnableOutputCheckedChanged;

            // Description Label
            _lblDescription = new Label
            {
                Text = "Customize the format for the cargo.txt output file:",
                Location = new Point(15, 54),
                AutoSize = true
            };

            // Format TextBox
            _txtOutputFormat = new TextBox
            {
                Location = new Point(18, 70),
                Size = new Size(407, 20)
            };

            // Output File Name Label
            _lblOutputFileName = new Label
            {
                Text = "Output file name:",
                Location = new Point(15, 100),
                AutoSize = true
            };

            // Output File Name TextBox
            _txtOutputFileName = new TextBox
            {
                Location = new Point(18, 116),
                Size = new Size(407, 20)
            };

            // Output Directory Label
            _lblOutputDirectory = new Label
            {
                Text = "Output directory:",
                Location = new Point(15, 142),
                AutoSize = true
            };

            // Output Directory TextBox
            _txtOutputDirectory = new TextBox
            {
                Location = new Point(18, 158),
                Size = new Size(326, 20)
            };

            // Browse Button
            _btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(350, 157),
                Size = new Size(75, 22)
            };
            _btnBrowse.Click += OnBrowseClicked;

            // Placeholders Label
            _lblPlaceholders = new Label
            {
                Text = "Available placeholders:\n" +
                       "{count} - Total number of items in cargo\n" +
                       "{capacity} - Total cargo capacity (blank if unknown)\n" +
                       "{count_slash_capacity} - e.g., \"128/256\" or just \"128\" if capacity is unknown\n" +
                       "{items} - Single-line list of items, e.g., \"Gold (10) Silver (5)\"\n" +
                       "{items_multiline} - Multi-line list of items\n" +
                       "\\n - Newline character", // Note: Backslash needs to be escaped in C# string literal
                Location = new Point(15, 188),
                AutoSize = true
            };

            // Session Tracking GroupBox
            _grpSessionTracking = new GroupBox
            {
                Text = "Session Tracking",
                Location = new Point(12, 316),
                Size = new Size(440, 55),
            };
            _chkEnableSessionTracking = new CheckBox
            {
                Text = "Enable session tracking (credits, cargo, etc.)",
                Location = new Point(15, 20),
                AutoSize = true
            };
            _grpSessionTracking.Controls.Add(_chkEnableSessionTracking);

            // Add controls to the file output groupbox
            _grpOutputFormat.Controls.Add(_chkEnableFileOutput);
            _grpOutputFormat.Controls.Add(_lblDescription);
            _grpOutputFormat.Controls.Add(_txtOutputFormat);
            _grpOutputFormat.Controls.Add(_lblOutputDirectory);
            _grpOutputFormat.Controls.Add(_txtOutputDirectory);
            _grpOutputFormat.Controls.Add(_btnBrowse);
            _grpOutputFormat.Controls.Add(_lblOutputFileName);
            _grpOutputFormat.Controls.Add(_txtOutputFileName);
            _grpOutputFormat.Controls.Add(_lblPlaceholders);

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
                Size = new Size(440, 210),
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

            _chkShowSessionOnOverlay = new CheckBox
            {
                Text = "Show session stats on cargo overlay",
                Location = new Point(15, 145),
                AutoSize = true
            };
            _chkShowSessionOnOverlay.CheckedChanged += OnShowSessionCheckedChanged;

            // Reposition Overlays Button
            _btnRepositionOverlays = new Button
            {
                Text = "Reposition Overlays",
                Location = new Point(15, 175),
                Size = new Size(160, 23)
            };
            _btnRepositionOverlays.Click += OnRepositionOverlaysClicked;

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
                Size = new Size(410, 155),
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

            // Add controls to the General tab
            generalTabPage.Controls.Add(_grpOutputFormat);
            generalTabPage.Controls.Add(_grpSessionTracking);

            #endregion

            #region Overlay Tab

            // Add functionality controls to the overlay settings group
            _grpOverlaySettings.Controls.Add(_chkEnableLeftOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableRightOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableShipIconOverlay);
            _grpOverlaySettings.Controls.Add(_chkShowSessionOnOverlay);
            _grpOverlaySettings.Controls.Add(_btnRepositionOverlays);

            // Font GroupBox
            var grpFont = new GroupBox { Text = "Appearance: Font", Location = new Point(12, 225), Size = new Size(410, 80) };
            var lblCurrentFontHeader = new Label { Text = "Current Font:", Location = new Point(15, 25), AutoSize = true };
            _lblCurrentFont = new Label { Text = "Consolas, 11pt", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(100, 25), AutoSize = true };
            var btnChangeFont = new Button { Text = "Change Font...", Location = new Point(15, 45), Size = new Size(100, 23) };
            btnChangeFont.Click += OnChangeFontClicked;
            grpFont.Controls.Add(lblCurrentFontHeader);
            grpFont.Controls.Add(_lblCurrentFont);
            grpFont.Controls.Add(btnChangeFont);

            // Colors GroupBox
            var grpColors = new GroupBox { Text = "Appearance: Colors & Opacity", Location = new Point(12, 310), Size = new Size(410, 140) };
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
            _btnResetOverlaySettings = new Button { Text = "Reset All Overlay Settings", Location = new Point(12, 435), Size = new Size(180, 23) };
            _btnResetOverlaySettings.Click += OnResetOverlaySettingsClicked;

            // Add controls to the Overlay tab
            overlayTabPage.Controls.Add(_grpOverlaySettings);
            overlayTabPage.Controls.Add(grpFont);
            overlayTabPage.Controls.Add(grpColors);
            overlayTabPage.Controls.Add(_btnResetOverlaySettings);

            #endregion

            #region Hotkeys Tab

            // Add controls to the Hotkeys tab
            hotkeysTabPage.Controls.Add(_grpHotkeys);
            #endregion

            #region Twitch Tab

            // Twitch GroupBox
            var grpTwitch = new GroupBox
            {
                Text = "Twitch Integration",
                Location = new Point(12, 12),
                Size = new Size(440, 305),
            };

            _chkEnableTwitchIntegration = new CheckBox
            {
                Text = "Enable Twitch integration (chat bubbles, alerts)",
                Location = new Point(15, 25),
                AutoSize = true
            };
            _chkEnableTwitchIntegration.CheckedChanged += OnEnableTwitchIntegrationCheckedChanged;

            // Connection Settings GroupBox
            var grpTwitchConnection = new GroupBox
            {
                Text = "Connection Settings",
                Location = new Point(15, 55),
                Size = new Size(410, 140),
            };

            var lblChannel = new Label { Text = "Twitch Channel Name:", Location = new Point(15, 25), AutoSize = true };
            _txtTwitchChannel = new TextBox { Location = new Point(160, 22), Size = new Size(235, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lblClientId = new Label { Text = "Client ID:", Location = new Point(15, 53), AutoSize = true };
            _txtTwitchClientId = new TextBox { Location = new Point(160, 50), Size = new Size(235, 20), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lblSecret = new Label { Text = "Client Secret:", Location = new Point(15, 81), AutoSize = true };
            _txtTwitchClientSecret = new TextBox { Location = new Point(160, 78), Size = new Size(235, 20), PasswordChar = '*', Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lblLoginStatus = new Label { Text = "Status:", Location = new Point(15, 109), AutoSize = true };
            _lblTwitchLoginStatus = new Label { Text = "Not logged in", Location = new Point(160, 109), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };

            _btnLogoutOfTwitch = new Button
            {
                Text = "Logout",
                Location = new Point(280, 105),
                Size = new Size(65, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnLogoutOfTwitch.Click += OnLogoutOfTwitchClicked;

            _btnLoginToTwitch = new Button
            {
                Text = "Login with Twitch",
                Location = new Point(350, 105),
                Size = new Size(115, 25), // Adjusted size and location
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnLoginToTwitch.Click += OnLoginToTwitchClicked;

            grpTwitchConnection.Controls.Add(lblChannel);
            grpTwitchConnection.Controls.Add(_txtTwitchChannel);
            grpTwitchConnection.Controls.Add(lblClientId);
            grpTwitchConnection.Controls.Add(_txtTwitchClientId);
            grpTwitchConnection.Controls.Add(lblLoginStatus);
            grpTwitchConnection.Controls.Add(lblSecret);
            grpTwitchConnection.Controls.Add(_txtTwitchClientSecret);
            grpTwitchConnection.Controls.Add(_lblTwitchLoginStatus);
            grpTwitchConnection.Controls.Add(_btnLogoutOfTwitch);
            grpTwitchConnection.Controls.Add(_btnLoginToTwitch);

            // Features GroupBox
            var grpTwitchFeatures = new GroupBox
            {
                Text = "Enabled Features",
                Location = new Point(15, 205),
                Size = new Size(410, 150),
            };

            _chkEnableTwitchChatBubbles = new CheckBox
            {
                Text = "Show animated chat column",
                Location = new Point(15, 25),
                AutoSize = true
            };
            _chkEnableTwitchFollowerAlerts = new CheckBox
            {
                Text = "Show new follower alerts",
                Location = new Point(15, 50),
                AutoSize = true
            };
            _chkEnableTwitchRaidAlerts = new CheckBox
            {
                Text = "Show raid alerts",
                Location = new Point(15, 75),
                AutoSize = true
            };
            _chkEnableTwitchSubAlerts = new CheckBox
            {
                Text = "Show new subscriber alerts",
                Location = new Point(15, 100),
                AutoSize = true
            };

            _btnTestAlerts = new Button
            {
                Text = "Test Alerts...",
                Location = new Point(300, 115),
                Size = new Size(95, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _btnTestAlerts.Click += OnTestAlertsClicked;

            grpTwitchFeatures.Controls.Add(_chkEnableTwitchChatBubbles);
            grpTwitchFeatures.Controls.Add(_chkEnableTwitchFollowerAlerts);
            grpTwitchFeatures.Controls.Add(_chkEnableTwitchRaidAlerts);
            grpTwitchFeatures.Controls.Add(_chkEnableTwitchSubAlerts);
            grpTwitchFeatures.Controls.Add(_btnTestAlerts);
            grpTwitch.Controls.Add(_chkEnableTwitchIntegration);
            grpTwitch.Controls.Add(grpTwitchConnection);
            grpTwitch.Controls.Add(grpTwitchFeatures);
            twitchTabPage.Controls.Add(grpTwitch);

            #endregion

            // OK Button
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(296, 550) };
            _btnOk.Click += (sender, e) => SaveSettings();

            // Cancel Button
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(377, 550) };

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