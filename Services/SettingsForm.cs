using System;
using System.Drawing;
using System.IO;
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

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            // Form Properties
            Text = "Settings";
            ClientSize = new Size(464, 319);
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
                Size = new Size(440, 268),
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
                Location = new Point(15, 158),
                AutoSize = true
            };

            // OK Button
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(296, 288) };
            _btnOk.Click += (sender, e) => SaveSettings();

            // Cancel Button
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(377, 288) };

            // Add Controls
            _grpOutputFormat.Controls.Add(_lblDescription);
            _grpOutputFormat.Controls.Add(_txtOutputFormat);
            _grpOutputFormat.Controls.Add(_lblOutputDirectory);
            _grpOutputFormat.Controls.Add(_txtOutputDirectory);
            _grpOutputFormat.Controls.Add(_btnBrowse);
            _grpOutputFormat.Controls.Add(_lblOutputFileName);
            _grpOutputFormat.Controls.Add(_txtOutputFileName);
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

        private void SaveSettings()
        {
            // --- Save all settings ---
            AppConfiguration.OutputFileFormat = _txtOutputFormat.Text;
            AppConfiguration.OutputFileName = _txtOutputFileName.Text;
            AppConfiguration.OutputDirectory = _txtOutputDirectory.Text;
            AppConfiguration.Save();
        }
    }
}