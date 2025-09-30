using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class SettingsForm
    {
        private void OnChangeFontClicked(object? sender, EventArgs e)
        {
            using (var fontDialog = new FontDialog())
            {
                fontDialog.Font = _overlayFont;
                fontDialog.ShowColor = false;
                fontDialog.FontMustExist = true;
                fontDialog.AllowVerticalFonts = false;

                if (fontDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _overlayFont = fontDialog.Font;
                    UpdateAppearanceControls();
                }
            }
        }

        private void OnChangeTextColorClicked(object? sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = _overlayTextColor;
                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _overlayTextColor = colorDialog.Color;
                    UpdateAppearanceControls();
                }
            }
        }

        private void OnChangeBackColorClicked(object? sender, EventArgs e)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = _overlayBackColor;
                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _overlayBackColor = colorDialog.Color;
                    UpdateAppearanceControls();
                }
            }
        }

        private void OnOpacityTrackBarScroll(object? sender, EventArgs e)
        {
            _overlayOpacity = _trackBarOpacity.Value;
            UpdateAppearanceControls();
        }

        private void OnResetOverlaySettingsClicked(object? sender, EventArgs e)
        {
            // Set temporary fields to defaults for the UI
            _overlayFont = new Font("Consolas", 12F);
            _overlayTextColor = Color.Orange;
            _overlayBackColor = Color.FromArgb(200, 0, 0, 0);
            _overlayOpacity = 85;
            UpdateAppearanceControls();

            // Also update the static configuration directly to apply the changes live.
            AppConfiguration.OverlayFontName = _overlayFont.Name;
            AppConfiguration.OverlayFontSize = _overlayFont.Size;
            AppConfiguration.OverlayTextColor = _overlayTextColor;
            AppConfiguration.OverlayBackgroundColor = _overlayBackColor;
            AppConfiguration.OverlayOpacity = _overlayOpacity;

            // Also reset overlay positions
            AppConfiguration.InfoOverlayLocation = Point.Empty;
            AppConfiguration.CargoOverlayLocation = Point.Empty;
            AppConfiguration.ShipIconOverlayLocation = Point.Empty;

            // Raise the event to trigger a refresh of the live overlays.
            LiveSettingsChanged?.Invoke(this, EventArgs.Empty);

            MessageBox.Show(this, "All overlay settings (appearance and position) have been reset to defaults.\n\nClick OK to save this change, or Cancel to revert.", "Overlay Settings Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void OnEnableRightOverlayCheckedChanged(object? sender, EventArgs e)
        {
            _chkShowSessionOnOverlay.Enabled = _chkEnableRightOverlay.Checked;
            if (!_chkEnableRightOverlay.Checked)
            {
                _chkShowSessionOnOverlay.Checked = false;
            }
        }

        private void OnShowSessionCheckedChanged(object? sender, EventArgs e)
        {
            // This checkbox should only be enabled if the right overlay is also enabled.
            // If the user checks this, we can assume they want the right overlay on.
            if (_chkShowSessionOnOverlay.Checked && !_chkEnableRightOverlay.Checked)
            {
                _chkEnableRightOverlay.Checked = true;
            }
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

        private void OnEnableTwitchIntegrationCheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = _chkEnableTwitchIntegration.Checked;

            // Enable/disable connection and feature controls
            _txtTwitchChannel.Enabled = enabled;
            _btnLoginToTwitch.Enabled = enabled;
            _txtTwitchClientSecret.Enabled = enabled;
            _btnLogoutOfTwitch.Enabled = enabled; // Visibility is handled in UpdateTwitchLoginStatus
            _lblTwitchLoginStatus.Enabled = enabled;

            // Enable/disable the feature-specific checkboxes
            _chkEnableTwitchChatBubbles.Enabled = enabled;
            _chkEnableTwitchFollowerAlerts.Enabled = enabled;
            _chkEnableTwitchRaidAlerts.Enabled = enabled;
            _chkEnableTwitchSubAlerts.Enabled = enabled;

            // Also, if the user is logging in, we can assume they want to use their own channel name.
            if (enabled && !string.IsNullOrEmpty(AppConfiguration.TwitchUsername))
            {
                _txtTwitchChannel.Text = AppConfiguration.TwitchUsername;
            }
        }

        private async void OnLoginToTwitchClicked(object? sender, EventArgs e)
        {
            // Disable the form to prevent interaction during the async login process.
            this.Enabled = false;
            _lblTwitchLoginStatus.Text = "Waiting for browser...";
            _lblTwitchLoginStatus.ForeColor = Color.Blue;
            this.Update(); // Force the UI to repaint immediately.

            try
            {
                bool success = await TwitchAuthService.LoginToTwitch();
                if (success)
                {
                    // Pre-fill the channel name with the logged-in user's name and save it immediately.
                    _txtTwitchChannel.Text = AppConfiguration.TwitchUsername;
                    AppConfiguration.TwitchChannelName = AppConfiguration.TwitchUsername;
                    MessageBox.Show(this, $"Successfully logged in as {AppConfiguration.TwitchUsername}!", "Login Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "Twitch login was cancelled or failed.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                this.Enabled = true;
                UpdateTwitchLoginStatus();
            }
        }

        private void OnLogoutOfTwitchClicked(object? sender, EventArgs e)
        {
            AppConfiguration.ClearTwitchCredentials();
            UpdateTwitchLoginStatus();
            MessageBox.Show(this, "You have been logged out.", "Logout Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}