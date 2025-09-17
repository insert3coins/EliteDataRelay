using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;

namespace EliteCargoMonitor.UI
{
    /// <summary>
    /// Form for configuring application settings.
    /// </summary>
    public class SettingsForm : Form
    {
        private TextBox _txtOutputFormat = null!;
        private Label _lblShipDesigns = null!;
        private TextBox _txtShipDesigns = null!;
        private TextBox _txtOutputFileName = null!;
        private TextBox _txtOutputDirectory = null!;
        private Button _btnBrowse = null!;
        private Label _lblDescription = null!;
        private Button _btnRestoreShips = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;
        private Label _lblPlaceholders = null!;
        private GroupBox _grpOutputFormat = null!;
        private Label _lblOutputDirectory = null!;
        private CheckBox _chkUseShipPrefix = null!;
        private Label _lblOutputFileName = null!;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            // Form Properties
            Text = "Settings";
            ClientSize = new Size(464, 471);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // GroupBox
            _grpOutputFormat = new GroupBox
            {
                Text = "Text File Output Format",
                Location = new Point(12, 12),
                Size = new Size(440, 420),
            };

            // Description Label
            _lblDescription = new Label
            {
                Text = "Customize the format for the cargo.txt output file:",
                Location = new Point(15, 24),
                AutoSize = true
            };

            // Format TextBox
            _txtOutputFormat = new TextBox
            {
                Location = new Point(18, 40),
                Size = new Size(407, 20)
            };

            // Output File Name Label
            _lblOutputFileName = new Label
            {
                Text = "Output file name:",
                Location = new Point(15, 70),
                AutoSize = true
            };

            // Output File Name TextBox
            _txtOutputFileName = new TextBox
            {
                Location = new Point(18, 86),
                Size = new Size(407, 20)
            };

            // Output Directory Label
            _lblOutputDirectory = new Label
            {
                Text = "Output directory:",
                Location = new Point(15, 112),
                AutoSize = true
            };

            // Output Directory TextBox
            _txtOutputDirectory = new TextBox
            {
                Location = new Point(18, 128),
                Size = new Size(326, 20)
            };

            // Browse Button
            _btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(350, 127),
                Size = new Size(75, 22)
            };
            _btnBrowse.Click += OnBrowseClicked;

            // Ship Prefix CheckBox
            _chkUseShipPrefix = new CheckBox
            {
                Text = "Use ship designs as line prefixes in main window",
                Location = new Point(18, 158),
                AutoSize = true
            };

            // New Label for Ship Designs
            _lblShipDesigns = new Label
            {
                Text = "Ship prefixes (one per line, max 8 characters each):",
                Location = new Point(15, 188),
                AutoSize = true
            };

            // New TextBox for Ship Designs
            _txtShipDesigns = new TextBox
            {
                Location = new Point(18, 204),
                Size = new Size(407, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true
            };

            // Restore Defaults Button for Ship Designs
            _btnRestoreShips = new Button
            {
                Text = "Restore Defaults",
                Location = new Point(310, 184),
                Size = new Size(115, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnRestoreShips.Click += OnRestoreShipsClicked;

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
                Location = new Point(15, 294),
                AutoSize = true
            };

            // OK Button
            _btnOk = new Button { Text = "OK", Location = new Point(296, 440) };
            _btnOk.Click += OnOkClicked;

            // Cancel Button
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(377, 440) };

            // Add Controls
            _grpOutputFormat.Controls.Add(_lblDescription);
            _grpOutputFormat.Controls.Add(_txtOutputFormat);
            _grpOutputFormat.Controls.Add(_lblOutputDirectory);
            _grpOutputFormat.Controls.Add(_txtOutputDirectory);
            _grpOutputFormat.Controls.Add(_btnBrowse);
            _grpOutputFormat.Controls.Add(_lblOutputFileName);
            _grpOutputFormat.Controls.Add(_txtOutputFileName);
            _grpOutputFormat.Controls.Add(_chkUseShipPrefix);
            _grpOutputFormat.Controls.Add(_lblShipDesigns);
            _grpOutputFormat.Controls.Add(_txtShipDesigns);
            _grpOutputFormat.Controls.Add(_btnRestoreShips);
            _grpOutputFormat.Controls.Add(_lblPlaceholders);
            Controls.Add(_grpOutputFormat);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void LoadSettings()
        {
            _txtOutputFormat.Text = AppConfiguration.OutputFileFormat;
            _txtOutputFileName.Text = AppConfiguration.OutputFileName;
            _txtOutputDirectory.Text = AppConfiguration.OutputDirectory;
            _chkUseShipPrefix.Checked = AppConfiguration.UseShipPrefix;
            _txtShipDesigns.Text = string.Join(Environment.NewLine, AppConfiguration.ShipDesigns);
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

        private void OnRestoreShipsClicked(object? sender, EventArgs e)
        {
            var defaultShips = AppConfiguration.GetDefaultShipDesigns();
            _txtShipDesigns.Text = string.Join(Environment.NewLine, defaultShips);
        }

        private void OnOkClicked(object? sender, EventArgs e)
        {
            if (TrySaveSettings())
            {
                DialogResult = DialogResult.OK;
            }
        }

        private bool TrySaveSettings()
        {
            // --- Validation ---
            var lines = _txtShipDesigns.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(l => l.TrimEnd())
                                            .ToList();

            foreach (var line in lines)
            {
                if (line.Length > 8)
                {
                    MessageBox.Show(
                        this,
                        $"The ship design \"{line}\" is longer than the maximum of 8 characters.",
                        "Invalid Ship Design",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _txtShipDesigns.Focus();
                    return false; // Indicate failure
                }
            }

            // --- Save all settings ---
            AppConfiguration.OutputFileFormat = _txtOutputFormat.Text;
            AppConfiguration.OutputFileName = _txtOutputFileName.Text;
            AppConfiguration.OutputDirectory = _txtOutputDirectory.Text;
            AppConfiguration.UseShipPrefix = _chkUseShipPrefix.Checked;
            AppConfiguration.ShipDesigns = lines;
            AppConfiguration.Save();
            return true;
        }
    }
}