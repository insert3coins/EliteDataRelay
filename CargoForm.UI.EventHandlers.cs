using System;
using System.Diagnostics;
using System.IO;
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
            // Check for required files/paths before starting.
            if (string.IsNullOrEmpty(_journalWatcherService.JournalDirectoryPath) || !Directory.Exists(_journalWatcherService.JournalDirectoryPath))
            {
                MessageBox.Show(
                    $"Journal directory not found. Cannot start monitoring.\nPlease check the path in Settings.\n\nPath: {_journalWatcherService.JournalDirectoryPath}",
                    "Directory Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(AppConfiguration.CargoPath))
            {
                MessageBox.Show(
                    $"Cargo.json not found. Cannot start monitoring.\n\nMake sure Elite Dangerous is running and you are logged into the game.\nThe file is usually created when you enter your ship.\n\nExpected Path: {AppConfiguration.CargoPath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

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
                // Subscribe to the event that fires when a live setting (like repositioning) is changed.
                settingsForm.LiveSettingsChanged += (s, a) => ApplyLiveSettingsChanges();

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
            // Update button states and monitoring visuals to reflect any changes.
            // This is done first to ensure services like the overlay are started if needed.
            _cargoFormUI.SetButtonStates(
                startEnabled: !_fileMonitoringService.IsMonitoring,
                stopEnabled: _fileMonitoringService.IsMonitoring
            );
            _cargoFormUI.UpdateMonitoringVisuals(_fileMonitoringService.IsMonitoring);

            // Unregister any existing hotkeys before re-registering, to handle changes.
            UnregisterHotkeys();
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // If monitoring is active, refresh the overlay to apply visibility changes.
            if (_fileMonitoringService.IsMonitoring)
            {
                // Handle session tracking state change. If the user just enabled it,
                // we need to start the service. If they disabled it, we stop it.
                if (AppConfiguration.EnableSessionTracking)
                {
                    _sessionTrackingService.StartSession();
                }
                else
                {
                    _sessionTrackingService.StopSession();
                }

                _cargoFormUI.RefreshOverlay();
                _cargoFormUI.ShowOverlays(); // Ensure overlays are visible after refresh
                // Use BeginInvoke to queue the repopulation. This ensures that the new overlay
                // windows have fully processed their creation messages and are ready to be
                // updated before we try to send them data, preventing a race condition.
                this.BeginInvoke(new Action(RepopulateOverlay));
            }
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