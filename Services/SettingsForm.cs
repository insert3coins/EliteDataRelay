using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    // Form for configuring application settings.
    public partial class SettingsForm : Form
    {
        private CheckBox _chkEnableFileOutput = null!;
        private TextBox _txtOutputFormat = null!;
        private TextBox _txtOutputFileName = null!;
        private TextBox _txtOutputDirectory = null!;
        private Button _btnBrowse = null!;
        private Label _lblDescription = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;
        private Label _lblPlaceholders = null!;
        private GroupBox _grpOutputFormat = null!;
        private Label _lblOutputDirectory = null!;
        private Label _lblOutputFileName = null!;
        private CheckBox _chkEnableLeftOverlay = null!;
        private CheckBox _chkEnableRightOverlay = null!;
        private CheckBox _chkEnableShipIconOverlay = null!;
        private GroupBox _grpOverlaySettings = null!;
        private CheckBox _chkShowSessionOnOverlay = null!;
        private GroupBox _grpSessionTracking = null!;
        private CheckBox _chkEnableSessionTracking = null!;
        private CheckBox _chkEnableHotkeys = null!;
        private GroupBox _grpHotkeys = null!;
        private TextBox _txtStartHotkey = null!;
        private TextBox _txtStopHotkey = null!;
        private TextBox _txtShowOverlayHotkey = null!;
        private TextBox _txtHideOverlayHotkey = null!;
        private Button _btnResetOverlaySettings = null!;
        private Button _btnRepositionOverlays = null!;
        private Label _lblCurrentFont = null!;
        private Panel _pnlTextColor = null!;
        private Panel _pnlBackColor = null!;
        private TrackBar _trackBarOpacity = null!;
        private Label _lblOpacityValue = null!;
        private CheckBox _chkEnableTwitchIntegration = null!;
        private CheckBox _chkEnableTwitchChatBubbles = null!;
        private CheckBox _chkEnableTwitchFollowerAlerts = null!;
        private CheckBox _chkEnableTwitchRaidAlerts = null!;
        private CheckBox _chkEnableTwitchSubAlerts = null!;
        private Button _btnLoginToTwitch = null!;
        private Button _btnLogoutOfTwitch = null!;
        private Label _lblTwitchLoginStatus = null!;
        private Button _btnTestAlerts = null!;
        private TextBox _txtTwitchChannel = null!;
        private TextBox _txtTwitchClientId = null!;
        private TextBox _txtTwitchClientSecret = null!;


        private Keys _startHotkey;
        private Keys _stopHotkey;
        private Keys _showOverlayHotkey;
        private Keys _hideOverlayHotkey;

        // Temporary storage for appearance settings
        private Font _overlayFont = null!;
        private Color _overlayTextColor;
        private Color _overlayBackColor;
        private int _overlayOpacity;

        // Cache for original settings to allow for cancellation of live changes
        private Font _originalOverlayFont = null!;
        private Color _originalOverlayTextColor;
        private Color _originalOverlayBackColor;
        private int _originalOverlayOpacity;
        private Point _originalInfoOverlayLocation;
        private Point _originalCargoOverlayLocation;
        private Point _originalShipIconOverlayLocation;

        public event EventHandler? LiveSettingsChanged;

        private readonly TwitchTestService _testService;

        public SettingsForm(TwitchTestService testService)
        {
            _testService = testService;
            InitializeComponent();

            // When the form is closing, check if the user cancelled. If so, revert any live changes.
            this.FormClosing += SettingsForm_FormClosing;

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Assumes a new boolean property 'EnableFileOutput' in AppConfiguration
            _chkEnableFileOutput.Checked = AppConfiguration.EnableFileOutput;
            _txtOutputFormat.Text = AppConfiguration.OutputFileFormat;
            _txtOutputFileName.Text = AppConfiguration.OutputFileName;
            _chkEnableSessionTracking.Checked = AppConfiguration.EnableSessionTracking;
            _chkEnableLeftOverlay.Checked = AppConfiguration.EnableInfoOverlay;
            _chkShowSessionOnOverlay.Checked = AppConfiguration.ShowSessionOnOverlay;
            _chkEnableRightOverlay.Checked = AppConfiguration.EnableCargoOverlay;
            _chkEnableShipIconOverlay.Checked = AppConfiguration.EnableShipIconOverlay;
            _chkEnableHotkeys.Checked = AppConfiguration.EnableHotkeys;
            _startHotkey = AppConfiguration.StartMonitoringHotkey;
            _stopHotkey = AppConfiguration.StopMonitoringHotkey;
            _showOverlayHotkey = AppConfiguration.ShowOverlayHotkey;
            _hideOverlayHotkey = AppConfiguration.HideOverlayHotkey;
            UpdateHotkeyText();
            _txtOutputDirectory.Text = AppConfiguration.OutputDirectory;
            OnEnableOutputCheckedChanged(null, EventArgs.Empty); // Set initial state of controls
            OnEnableRightOverlayCheckedChanged(null, EventArgs.Empty);
            OnEnableHotkeysCheckedChanged(null, EventArgs.Empty);

            // Load Twitch Settings
            _chkEnableTwitchIntegration.Checked = AppConfiguration.EnableTwitchIntegration;
            _chkEnableTwitchChatBubbles.Checked = AppConfiguration.EnableTwitchChatOverlay;
            _chkEnableTwitchFollowerAlerts.Checked = AppConfiguration.EnableTwitchFollowerAlerts;
            _chkEnableTwitchRaidAlerts.Checked = AppConfiguration.EnableTwitchRaidAlerts;
            _chkEnableTwitchSubAlerts.Checked = AppConfiguration.EnableTwitchSubAlerts;
            _txtTwitchChannel.Text = AppConfiguration.TwitchChannelName;
            _txtTwitchClientId.Text = AppConfiguration.TwitchClientId;
            _txtTwitchClientSecret.Text = AppConfiguration.TwitchClientSecret;
            UpdateTwitchLoginStatus();

            // Trigger the check changed event to set the initial enabled state of child controls
            OnEnableTwitchIntegrationCheckedChanged(null, EventArgs.Empty);

            // Load overlay appearance settings
            _overlayFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);
            _overlayTextColor = AppConfiguration.OverlayTextColor;
            _overlayBackColor = AppConfiguration.OverlayBackgroundColor;
            _overlayOpacity = AppConfiguration.OverlayOpacity;

            // Store original values for cancellation
            _originalOverlayFont = (Font)_overlayFont.Clone();
            _originalOverlayTextColor = _overlayTextColor;
            _originalOverlayBackColor = _overlayBackColor;
            _originalOverlayOpacity = _overlayOpacity;
            _originalInfoOverlayLocation = AppConfiguration.InfoOverlayLocation;
            _originalCargoOverlayLocation = AppConfiguration.CargoOverlayLocation;
            _originalShipIconOverlayLocation = AppConfiguration.ShipIconOverlayLocation;

            UpdateAppearanceControls();
        }

        private void UpdateHotkey(string? tag, Keys key)
        {
            switch (tag)
            {
                case "Start": _startHotkey = key; break;
                case "Stop": _stopHotkey = key; break;
                case "Show": _showOverlayHotkey = key; break;
                case "Hide": _hideOverlayHotkey = key; break;
            }
        }

        private void UpdateHotkeyText()
        {
            var converter = new KeysConverter();
            _txtStartHotkey.Text = _startHotkey == Keys.None ? "None" : converter.ConvertToString(_startHotkey);
            _txtStopHotkey.Text = _stopHotkey == Keys.None ? "None" : converter.ConvertToString(_stopHotkey);
            _txtShowOverlayHotkey.Text = _showOverlayHotkey == Keys.None ? "None" : converter.ConvertToString(_showOverlayHotkey);
            _txtHideOverlayHotkey.Text = _hideOverlayHotkey == Keys.None ? "None" : converter.ConvertToString(_hideOverlayHotkey);
        }

        private void UpdateAppearanceControls()
        {
            _lblCurrentFont.Text = $"{_overlayFont.Name}, {_overlayFont.SizeInPoints}pt";
            _pnlTextColor.BackColor = _overlayTextColor;
            _pnlBackColor.BackColor = _overlayBackColor;
            _trackBarOpacity.Value = _overlayOpacity;
            _lblOpacityValue.Text = $"{_overlayOpacity}%";
        }

        private void SaveSettings()
        {
            // --- Save all settings ---
            AppConfiguration.EnableFileOutput = _chkEnableFileOutput.Checked;
            AppConfiguration.OutputFileFormat = _txtOutputFormat.Text;
            AppConfiguration.OutputFileName = _txtOutputFileName.Text;
            AppConfiguration.EnableSessionTracking = _chkEnableSessionTracking.Checked;
            AppConfiguration.EnableInfoOverlay = _chkEnableLeftOverlay.Checked;
            AppConfiguration.ShowSessionOnOverlay = _chkShowSessionOnOverlay.Checked;
            AppConfiguration.EnableCargoOverlay = _chkEnableRightOverlay.Checked;
            AppConfiguration.EnableShipIconOverlay = _chkEnableShipIconOverlay.Checked;
            AppConfiguration.EnableHotkeys = _chkEnableHotkeys.Checked;
            AppConfiguration.StartMonitoringHotkey = _startHotkey;
            AppConfiguration.StopMonitoringHotkey = _stopHotkey;
            AppConfiguration.ShowOverlayHotkey = _showOverlayHotkey;
            AppConfiguration.HideOverlayHotkey = _hideOverlayHotkey;
            AppConfiguration.OutputDirectory = _txtOutputDirectory.Text;

            // Save Twitch Settings
            AppConfiguration.EnableTwitchIntegration = _chkEnableTwitchIntegration.Checked;
            AppConfiguration.EnableTwitchChatOverlay = _chkEnableTwitchChatBubbles.Checked;
            AppConfiguration.EnableTwitchFollowerAlerts = _chkEnableTwitchFollowerAlerts.Checked;
            AppConfiguration.EnableTwitchRaidAlerts = _chkEnableTwitchRaidAlerts.Checked;
            AppConfiguration.EnableTwitchSubAlerts = _chkEnableTwitchSubAlerts.Checked;
            AppConfiguration.TwitchChannelName = _txtTwitchChannel.Text;
            AppConfiguration.TwitchClientId = _txtTwitchClientId.Text;
            AppConfiguration.TwitchClientSecret = _txtTwitchClientSecret.Text;
            // The tokens are saved by the TwitchAuthService, so we don't need to save them here.
            // AppConfiguration.TwitchOAuthToken = _txtTwitchOAuthToken.Text;
            // AppConfiguration.TwitchRefreshToken = ...

            // Save appearance settings
            AppConfiguration.OverlayFontName = _overlayFont.Name;
            AppConfiguration.OverlayFontSize = _overlayFont.Size;
            AppConfiguration.OverlayTextColor = _overlayTextColor;
            AppConfiguration.OverlayBackgroundColor = _overlayBackColor;
            AppConfiguration.OverlayOpacity = _overlayOpacity;

            AppConfiguration.Save();
        }

        private void UpdateTwitchLoginStatus()
        {
            if (!string.IsNullOrEmpty(AppConfiguration.TwitchUsername))
            {
                _lblTwitchLoginStatus.Text = $"Logged in as: {AppConfiguration.TwitchUsername}";
                _lblTwitchLoginStatus.ForeColor = Color.Green;
                _btnLoginToTwitch.Visible = false;
                _btnLogoutOfTwitch.Enabled = true;
                _btnLogoutOfTwitch.Visible = true;
            }
            else
            {
                _lblTwitchLoginStatus.Text = "Not Logged In";
                _lblTwitchLoginStatus.ForeColor = Color.Red;
                _btnLoginToTwitch.Visible = true;
                _btnLogoutOfTwitch.Enabled = false;
                _btnLogoutOfTwitch.Visible = false;
            }
        }

        private void OnRepositionOverlaysClicked(object? sender, EventArgs e)
        {
            // Temporarily enable dragging so the user can move the overlays.
            AppConfiguration.AllowOverlayDrag = true;
            
            // Raise the event now to apply the temporary draggable state to the overlays.
            LiveSettingsChanged?.Invoke(this, EventArgs.Empty);

            this.Hide();

            // Show a small, non-modal dialog to allow interaction with the overlays.
            var repositionDialog = new RepositionDialog();
            repositionDialog.FormClosed += (s, args) =>
            {
                // Disable dragging when the repositioning dialog is closed.
                AppConfiguration.AllowOverlayDrag = false;
                // Raise an event to tell the main form to apply this live change.
                LiveSettingsChanged?.Invoke(this, EventArgs.Empty);

                // Check if the form has been disposed, which can happen if the main application is closed
                // while the reposition dialog is open.
                if (!this.IsDisposed)
                {
                    this.Show();
                    this.Activate();
                }
            };
            repositionDialog.Show(this.Owner); // Show non-modally
        }
    }

    public partial class SettingsForm
    {
        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.Cancel)
            {
                RevertLiveChanges();
            }
        }
        private void RevertLiveChanges()
        {
            AppConfiguration.OverlayFontName = _originalOverlayFont.Name;
            AppConfiguration.OverlayFontSize = _originalOverlayFont.Size;
            AppConfiguration.OverlayTextColor = _originalOverlayTextColor;
            AppConfiguration.OverlayBackgroundColor = _originalOverlayBackColor;
            AppConfiguration.OverlayOpacity = _originalOverlayOpacity;

            AppConfiguration.InfoOverlayLocation = _originalInfoOverlayLocation;
            AppConfiguration.CargoOverlayLocation = _originalCargoOverlayLocation;
            AppConfiguration.ShipIconOverlayLocation = _originalShipIconOverlayLocation;

            LiveSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}