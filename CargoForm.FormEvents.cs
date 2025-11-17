using EliteDataRelay.Configuration;
using System;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        private void CargoForm_Load(object? sender, EventArgs e)
        {
            // Load settings when the form loads
            AppConfiguration.Load();

            // Restore window state and location from settings
            if (AppConfiguration.WindowLocation != Point.Empty)
            {
                // Ensure the window is restored to a visible screen.
                // This prevents the window from being "lost" if the monitor configuration changes.
                bool isOnScreen = false;
                foreach (var screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.Contains(AppConfiguration.WindowLocation))
                    {
                        isOnScreen = true;
                        break;
                    }
                }

                if (isOnScreen)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = AppConfiguration.WindowLocation;
                }
            }
            var desiredState = AppConfiguration.WindowState;
            if (desiredState == FormWindowState.Minimized)
            {
                desiredState = FormWindowState.Normal;
            }
            this.WindowState = desiredState;

            // Hotkeys have been removed from the application.

            // Kick off one-time historical exploration import in the background.
            // No UI required; runs once and populates the exploration database.
            RunHistoricalExplorationImportIfNeeded();
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Hotkeys removed; nothing to unregister.

            // Ensure active monitoring/session is cleanly stopped so data (like history) is persisted.
            try
            {
                if (_fileMonitoringService.IsMonitoring)
                {
                    StopMonitoringInternal();
                }
                else if (AppConfiguration.EnableSessionTracking && _sessionTrackingService.IsMainSessionActive)
                {
                    _sessionTrackingService.StopSession();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[CargoForm] Failed to stop services on exit: {ex}");
            }

            // Save settings on exit, unless the user is canceling out of a prompt.
            if (e.CloseReason != CloseReason.None && e.CloseReason != CloseReason.TaskManagerClosing)
            {
                SaveOnExit();
            }
        }

        private void SaveOnExit()
        {
            // Save the window's current state and location before closing.
            var stateToPersist = this.WindowState;
            var locationToPersist = this.Location;

            if (stateToPersist != FormWindowState.Normal)
            {
                var bounds = this.RestoreBounds;
                locationToPersist = bounds.Location;
                stateToPersist = FormWindowState.Normal;
            }

            AppConfiguration.WindowState = stateToPersist;
            AppConfiguration.WindowLocation = locationToPersist;

            // Persist all settings to the settings.json file.
            AppConfiguration.Save();
        }
    }
}
