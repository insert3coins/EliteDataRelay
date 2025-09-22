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

            // Start services that subscribe to journal events first, so they don't miss the initial scan.
            _materialService.Start();
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StartSession();
            }

            // Now start the services that produce events.
            _journalWatcherService.StartMonitoring(); // This will do an initial read of the whole journal.
            _statusWatcherService.StartMonitoring();
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

            // Stop status monitoring
            _statusWatcherService.StopMonitoring();

            // Stop material service
            _materialService.Stop();

            // Stop the game process checker
            _gameProcessCheckTimer?.Stop();

            // Stop the session tracker
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StopSession();
            }
        }

        #endregion
    }
}