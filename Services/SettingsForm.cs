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
        // Legacy text file output controls removed
        private Button _btnOk = null!;
        private Button _btnCancel = null!;
        // Legacy output group removed
        private CheckBox _chkEnableLeftOverlay = null!;
        private CheckBox _chkEnableRightOverlay = null!;
        private CheckBox _chkEnableSessionOverlay = null!;
        private CheckBox _chkEnableShipIconOverlay = null!;
        private CheckBox _chkEnableExplorationOverlay = null!;
        // Next Jump overlay removed
        private GroupBox _grpOverlaySettings = null!;
        private CheckBox _chkTrafficExplorationOverlay = null!;
        // Next Jump traffic toggle removed
        private CheckBox _chkShowBorderInfo = null!;
        private CheckBox _chkShowBorderCargo = null!;
        private CheckBox _chkShowBorderSession = null!;
        private CheckBox _chkShowBorderShipIcon = null!;
        private CheckBox _chkShowBorderExploration = null!;
        // Next Jump border toggle removed
        private CheckBox _chkFastStart = null!;
        private GroupBox _grpSessionTracking = null!;
        private CheckBox _chkEnableSessionTracking = null!;
        // Hotkeys removed
        private Button _btnResetOverlaySettings = null!;
        private Button _btnRepositionOverlays = null!;
        private Label _lblCurrentFont = null!;
        private Panel _pnlTextColor = null!;
        private Panel _pnlBackColor = null!;
        private Panel _pnlBorderColor = null!;
        private TrackBar _trackBarOpacity = null!;
        private Label _lblOpacityValue = null!;
        // Web overlay UI (local to Advanced tab; created inline), add fields if later reused


        // Hotkeys removed

        // Temporary storage for appearance settings
        private Font _overlayFont = null!;
        private Color _overlayTextColor;
        private Color _overlayBackColor;
        private Color _overlayBorderColor;
        private int _overlayOpacity;
        private bool _overlayShowBorderInfo;
        private bool _overlayShowBorderCargo;
        private bool _overlayShowBorderSession;
        private bool _overlayShowBorderShipIcon;
        private bool _overlayShowBorderExploration;
        // removed: _overlayShowBorderJump

        // Cache for original settings to allow for cancellation of live changes
        private Font _originalOverlayFont = null!;
        private Color _originalOverlayTextColor;
        private Color _originalOverlayBackColor;
        private Color _originalOverlayBorderColor;
        private int _originalOverlayOpacity;
        private bool _originalShowBorderInfo;
        private bool _originalShowBorderCargo;
        private bool _originalShowBorderSession;
        private bool _originalShowBorderShipIcon;
        private bool _originalShowBorderExploration;
        // removed: _originalShowBorderJump
        private Point _originalInfoOverlayLocation;
        private Point _originalCargoOverlayLocation;
        private Point _originalSessionOverlayLocation;
        private Point _originalShipIconOverlayLocation;

        public event EventHandler? LiveSettingsChanged;

        public SettingsForm()
        {
            InitializeComponent();
            // Mining settings removed

            // When the form is closing, check if the user cancelled. If so, revert any live changes.
            this.FormClosing += SettingsForm_FormClosing;

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Legacy text file output removed
            _chkEnableSessionTracking.Checked = AppConfiguration.EnableSessionTracking;
            _chkEnableLeftOverlay.Checked = AppConfiguration.EnableInfoOverlay;
            _chkEnableSessionOverlay.Checked = AppConfiguration.EnableSessionOverlay;
            _chkEnableRightOverlay.Checked = AppConfiguration.EnableCargoOverlay;
            _chkEnableShipIconOverlay.Checked = AppConfiguration.EnableShipIconOverlay;
            _chkEnableExplorationOverlay.Checked = AppConfiguration.EnableExplorationOverlay;
            // Next Jump overlay removed
            _chkFastStart.Checked = AppConfiguration.FastStartSkipJournalHistory;
            // Hotkeys removed
            // Traffic toggles
            if (_chkTrafficExplorationOverlay != null) _chkTrafficExplorationOverlay.Checked = AppConfiguration.ShowTrafficOnExplorationOverlay;
            // Next Jump traffic removed
            // Hotkeys removed
            // Hotkeys removed

            // Load overlay appearance settings
            _overlayFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);
            _overlayTextColor = AppConfiguration.OverlayTextColor;
            _overlayBackColor = AppConfiguration.OverlayBackgroundColor;
            _overlayBorderColor = AppConfiguration.OverlayBorderColor;
            _overlayOpacity = AppConfiguration.OverlayOpacity;
            _overlayShowBorderInfo = AppConfiguration.OverlayShowBorderInfo;
            _overlayShowBorderCargo = AppConfiguration.OverlayShowBorderCargo;
            _overlayShowBorderSession = AppConfiguration.OverlayShowBorderSession;
            _overlayShowBorderShipIcon = AppConfiguration.OverlayShowBorderShipIcon;
            _overlayShowBorderExploration = AppConfiguration.OverlayShowBorderExploration;

            // Store original values for cancellation
            _originalOverlayFont = (Font)_overlayFont.Clone();
            _originalOverlayTextColor = _overlayTextColor;
            _originalOverlayBackColor = _overlayBackColor;
            _originalOverlayBorderColor = _overlayBorderColor;
            _originalOverlayOpacity = _overlayOpacity;
            _originalShowBorderInfo = _overlayShowBorderInfo;
            _originalShowBorderCargo = _overlayShowBorderCargo;
            _originalShowBorderSession = _overlayShowBorderSession;
            _originalShowBorderShipIcon = _overlayShowBorderShipIcon;
            _originalShowBorderExploration = _overlayShowBorderExploration;
            _originalInfoOverlayLocation = AppConfiguration.InfoOverlayLocation;
            _originalCargoOverlayLocation = AppConfiguration.CargoOverlayLocation;
            _originalSessionOverlayLocation = AppConfiguration.SessionOverlayLocation;
            _originalShipIconOverlayLocation = AppConfiguration.ShipIconOverlayLocation;

            UpdateAppearanceControls();
        }

        // Hotkeys removed

        private void UpdateAppearanceControls()
        {
            _lblCurrentFont.Text = $"{_overlayFont.Name}, {_overlayFont.SizeInPoints}pt";
            _pnlTextColor.BackColor = _overlayTextColor;
            _pnlBackColor.BackColor = _overlayBackColor;
            _pnlBorderColor.BackColor = _overlayBorderColor;
            _trackBarOpacity.Value = _overlayOpacity;
            _lblOpacityValue.Text = $"{_overlayOpacity}%";
            if (_chkShowBorderInfo != null) _chkShowBorderInfo.Checked = _overlayShowBorderInfo;
            if (_chkShowBorderCargo != null) _chkShowBorderCargo.Checked = _overlayShowBorderCargo;
            if (_chkShowBorderSession != null) _chkShowBorderSession.Checked = _overlayShowBorderSession;
            if (_chkShowBorderShipIcon != null) _chkShowBorderShipIcon.Checked = _overlayShowBorderShipIcon;
            if (_chkShowBorderExploration != null) _chkShowBorderExploration.Checked = _overlayShowBorderExploration;
        }

        private void SaveSettings()
        {
            // --- Save all settings ---
            // Removed legacy text file output persistence
            AppConfiguration.EnableSessionTracking = _chkEnableSessionTracking.Checked;
            AppConfiguration.EnableInfoOverlay = _chkEnableLeftOverlay.Checked;
            AppConfiguration.EnableSessionOverlay = _chkEnableSessionOverlay.Checked;
            AppConfiguration.EnableCargoOverlay = _chkEnableRightOverlay.Checked;
            AppConfiguration.EnableShipIconOverlay = _chkEnableShipIconOverlay.Checked;
            AppConfiguration.EnableExplorationOverlay = _chkEnableExplorationOverlay.Checked;
            AppConfiguration.FastStartSkipJournalHistory = _chkFastStart.Checked;
            // Hotkeys removed
            AppConfiguration.ShowTrafficOnExplorationOverlay = _chkTrafficExplorationOverlay.Checked;
            // Hotkeys removed
            // Removed legacy output directory persistence

            // Save appearance settings
            AppConfiguration.OverlayFontName = _overlayFont.Name;
            AppConfiguration.OverlayFontSize = _overlayFont.Size;
            AppConfiguration.OverlayTextColor = _overlayTextColor;
            AppConfiguration.OverlayBackgroundColor = _overlayBackColor;
            AppConfiguration.OverlayBorderColor = _overlayBorderColor;
            AppConfiguration.OverlayOpacity = _overlayOpacity;
            AppConfiguration.OverlayShowBorderInfo = _chkShowBorderInfo.Checked;
            AppConfiguration.OverlayShowBorderCargo = _chkShowBorderCargo.Checked;
            AppConfiguration.OverlayShowBorderSession = _chkShowBorderSession.Checked;
            AppConfiguration.OverlayShowBorderShipIcon = _chkShowBorderShipIcon.Checked;
            AppConfiguration.OverlayShowBorderExploration = _chkShowBorderExploration.Checked;

            AppConfiguration.Save();
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
            AppConfiguration.OverlayBorderColor = _originalOverlayBorderColor;
            AppConfiguration.OverlayOpacity = _originalOverlayOpacity;
            AppConfiguration.OverlayShowBorderInfo = _originalShowBorderInfo;
            AppConfiguration.OverlayShowBorderCargo = _originalShowBorderCargo;
            AppConfiguration.OverlayShowBorderShipIcon = _originalShowBorderShipIcon;
            AppConfiguration.OverlayShowBorderExploration = _originalShowBorderExploration;
            // Next Jump border removed

            LiveSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
