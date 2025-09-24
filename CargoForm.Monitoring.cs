using EliteDataRelay.Configuration;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Monitoring Control

        private void RepopulateOverlay()
        {
            // Re-populate the UI (and the new overlay) with the last known data.
            if (_lastCommanderName != null) _cargoFormUI.UpdateCommanderName(_lastCommanderName);
            if (_lastShipName != null && _lastShipIdent != null && _lastShipType != null && _lastInternalShipName != null) _cargoFormUI.UpdateShipInfo(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName);
            if (_lastBalance.HasValue) _cargoFormUI.UpdateBalance(_lastBalance.Value);
            if (_lastLocation != null) _cargoFormUI.UpdateLocation(_lastLocation);
            if (_lastCargoSnapshot != null)
            {
                _cargoFormUI.UpdateCargoList(_lastCargoSnapshot);
                _cargoFormUI.UpdateCargoHeader(_lastCargoSnapshot.Count, _cargoCapacity);
                _cargoFormUI.UpdateCargoDisplay(_lastCargoSnapshot, _cargoCapacity);
            }
            if (_lastMaterialServiceCache != null) _cargoFormUI.UpdateMaterialsOverlay(_lastMaterialServiceCache);

            // Also repopulate session data if tracking is active and shown on the overlay.
            if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
            {
                _cargoFormUI.UpdateSessionOverlay(_sessionTrackingService.TotalCargoCollected, _sessionTrackingService.CreditsEarned);
            }
        }

        private void StartMonitoring()
        {
            // Play start sound
            _soundService.PlayStartSound();

            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateMonitoringVisuals(isMonitoring: true);
            _cargoFormUI.UpdateTitle("Elite Data Relay – Watching");

            // Re-populate the UI (and the new overlay) with the last known data.
            RepopulateOverlay();

            // Start the journal watcher first. Its initial poll will establish the current location.
            _journalWatcherService.StartMonitoring();

            // Now start the other services that may depend on the initial state.
            _materialService.Start();
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StartSession();
            }
            _systemInfoService.Start();
            _fileMonitoringService.StartMonitoring();

            // Process initial file snapshot after starting monitoring
            _cargoProcessorService.ProcessCargoFile();

            // Start the game process checker
            _gameProcessCheckTimer?.Start();
        }

        private void StopMonitoringInternal()
        {
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: true, stopEnabled: false);
            _cargoFormUI.UpdateMonitoringVisuals(isMonitoring: false);
            _cargoFormUI.UpdateTitle("Elite Data Relay – Stopped");

            // Stop file monitoring
            _fileMonitoringService.StopMonitoring();

            // Stop journal monitoring
            _journalWatcherService.StopMonitoring();

            // Stop material service
            _materialService.Stop();

            // Stop the game process checker
            _gameProcessCheckTimer?.Stop();

            // Stop the session tracker
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StopSession();
            }

            _systemInfoService.Stop();

            // Clear the system info cache to prevent showing stale data on next start.
            _lastSystemInfoData = null;
        }

        #endregion
    }
}