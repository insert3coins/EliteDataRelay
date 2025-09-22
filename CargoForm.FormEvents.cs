using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Services;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Form Events

        private void CargoForm_Load(object? sender, EventArgs e)
        {
            // Force overlay dragging to be disabled on every application start.
            // This ensures a predictable, non-draggable default state, regardless of saved settings.
            AppConfiguration.AllowOverlayDrag = false;

            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // Asynchronously check for updates on startup without blocking the UI.
            _ = UpdateCheckService.CheckForUpdatesAsync();

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
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Force overlay dragging to be disabled on every application start.
            // This ensures a predictable, non-draggable default state, regardless of saved settings.
            AppConfiguration.AllowOverlayDrag = false;

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