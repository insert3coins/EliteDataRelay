using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Form Events

        private void CargoForm_Load(object? sender, EventArgs e)
        {
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // Set initial visibility of session button based on settings
            _cargoFormUI.SetSessionButtonVisibility(AppConfiguration.EnableSessionTracking);

            // Restore window size and location from settings
            if (AppConfiguration.WindowSize.Width > 0 && AppConfiguration.WindowSize.Height > 0)
            {
                this.Size = AppConfiguration.WindowSize;
            }

            // Ensure the form is not loaded off-screen
            if (AppConfiguration.WindowLocation != Point.Empty)
            {
                bool isVisible = Screen.AllScreens.Any(s => s.WorkingArea.Contains(AppConfiguration.WindowLocation));

                if (isVisible)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = AppConfiguration.WindowLocation;
                }
            }

            // Restore window state, but don't start minimized.
            if (AppConfiguration.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }

            // Check if cargo file exists on startup
            if (!File.Exists(AppConfiguration.CargoPath))
            {
                MessageBox.Show(
                    $"Cargo.json not found.\nMake sure Elite Dangerous is running\nand the file is at:\n{AppConfiguration.CargoPath}",
                    "File not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Check if journal directory exists
            if (string.IsNullOrEmpty(_journalWatcherService.JournalDirectoryPath) || !Directory.Exists(_journalWatcherService.JournalDirectoryPath))
            {
                MessageBox.Show(
                    $"Journal directory not found.\nJournal watcher will be disabled.\nPath: {_journalWatcherService.JournalDirectoryPath}",
                    "Directory not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (AppConfiguration.EnableHotkeys)
            {
                UnregisterHotkeys();
            }

            // Save window state before closing.
            // Use RestoreBounds if the window is minimized or maximized.
            switch (this.WindowState)
            {
                case FormWindowState.Maximized:
                    AppConfiguration.WindowState = FormWindowState.Maximized;
                    AppConfiguration.WindowLocation = this.RestoreBounds.Location;
                    AppConfiguration.WindowSize = this.RestoreBounds.Size;
                    break;
                case FormWindowState.Normal:
                    AppConfiguration.WindowState = FormWindowState.Normal;
                    AppConfiguration.WindowLocation = this.Location;
                    AppConfiguration.WindowSize = this.Size;
                    break;
                default: // Minimized
                    AppConfiguration.WindowState = FormWindowState.Normal; // Don't save as minimized
                    AppConfiguration.WindowLocation = this.RestoreBounds.Location;
                    AppConfiguration.WindowSize = this.RestoreBounds.Size;
                    break;
            }
            AppConfiguration.Save();

            // If user closes window, hide to tray instead of exiting
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized; // This will trigger the hide logic in CargoFormUI
            }
            else
            {
                // Stop monitoring and dispose services on actual exit
                StopMonitoringInternal();
            }
        }

        #endregion
    }
}