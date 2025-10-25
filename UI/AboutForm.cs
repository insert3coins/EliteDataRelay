using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// A form to display application information, links, and disclaimers.
    public class AboutForm : Form
    {
        private Icon? _formIcon;

        private const string ABOUT_INFO = "Elite Data Relay";
        private const string ABOUT_URL = "https://github.com/insert3coins/EliteDataRelay";
        private const string LICENSE_URL = "https://github.com/insert3coins/EliteDataRelay/blob/main/LICENSE.txt";

        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form properties
            Text = "About Elite Data Relay";
            ClientSize = new Size(450, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            // Icon PictureBox
            var picIcon = new PictureBox
            {
                Location = new Point(15, 15),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            try
            {
                // Load the icon resource and create a bitmap for the PictureBox
                using (var iconStream = new MemoryStream(Properties.Resources.AppIcon))
                {
                    _formIcon = new Icon(iconStream, 256, 256);
                    picIcon.Image = _formIcon.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AboutForm] Failed to load icon: {ex.Message}");
            }

            // App Name Label
            var lblAppName = new Label
            {
                Text = ABOUT_INFO,
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(90, 20)
            };

            // Get version from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : "Version not found";

            // Version Label
            var lblVersion = new Label
            {
                Text = versionString,
                AutoSize = true,
                Location = new Point(90, 48)
            };

            // Copyright Label
            var lblCopyright = new Label
            {
                Text = $"Copyright © {DateTime.Now.Year} insert3coins",
                AutoSize = true,
                Location = new Point(90, 68)
            };

            // Project Link
            var linkProject = new LinkLabel { Text = "Project GitHub Page", AutoSize = true, Location = new Point(12, 100) };
            linkProject.LinkClicked += (s, e) => OpenUrl(ABOUT_URL);

            // License Link
            var linkLicense = new LinkLabel { Text = "View License (GPL-3.0)", AutoSize = true, Location = new Point(150, 90) };
            linkLicense.LinkClicked += (s, e) => OpenUrl(LICENSE_URL);

            // Disclaimer TextBox
            var txtDisclaimer = new TextBox
            {
                Text = "A lightweight Windows utility for players of Elite Dangerous. It monitors your in-game cargo in real-time, displaying the contents and total count in a simple interface and exporting the data to a text file for use with streaming overlays or other tools.",
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, 130),
                Size = new Size(426, 130),
                ScrollBars = ScrollBars.Vertical
            };

            // OK Button
            var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(363, 265), Size = new Size(75, 23) };

            // Add controls to form
            Controls.Add(picIcon);
            Controls.Add(lblAppName);
            Controls.Add(lblVersion);
            Controls.Add(lblCopyright);
            Controls.Add(linkProject);
            Controls.Add(linkLicense);
            Controls.Add(txtDisclaimer);
            Controls.Add(btnOk);

            AcceptButton = btnOk;
            CancelButton = btnOk;
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _formIcon?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}