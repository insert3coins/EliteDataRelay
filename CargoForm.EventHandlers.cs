using System;
using System.Linq;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Service Event Handlers

        private void OnFileChanged(object? sender, EventArgs e)
        {
            // Delegate file processing to the cargo processor service
            _cargoProcessorService.ProcessCargoFile();
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            _lastCargoSnapshot = e.Snapshot;
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.OnCargoChanged(e.Snapshot);
            }
            // --- File Output ---
            // If enabled in settings, write the snapshot to the output text file.
            if (AppConfiguration.EnableFileOutput)
            {
                _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
            }

            int totalCount = e.Snapshot.Inventory.Sum(item => item.Count);

            // Update the header label in the button panel
            _cargoFormUI.UpdateCargoHeader(totalCount, _cargoCapacity);

            // Update the main window display with the new list view
            _cargoFormUI.UpdateCargoList(e.Snapshot);

            // Update the visual cargo size indicator
            _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            _cargoCapacity = e.CargoCapacity;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;
            _cargoFormUI.UpdateLocation(e.StarSystem);
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            _lastBalance = e.Balance;
            _cargoFormUI.UpdateBalance(e.Balance);
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.OnBalanceChanged(e.Balance);
            }
        }

        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            _lastCommanderName = e.CommanderName;
            _cargoFormUI.UpdateCommanderName(e.CommanderName);
        }

        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            _lastShipName = e.ShipName;
            _lastShipIdent = e.ShipIdent;
            _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent);
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
            {
                _cargoFormUI.UpdateSessionOverlay(_sessionTrackingService.TotalCargoCollected, _sessionTrackingService.CreditsEarned);
            }
        }

        #endregion
    }
}