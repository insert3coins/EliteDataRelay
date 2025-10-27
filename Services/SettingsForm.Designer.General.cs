using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void InitializeGeneralTab(TabPage generalTabPage)
        {
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

            // Add controls to the General tab
            generalTabPage.Controls.Add(_grpOutputFormat);
            generalTabPage.Controls.Add(_grpSessionTracking);

            // Screenshots group (added after output/session)
            var grpScreenshots = new GroupBox
            {
                Text = "Screenshots",
                Location = new Point(12, 380),
                Size = new Size(440, 75)
            };
            var chkEnableScreenshotRenamer = new CheckBox
            {
                Text = "Auto-rename screenshots (system/body/timestamp)",
                Location = new Point(15, 25),
                AutoSize = true
            };
            chkEnableScreenshotRenamer.CheckedChanged += (s, e) => AppConfiguration.EnableScreenshotRenamer = chkEnableScreenshotRenamer.Checked;
            var lblFormat = new Label { Text = "Format:", Location = new Point(15, 50), AutoSize = true };
            var txtFormat = new TextBox { Location = new Point(70, 47), Size = new Size(355, 20) };
            txtFormat.TextChanged += (s, e) => AppConfiguration.ScreenshotRenameFormat = txtFormat.Text;
            // initialize values
            chkEnableScreenshotRenamer.Checked = AppConfiguration.EnableScreenshotRenamer;
            txtFormat.Text = AppConfiguration.ScreenshotRenameFormat;
            grpScreenshots.Controls.Add(chkEnableScreenshotRenamer);
            grpScreenshots.Controls.Add(lblFormat);
            grpScreenshots.Controls.Add(txtFormat);
            generalTabPage.Controls.Add(grpScreenshots);

            // Advanced moved to its own tab
        }
    }
}
