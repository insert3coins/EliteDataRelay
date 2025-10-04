using EliteDataRelay.Configuration;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Monitoring Control
        
        /// <summary>
        /// Populates the entire UI from the cached data. This is called once after the initial
        /// journal scan is complete to ensure the UI reflects the game's state at startup.
        /// </summary>
        private void RefreshAllUIData()
        {
            // Populate the entire UI with the last known data.
            if (_lastCommanderName != null) _cargoFormUI.UpdateCommanderName(_lastCommanderName);
            if (_lastShipName != null && _lastShipIdent != null && _lastShipType != null && _lastInternalShipName != null) _cargoFormUI.UpdateShipInfo(_lastShipName, _lastShipIdent, _lastShipType, _lastInternalShipName);
            if (_lastLoadout != null) _cargoFormUI.UpdateShipLoadout(_lastLoadout);
            if (_lastBalance.HasValue) _cargoFormUI.UpdateBalance(_lastBalance.Value);
            if (_lastLocation != null) _cargoFormUI.UpdateLocation(_lastLocation);
            if (_lastStatus != null) _cargoFormUI.UpdateShipStatus(_lastStatus);
            if (_lastStationInfoData != null) _cargoFormUI.UpdateStationInfo(_lastStationInfoData);
            if (_lastSystemInfoData != null) _cargoFormUI.UpdateSystemInfo(_lastSystemInfoData);

            // Populate cargo and materials
            if (_lastCargoSnapshot != null)
            {
                _cargoFormUI.UpdateCargoDisplay(_lastCargoSnapshot, _cargoCapacity);
            }
            if (_lastMaterials != null)
            {
                (_cargoFormUI as CargoFormUI)?.UpdateMaterials(_lastMaterials);
            }

            // Also repopulate session data if tracking is active and shown on the overlay.
            if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
            {
                _cargoFormUI.UpdateSessionOverlay(_sessionTrackingService.TotalCargoCollected, _sessionTrackingService.CreditsEarned);
            }
        }

        private void StartMonitoring()
        {
            // Set a flag to prevent piecemeal UI updates during the initial scan.
            _isInitializing = true;

            // Play start sound
            _soundService.PlayStartSound();

            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateMonitoringVisuals(isMonitoring: true);

            // Start the overlay service
            _overlayService.Start();

            // Start file-based services first so they are ready.
            _fileMonitoringService.StartMonitoring();
            _stationInfoService.Start();
            _systemInfoService.Start();

            // Start the journal watcher last. Its initial poll will fire events that other services may need to handle.
            // This initial poll is synchronous and will populate _lastBalance before we proceed.
            _journalWatcherService.StartMonitoring();

            // Force one more read of the cargo file to ensure we have the absolute latest count before starting the session.
            _cargoProcessorService.ProcessCargoFile(force: true);

            // Now that the initial poll is complete and _lastBalance is populated, start the session.
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StartSession(_lastBalance ?? 0, _lastCargoSnapshot);
            }

            // Start the game process checker
            _gameProcessCheckTimer?.Start();
        }

        private void StopMonitoringInternal()
        {
            // Clear cached data first to prevent showing stale info on next start.

            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: true, stopEnabled: false);
            _cargoFormUI.UpdateMonitoringVisuals(isMonitoring: false);

            // Stop file monitoring
            _fileMonitoringService.StopMonitoring();

            // Stop the overlay service
            _overlayService.Stop();

            // Stop journal monitoring
            _journalWatcherService.StopMonitoring();

            // Stop the game process checker
            _gameProcessCheckTimer?.Stop();

            // Stop the session tracker
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StopSession();
            }

            _stationInfoService.Stop();
            _systemInfoService.Stop();

            // Clear cached data after stopping services
            _lastStationInfoData = null;
            _lastSystemInfoData = null;
        }

        #endregion
    }
}