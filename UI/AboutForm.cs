using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public class AboutForm : Form
    {
        private const string ABOUT_URL = "https://github.com/insert3coins/EliteDataRelay";

        public AboutForm()
        {
            // Use the designer-generated InitializeComponent
            // and then add our custom logic.
            InitializeComponent();
            LoadVersionInfo();
            SetupControls();
        }

        private void LoadVersionInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version != null ? string.Format("{0} {1}.{2}.{3}", Properties.Strings.About_VersionLabel, version.Major, version.Minor, version.Build) : Properties.Strings.About_VersionNotFound;

            // Check for informational version (e.g., from GitVersion)
            var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(productVersion) && version != null && productVersion != version.ToString())
            {
                versionString += $" ({productVersion})";
            }
            versionLabel.Text = versionString;
        }

        private void SetupControls()
        {
            // Link click event
            linkLabelGitHub.LinkClicked += (s, e) => OpenUrl(ABOUT_URL);

            // Close button event
            closeButton.Click += (s, e) => this.Close();

            // Make the version label clickable to copy to clipboard
            versionLabel.Cursor = Cursors.Hand;
            versionLabel.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(versionLabel.Text);
                    // Provide feedback to the user
                    var originalText = versionLabel.Text;
                    versionLabel.Text = Properties.Strings.Common_Copied;
                    var t = new System.Windows.Forms.Timer { Interval = 1500 };
                    t.Tick += (sender, args) =>
                    {
                        versionLabel.Text = originalText;
                        t.Stop();
                        t.Dispose();
                    };
                    t.Start();
                }
                catch (Exception ex)
                {
                    Logger.Info($"[AboutForm] Failed to copy to clipboard: {ex.Message}");
                }
            };
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(Properties.Strings.Error_OpenUrlFormat, ex.Message), Properties.Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // The designer will create a 'components' field to dispose
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // NOTE: This is typically in a .Designer.cs file.
        // I've included it here to provide a single, complete file for the new design.

        private System.ComponentModel.IContainer components = null!;
        private Label labelTitle = null!;
        private Label versionLabel = null!;
        private Label labelCopyright = null!;
        private LinkLabel linkLabelGitHub = null!;
        private Button closeButton = null!;
        private Label labelDescription = null!;

        private void InitializeComponent()
        {
            this.labelTitle = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.labelCopyright = new System.Windows.Forms.Label();
            this.linkLabelGitHub = new System.Windows.Forms.LinkLabel();
            this.closeButton = new System.Windows.Forms.Button();
            this.labelDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.labelTitle.Location = new System.Drawing.Point(12, 9);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(158, 25);
            this.labelTitle.Text = Properties.Strings.App_Title;
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.versionLabel.Location = new System.Drawing.Point(17, 43);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(81, 15);
            this.versionLabel.Text = Properties.Strings.About_VersionNotFound;
            // 
            // labelDescription
            // 
            this.labelDescription.Location = new System.Drawing.Point(17, 70);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(380, 75);
            this.labelDescription.Text = Properties.Strings.About_Description;
            // 
            // linkLabelGitHub
            // 
            this.linkLabelGitHub.AutoSize = true;
            this.linkLabelGitHub.Location = new System.Drawing.Point(17, 150);
            this.linkLabelGitHub.Name = "linkLabelGitHub";
            this.linkLabelGitHub.Size = new System.Drawing.Size(133, 15);
            this.linkLabelGitHub.TabStop = true;
            this.linkLabelGitHub.Text = Properties.Strings.About_ViewProject;
            // 
            // labelCopyright
            // 
            this.labelCopyright.AutoSize = true;
            this.labelCopyright.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelCopyright.Location = new System.Drawing.Point(17, 175);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(166, 15);
            this.labelCopyright.Text = "Copyright © 2024 insert3coins";
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(322, 201);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.Text = Properties.Strings.Common_OK;
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // AboutForm
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(414, 236);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.linkLabelGitHub);
            this.Controls.Add(this.labelCopyright);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelDescription);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Properties.Strings.About_WindowTitle;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}




