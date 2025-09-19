using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region UI Event Handlers

        private void OnStartClicked(object? sender, EventArgs e)
        {
            StartMonitoring();
        }

        private void OnStopClicked(object? sender, EventArgs e)
        {
            _soundService.PlayStopSound();
            StopMonitoringInternal();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            // Open the dedicated About form instead of a simple MessageBox.
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    ApplyLiveSettingsChanges();
                }
            }
        }

        // Applies settings changes that need to take effect immediately,
        // such as hotkeys and overlay visibility.
        private void ApplyLiveSettingsChanges()
        {
            // Unregister any existing hotkeys before re-registering, to handle changes.
            UnregisterHotkeys();
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // If monitoring is active, refresh the overlay to apply visibility changes.
            if (_fileMonitoringService.IsMonitoring)
            {
                _cargoFormUI.RefreshOverlay();
                RepopulateOverlay();
            }

            // Update Session button visibility
            _cargoFormUI.SetSessionButtonVisibility(AppConfiguration.EnableSessionTracking);
        }

        private void OnSessionClicked(object? sender, EventArgs e)
        {
            // Lazily create the form if it doesn't exist or has been disposed.
            if (_sessionSummaryForm == null || _sessionSummaryForm.IsDisposed)
            {
                _sessionSummaryForm = new SessionSummaryForm(_sessionTrackingService);
            }
            _sessionSummaryForm.Show();
            _sessionSummaryForm.Activate();
        }

        // Periodically checks if the game process is still running and stops monitoring if it's not.
        private void OnGameProcessCheck(object? sender, EventArgs e)
        {
            if (!_fileMonitoringService.IsMonitoring) return;

            var gameProcesses = Process.GetProcessesByName("EliteDangerous64");
            var gameProcess = gameProcesses.FirstOrDefault();

            try
            {
                bool shouldStop = false;
                if (gameProcess == null)
                {
                    shouldStop = true;
                }
                else
                {
                    // This block safely checks properties of a process that might exit at any moment.
                    try
                    {
                        // The HasExited property is more reliable, and checking MainWindowHandle ensures we stop if the window closes.
                        if (gameProcess.HasExited || gameProcess.MainWindowHandle == IntPtr.Zero)
                        {
                            shouldStop = true;
                        }
                    }
                    catch
                    {
                        // If accessing properties throws, the process has likely exited.
                        shouldStop = true;
                    }
                }

                if (shouldStop)
                {
                    Debug.WriteLine("[CargoForm] Elite Dangerous process no longer found. Stopping monitoring automatically.");
                    OnStopClicked(null, EventArgs.Empty);
                }
            }
            finally
            {
                // Dispose all process objects retrieved to release system resources.
                foreach (var p in gameProcesses)
                {
                    p.Dispose();
                }
            }
        }

        #endregion
    }
}