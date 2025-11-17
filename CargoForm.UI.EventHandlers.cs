using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Services;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        private Process? _gameProcess;

        #region UI Event Handlers

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            try
            {
                // Reset Status priming and ensure Next Jump overlay is hidden before starting
                _statusPrimed = false;
                _overlayService.HideNextJumpOverlay();

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
                bool initialReadSuccess = await _cargoProcessorService.ProcessCargoFileAsync();

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

                // Asynchronously check for updates after starting.
                _ = UpdateCheckService.CheckForUpdatesAsync(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CargoForm] Start monitoring failed: {ex}");
                MessageBox.Show(
                    "An error occurred while starting monitoring. Please check settings and try again.",
                    "Start Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnStopClicked(object? sender, EventArgs e)
        {
            _gameProcess?.Dispose();
            _gameProcess = null;

            _soundService.PlayStopSound();

            // Reset services that maintain state to ensure a clean start next time.
            _cargoProcessorService.Reset();
            _journalWatcherService.Reset();

            StopMonitoringInternal();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            // Open the dedicated About form instead of a simple MessageBox.
            using (var aboutForm = new AboutForm())
            {
                aboutForm.StartPosition = FormStartPosition.CenterParent;
                aboutForm.ShowDialog(this);
            }
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                // Subscribe to the event that fires when a live setting (like repositioning) is changed.
                settingsForm.LiveSettingsChanged += (s, a) => ApplyLiveSettingsChanges();
                settingsForm.RepositionModeChanged += (s, active) =>
                {
                    _overlayService.SetOverlayRepositionMode(active);
                };

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
            // Hotkeys removed; nothing to update

            // Update button states and monitoring visuals to reflect any changes.
            _cargoFormUI.SetButtonStates(
                startEnabled: !_fileMonitoringService.IsMonitoring,
                stopEnabled: _fileMonitoringService.IsMonitoring
            );

            // If monitoring is active, we need to refresh everything.
            if (_fileMonitoringService.IsMonitoring)
            {
                _cargoFormUI.RefreshOverlay(this);
                // Handle session tracking state change. If the user just enabled it,
                // we need to start the service. If they disabled it, we stop it.
                if (AppConfiguration.EnableSessionTracking)
                {
                    _sessionTrackingService.StartSession(_lastBalance ?? 0, _lastCargoSnapshot);
                }
                else
                {
                    _sessionTrackingService.StopSession();
                }

                // Use BeginInvoke to queue the repopulation. This ensures that the new overlay
                // windows have fully processed their creation messages and are ready to be
                // updated before we try to send them data, preventing a race condition.
                this.BeginInvoke(new Action(RefreshAllUIData));

                // Also force a session update to ensure the mining overlay is recreated if it was closed.
                OnSessionUpdated(_sessionTrackingService, EventArgs.Empty);
            }

            // Update the main form's visuals to reflect the current monitoring state.
            _cargoFormUI.UpdateMonitoringVisuals(_fileMonitoringService.IsMonitoring);
        }

        // Periodically checks if the game process is still running and stops monitoring if it's not.
        private void OnGameProcessCheck(object? sender, EventArgs e)
        {
            if (!_fileMonitoringService.IsMonitoring) return;

            // If we don't have a reference to the game process, try to find it.
            if (_gameProcess == null)
            {
                _gameProcess = Process.GetProcessesByName("EliteDangerous64").FirstOrDefault();
                if (_gameProcess == null)
                {
                    // If still not found, stop monitoring.
                    Debug.WriteLine("[CargoForm] Elite Dangerous process no longer found. Stopping monitoring automatically.");
                    OnStopClicked(null, EventArgs.Empty);
                    return;
                }
            }

            // Now that we have a process reference, just check if it has exited.
            // This is much more efficient than scanning all system processes every time.
            try
            {
                if (_gameProcess.HasExited)
                {
                    Debug.WriteLine("[CargoForm] Elite Dangerous process has exited. Stopping monitoring automatically.");
                    OnStopClicked(null, EventArgs.Empty);
                }
            }
            catch (Win32Exception)
            {
                // This can happen if the process is forcefully terminated or access is denied.
                // In either case, we should stop monitoring.
                Debug.WriteLine("[CargoForm] Could not access game process state. Stopping monitoring automatically.");
                OnStopClicked(null, EventArgs.Empty);
            }
            catch (InvalidOperationException)
            {
                // This can happen if the process object is in an invalid state.
                OnStopClicked(null, EventArgs.Empty);
            }
        }

        #endregion
    }
}
