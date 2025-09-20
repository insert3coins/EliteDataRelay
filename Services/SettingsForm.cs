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
        private CheckBox _chkEnableMaterialsOverlay = null!;
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
        private CheckBox _chkPinMaterialsMode = null!;
        private CheckedListBox _clbPinnedMaterials = null!;

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
        private Point _originalLeftOverlayLocation;
        private Point _originalRightOverlayLocation;
        private Point _originalMaterialsOverlayLocation;

        public event EventHandler? LiveSettingsChanged;

        public SettingsForm()
        {
            InitializeComponent();

            // When the form is closing, check if the user cancelled. If so, revert any live changes.
            this.FormClosing += SettingsForm_FormClosing;

            PopulateMaterialsList();
            LoadSettings();
        }

        private void PopulateMaterialsList()
        {
            var materials = MaterialDataService.GetAll();
            _clbPinnedMaterials.Items.Clear();
            foreach (var material in materials)
            {
                // Store the non-localised name in the item's tag for saving
                _clbPinnedMaterials.Items.Add($"{material.LocalisedName} (G{material.Grade})", false);
            }
        }

        private void LoadSettings()
        {
            // Assumes a new boolean property 'EnableFileOutput' in AppConfiguration
            _chkEnableFileOutput.Checked = AppConfiguration.EnableFileOutput;
            _txtOutputFormat.Text = AppConfiguration.OutputFileFormat;
            _txtOutputFileName.Text = AppConfiguration.OutputFileName;
            _chkEnableSessionTracking.Checked = AppConfiguration.EnableSessionTracking;
            _chkEnableLeftOverlay.Checked = AppConfiguration.EnableLeftOverlay;
            _chkShowSessionOnOverlay.Checked = AppConfiguration.ShowSessionOnOverlay;
            _chkEnableRightOverlay.Checked = AppConfiguration.EnableRightOverlay;
            _chkEnableMaterialsOverlay.Checked = AppConfiguration.EnableMaterialsOverlay;
            _chkPinMaterialsMode.Checked = AppConfiguration.PinMaterialsMode;
            _chkEnableHotkeys.Checked = AppConfiguration.EnableHotkeys;
            _startHotkey = AppConfiguration.StartMonitoringHotkey;
            _stopHotkey = AppConfiguration.StopMonitoringHotkey;
            _showOverlayHotkey = AppConfiguration.ShowOverlayHotkey;
            _hideOverlayHotkey = AppConfiguration.HideOverlayHotkey;
            UpdateHotkeyText();
            _txtOutputDirectory.Text = AppConfiguration.OutputDirectory;
            OnEnableOutputCheckedChanged(null, EventArgs.Empty); // Set initial state of controls
            OnEnableRightOverlayCheckedChanged(null, EventArgs.Empty);
            _clbPinnedMaterials.Enabled = _chkPinMaterialsMode.Checked;
            OnEnableHotkeysCheckedChanged(null, EventArgs.Empty);

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
            _originalLeftOverlayLocation = AppConfiguration.LeftOverlayLocation;
            _originalRightOverlayLocation = AppConfiguration.RightOverlayLocation;
            _originalMaterialsOverlayLocation = AppConfiguration.MaterialsOverlayLocation;

            UpdateAppearanceControls();

            // Check the pinned materials
            var pinned = AppConfiguration.PinnedMaterials.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            var allMaterials = MaterialDataService.GetAll().ToList();
            for (int i = 0; i < _clbPinnedMaterials.Items.Count; i++)
            {
                if (i < allMaterials.Count)
                {
                    var materialName = allMaterials[i].Name;
                    _clbPinnedMaterials.SetItemChecked(i, pinned.Contains(materialName));
                }
            }
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
            AppConfiguration.EnableLeftOverlay = _chkEnableLeftOverlay.Checked;
            AppConfiguration.ShowSessionOnOverlay = _chkShowSessionOnOverlay.Checked;
            AppConfiguration.EnableRightOverlay = _chkEnableRightOverlay.Checked;
            AppConfiguration.EnableMaterialsOverlay = _chkEnableMaterialsOverlay.Checked;
            AppConfiguration.PinMaterialsMode = _chkPinMaterialsMode.Checked;
            AppConfiguration.EnableHotkeys = _chkEnableHotkeys.Checked;
            AppConfiguration.StartMonitoringHotkey = _startHotkey;
            AppConfiguration.StopMonitoringHotkey = _stopHotkey;
            AppConfiguration.ShowOverlayHotkey = _showOverlayHotkey;
            AppConfiguration.HideOverlayHotkey = _hideOverlayHotkey;
            AppConfiguration.OutputDirectory = _txtOutputDirectory.Text;

            // Save pinned materials
            var pinnedMaterials = new List<string>();
            var allMaterials = MaterialDataService.GetAll().ToList();
            for (int i = 0; i < _clbPinnedMaterials.CheckedItems.Count; i++)
            {
                var checkedItem = _clbPinnedMaterials.CheckedItems[i];
                if (checkedItem != null)
                {
                    int originalIndex = _clbPinnedMaterials.Items.IndexOf(checkedItem);
                    if (originalIndex >= 0 && originalIndex < allMaterials.Count)
                    {
                        pinnedMaterials.Add(allMaterials[originalIndex].Name);
                    }
                }
            }
            AppConfiguration.PinnedMaterials = pinnedMaterials;

            // Save appearance settings
            AppConfiguration.OverlayFontName = _overlayFont.Name;
            AppConfiguration.OverlayFontSize = _overlayFont.Size;
            AppConfiguration.OverlayTextColor = _overlayTextColor;
            AppConfiguration.OverlayBackgroundColor = _overlayBackColor;
            AppConfiguration.OverlayOpacity = _overlayOpacity;

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
            AppConfiguration.OverlayOpacity = _originalOverlayOpacity;

            AppConfiguration.LeftOverlayLocation = _originalLeftOverlayLocation;
            AppConfiguration.RightOverlayLocation = _originalRightOverlayLocation;
            AppConfiguration.MaterialsOverlayLocation = _originalMaterialsOverlayLocation;

            LiveSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}