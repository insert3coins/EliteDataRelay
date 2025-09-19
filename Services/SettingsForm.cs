using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    /// Form for configuring application settings.
    public class SettingsForm : Form
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
        private GroupBox _grpOverlaySettings = null!;
        private GroupBox _grpOverlayPositioning = null!;
        private ComboBox _cmbVerticalAlignment = null!;
        private NumericUpDown _numVerticalOffset = null!;
        private NumericUpDown _numLeftHorizontalOffset = null!;
        private NumericUpDown _numRightHorizontalOffset = null!;
        private CheckBox _chkEnableHotkeys = null!;
        private GroupBox _grpHotkeys = null!;
        private TextBox _txtStartHotkey = null!;
        private TextBox _txtStopHotkey = null!;
        private TextBox _txtShowOverlayHotkey = null!;
        private TextBox _txtHideOverlayHotkey = null!;

        private Keys _startHotkey;
        private Keys _stopHotkey;
        private Keys _showOverlayHotkey;
        private Keys _hideOverlayHotkey;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            // Form Properties
            Text = "Settings";
            ClientSize = new Size(464, 735);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

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

            // Overlay GroupBox
            _grpOverlaySettings = new GroupBox
            {
                Text = "In-Game Overlay",
                Location = new Point(12, 316),
                Size = new Size(440, 75),
            };

            // Enable Left Overlay CheckBox
            _chkEnableLeftOverlay = new CheckBox
            {
                Text = "Enable left overlay (CMDR, Ship, Balance)",
                Location = new Point(15, 20),
                AutoSize = true
            };

            // Enable Right Overlay CheckBox
            _chkEnableRightOverlay = new CheckBox
            {
                Text = "Enable right overlay (Cargo)",
                Location = new Point(15, 45),
                AutoSize = true
            };

            // Overlay Positioning GroupBox
            _grpOverlayPositioning = new GroupBox
            {
                Text = "Overlay Positioning",
                Location = new Point(12, 397),
                Size = new Size(440, 130),
            };

            var lblVerticalAlignment = new Label { Text = "Vertical Alignment:", Location = new Point(15, 25), AutoSize = true };
            _cmbVerticalAlignment = new ComboBox { Location = new Point(140, 22), Size = new Size(120, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbVerticalAlignment.Items.AddRange(Enum.GetNames(typeof(OverlayVerticalAlignment)));

            var lblVerticalOffset = new Label { Text = "Vertical Offset:", Location = new Point(280, 25), AutoSize = true };
            _numVerticalOffset = new NumericUpDown { Location = new Point(370, 22), Size = new Size(55, 20), Minimum = -2000, Maximum = 2000 };

            var lblLeftHorizontal = new Label { Text = "Left Panel Offset (from left edge):", Location = new Point(15, 50), AutoSize = true };
            _numLeftHorizontalOffset = new NumericUpDown { Location = new Point(240, 47), Size = new Size(55, 20), Minimum = 0, Maximum = 2000 };

            var lblRightHorizontal = new Label { Text = "Right Panel Offset (from right edge):", Location = new Point(15, 75), AutoSize = true };
            _numRightHorizontalOffset = new NumericUpDown { Location = new Point(240, 72), Size = new Size(55, 20), Minimum = 0, Maximum = 2000 };

            var btnResetPosition = new Button { Text = "Reset to Default", Location = new Point(310, 71), Size = new Size(115, 23) };
            btnResetPosition.Click += OnResetPositionClicked;

            _grpOverlayPositioning.Controls.Add(lblVerticalAlignment);
            _grpOverlayPositioning.Controls.Add(_cmbVerticalAlignment);
            _grpOverlayPositioning.Controls.Add(lblVerticalOffset);
            _grpOverlayPositioning.Controls.Add(_numVerticalOffset);
            _grpOverlayPositioning.Controls.Add(lblLeftHorizontal);
            _grpOverlayPositioning.Controls.Add(_numLeftHorizontalOffset);
            _grpOverlayPositioning.Controls.Add(lblRightHorizontal);
            _grpOverlayPositioning.Controls.Add(_numRightHorizontalOffset);
            _grpOverlayPositioning.Controls.Add(btnResetPosition);

            // Hotkeys GroupBox
            _grpHotkeys = new GroupBox
            {
                Text = "Hotkeys",
                Location = new Point(12, 533),
                Size = new Size(440, 155),
            };

            // Enable Hotkeys CheckBox
            _chkEnableHotkeys = new CheckBox
            {
                Text = "Enable global hotkeys",
                Location = new Point(15, 20),
                AutoSize = true
            };
            _chkEnableHotkeys.CheckedChanged += OnEnableHotkeysCheckedChanged;

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

            // OK Button
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(296, 699) };
            _btnOk.Click += (sender, e) => SaveSettings();

            // Cancel Button
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(377, 699) };

            // Add Controls
            _grpOutputFormat.Controls.Add(_chkEnableFileOutput);
            _grpOutputFormat.Controls.Add(_lblDescription);
            _grpOutputFormat.Controls.Add(_txtOutputFormat);
            _grpOutputFormat.Controls.Add(_lblOutputDirectory);
            _grpOutputFormat.Controls.Add(_txtOutputDirectory);
            _grpOutputFormat.Controls.Add(_btnBrowse);
            _grpOutputFormat.Controls.Add(_lblOutputFileName);
            _grpOutputFormat.Controls.Add(_txtOutputFileName);
            _grpOutputFormat.Controls.Add(_lblPlaceholders);
            Controls.Add(_grpOutputFormat);
            _grpOverlaySettings.Controls.Add(_chkEnableLeftOverlay);
            _grpOverlaySettings.Controls.Add(_chkEnableRightOverlay);
            Controls.Add(_grpOverlaySettings);
            Controls.Add(_grpOverlayPositioning);
            Controls.Add(_grpHotkeys);
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

        private void OnEnableOutputCheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = _chkEnableFileOutput.Checked;

            _lblDescription.Enabled = enabled;
            _txtOutputFormat.Enabled = enabled;
            _lblOutputFileName.Enabled = enabled;
            _txtOutputFileName.Enabled = enabled;
            _lblOutputDirectory.Enabled = enabled;
            _txtOutputDirectory.Enabled = enabled;
            _btnBrowse.Enabled = enabled;
            _lblPlaceholders.Enabled = enabled;
        }

        private void OnEnableHotkeysCheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = _chkEnableHotkeys.Checked;
            foreach (Control c in _grpHotkeys.Controls)
            {
                if (c != _chkEnableHotkeys)
                {
                    c.Enabled = enabled;
                }
            }
        }

        private void LoadSettings()
        {
            // Assumes a new boolean property 'EnableFileOutput' in AppConfiguration
            _chkEnableFileOutput.Checked = AppConfiguration.EnableFileOutput;
            _txtOutputFormat.Text = AppConfiguration.OutputFileFormat;
            _txtOutputFileName.Text = AppConfiguration.OutputFileName;
            _chkEnableLeftOverlay.Checked = AppConfiguration.EnableLeftOverlay;
            _chkEnableRightOverlay.Checked = AppConfiguration.EnableRightOverlay;
            _cmbVerticalAlignment.SelectedItem = AppConfiguration.OverlayVerticalAlignment.ToString();
            _numVerticalOffset.Value = AppConfiguration.OverlayVerticalOffset;
            _numLeftHorizontalOffset.Value = AppConfiguration.LeftOverlayHorizontalOffset;
            _numRightHorizontalOffset.Value = AppConfiguration.RightOverlayHorizontalOffset;
            _chkEnableHotkeys.Checked = AppConfiguration.EnableHotkeys;
            _startHotkey = AppConfiguration.StartMonitoringHotkey;
            _stopHotkey = AppConfiguration.StopMonitoringHotkey;
            _showOverlayHotkey = AppConfiguration.ShowOverlayHotkey;
            _hideOverlayHotkey = AppConfiguration.HideOverlayHotkey;
            UpdateHotkeyText();
            _txtOutputDirectory.Text = AppConfiguration.OutputDirectory;
            OnEnableOutputCheckedChanged(null, EventArgs.Empty); // Set initial state of controls
            OnEnableHotkeysCheckedChanged(null, EventArgs.Empty);
        }

        private void OnBrowseClicked(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select an output directory";
                dialog.ShowNewFolderButton = true;

                // Set initial directory if the textbox has a valid path
                if (!string.IsNullOrEmpty(_txtOutputDirectory.Text) && Directory.Exists(_txtOutputDirectory.Text))
                {
                    dialog.SelectedPath = _txtOutputDirectory.Text;
                }
                else
                {
                    // Fallback to the application's base directory
                    dialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _txtOutputDirectory.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnResetPositionClicked(object? sender, EventArgs e)
        {
            _cmbVerticalAlignment.SelectedItem = AppConfiguration.DefaultOverlayVerticalAlignment.ToString();
            _numVerticalOffset.Value = AppConfiguration.DefaultOverlayVerticalOffset;
            _numLeftHorizontalOffset.Value = AppConfiguration.DefaultLeftOverlayHorizontalOffset;
            _numRightHorizontalOffset.Value = AppConfiguration.DefaultRightOverlayHorizontalOffset;
        }

        private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            var txt = sender as TextBox;
            if (txt == null) return;

            // Clear hotkey on Delete or Backspace
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                UpdateHotkey(txt.Tag as string, Keys.None);
                UpdateHotkeyText();
                return;
            }

            // Ignore modifier-only key presses
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
            {
                return;
            }

            UpdateHotkey(txt.Tag as string, e.KeyData);
            UpdateHotkeyText();
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

        private void SaveSettings()
        {
            // --- Save all settings ---
            AppConfiguration.EnableFileOutput = _chkEnableFileOutput.Checked;
            AppConfiguration.OutputFileFormat = _txtOutputFormat.Text;
            AppConfiguration.OutputFileName = _txtOutputFileName.Text;
            AppConfiguration.EnableLeftOverlay = _chkEnableLeftOverlay.Checked;
            AppConfiguration.EnableRightOverlay = _chkEnableRightOverlay.Checked;
            AppConfiguration.OverlayVerticalAlignment = (OverlayVerticalAlignment)Enum.Parse(typeof(OverlayVerticalAlignment), _cmbVerticalAlignment.Text);
            AppConfiguration.OverlayVerticalOffset = (int)_numVerticalOffset.Value;
            AppConfiguration.LeftOverlayHorizontalOffset = (int)_numLeftHorizontalOffset.Value;
            AppConfiguration.RightOverlayHorizontalOffset = (int)_numRightHorizontalOffset.Value;
            AppConfiguration.EnableHotkeys = _chkEnableHotkeys.Checked;
            AppConfiguration.StartMonitoringHotkey = _startHotkey;
            AppConfiguration.StopMonitoringHotkey = _stopHotkey;
            AppConfiguration.ShowOverlayHotkey = _showOverlayHotkey;
            AppConfiguration.HideOverlayHotkey = _hideOverlayHotkey;
            AppConfiguration.OutputDirectory = _txtOutputDirectory.Text;
            AppConfiguration.Save();
        }
    }
}