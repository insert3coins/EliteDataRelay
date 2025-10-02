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

            // Attempt an initial read of the cargo file. This serves as a more robust check
            // than just File.Exists, as it also handles an empty or locked file.
            bool initialReadSuccess = _cargoProcessorService.ProcessCargoFile();

            if (!initialReadSuccess)
            {
                MessageBox.Show(
                    "Could not read initial cargo data.\n\n" +
                    "This can happen if the game is still starting up. Monitoring will begin, and the display will update automatically once you are in-game.",
                    "Initial Cargo Read Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            StartMonitoring();
        }

        private void OnStopClicked(object? sender, EventArgs e)
        {
            _soundService.PlayStopSound();

            // Reset services that maintain state to ensure a clean start next time.
            _cargoProcessorService.Reset();
            _journalWatcherService.Reset();

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

        /// <summary>
        /// Applies settings changes that need to take effect immediately, such as hotkeys and overlay visibility.
        /// </summary>
        private void ApplyLiveSettingsChanges()
        {
            UpdateHotkeysFromSettings();

            // If monitoring is active, we need to refresh services and UI.
            if (_fileMonitoringService.IsMonitoring)
            {
                UpdateSessionTrackingFromSettings();
                RefreshUIAndOverlays();
            }
            else
            {
                // If not monitoring, just update the main window visuals, which will hide any overlays.
                _cargoFormUI.UpdateMonitoringVisuals(false);
            }

            // Always update button states regardless of monitoring status.
            _cargoFormUI.SetButtonStates(
                startEnabled: !_fileMonitoringService.IsMonitoring,
                stopEnabled: _fileMonitoringService.IsMonitoring
            );
        }

        /// <summary>
        /// Registers or unregisters global hotkeys based on the current application settings.
        /// </summary>
        private void UpdateHotkeysFromSettings()
        {
            UnregisterHotkeys();
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }
        }

        /// <summary>
        /// Starts or stops the session tracking service based on the current application settings.
        /// </summary>
        private void UpdateSessionTrackingFromSettings()
        {
            if (AppConfiguration.EnableSessionTracking)
            {
                var initialCargo = _lastCargoSnapshot?.Count ?? 0;
                _sessionTrackingService.StartSession(_lastBalance ?? 0, initialCargo);
            }
            else
            {
                _sessionTrackingService.StopSession();
            }
        }

        /// <summary>
        /// Refreshes the main UI visuals and repopulates all overlays with the latest data.
        /// </summary>
        private void RefreshUIAndOverlays()
        {
            _cargoFormUI.UpdateMonitoringVisuals(true);

            // Use BeginInvoke to queue the repopulation. This ensures that the new overlay
            // windows have fully processed their creation messages and are ready to be
            // updated before we try to send them data, preventing a race condition.
            this.BeginInvoke(new Action(RepopulateOverlay));

            // Also force a session update to ensure the mining overlay is created/destroyed as needed.
            OnSessionUpdated(_sessionTrackingService, EventArgs.Empty);
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