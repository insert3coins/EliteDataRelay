using EliteDataRelay.Configuration;
using EliteDataRelay.UI;
using System.Threading.Tasks;

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
                _cargoFormUI.UpdateSessionOverlay((int)_sessionTrackingService.TotalCargoCollected, _sessionTrackingService.CreditsEarned);
            }
        }

        private void StartMonitoring()
        {
            // Set a flag to prevent piecemeal UI updates during the initial scan.
            _isInitializing = true;

            // Play start sound
            _soundService.PlayStartSound();

            // Immediately update UI state and show overlays for instant feedback.
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateMonitoringVisuals(isMonitoring: true);
            _overlayService.Start();
            // Start optional services
            if (AppConfiguration.EnableScreenshotRenamer) _screenshotRenamerService.Start();
            if (AppConfiguration.EnableWebOverlayServer) _webOverlayService.Start();
            _miningCompanionService.Start();

            // Start all background monitoring services.
            _fileMonitoringService.StartMonitoring();
            _stationInfoService.Start();
            _systemInfoService.Start();
            _gameProcessCheckTimer?.Start();

            // The initial journal scan can be slow. Run it in the background without awaiting it.
            // This allows the UI to remain responsive and the overlays to be visible immediately.
            // The `InitialScanComplete` event will fire when it's done, populating the UI.
            _ = Task.Run(() =>
            {
                _journalWatcherService.StartMonitoring();

                // Now that the initial poll is complete and _lastBalance is populated, start the session.
                if (AppConfiguration.EnableSessionTracking)
                {
                    _sessionTrackingService.StartSession(_lastBalance ?? 0, _lastCargoSnapshot);
                }

            });

            // Start exploration session tracking
            _explorationDataService.StartSession();

            // no webhook
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

            // Stop exploration session tracking
            _explorationDataService.StopSession();
            _explorationDataService.Reset();

            _stationInfoService.Stop();
            _systemInfoService.Stop();

            // Clear cached data after stopping services
            _lastStationInfoData = null;
            _lastSystemInfoData = null;

            // Stop optional services
            _screenshotRenamerService.Stop();
            _webOverlayService.Stop();
            _miningCompanionService.Stop();
            // no webhook
        }

        #endregion
    }
}
