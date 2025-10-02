using System;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        #region Service Event Handlers

        // A helper to check if the form can safely process an Invoke call.
        private bool CanInvoke() => !IsDisposed && !Disposing && IsHandleCreated;

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            Invoke(new Action(() =>
            {
                _lastCargoSnapshot = e.Snapshot;
                // --- File Output ---
                if (AppConfiguration.EnableFileOutput)
                {
                    _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
                }

                // Update the header label in the button panel
                _cargoFormUI.UpdateCargoHeader(e.Snapshot.Count, _cargoCapacity);

                // Update the main window display with the new list view
                _cargoFormUI.UpdateCargoList(e.Snapshot);

                // Update the visual cargo size indicator
                _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);
            }));
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            // This event can be raised from a background thread.
            _cargoCapacity = e.CargoCapacity;

            // After updating capacity, we must invoke on the UI thread to update the display.
            // This ensures the UI reflects the new capacity immediately.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                if (_lastCargoSnapshot != null)
                {
                    _cargoFormUI.UpdateCargoHeader(_lastCargoSnapshot.Count, _cargoCapacity);
                    _cargoFormUI.UpdateCargoDisplay(_lastCargoSnapshot, _cargoCapacity);
                }
                else
                {
                    // If we don't have cargo data yet, just update the header with 0 count.
                    _cargoFormUI.UpdateCargoHeader(0, _cargoCapacity);
                }
            }));
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastBalance = e.Balance;
                _cargoFormUI.UpdateBalance(e.Balance);

                // Notify the session tracker of the new balance to update session stats.
                _sessionTrackingService.UpdateBalance(e.Balance);
            }));
        }

        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastCommanderName = e.CommanderName;
                _cargoFormUI.UpdateCommanderName(e.CommanderName);
            }));
        }

        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastShipName = e.ShipName;
                _lastShipIdent = e.ShipIdent;
                _lastShipType = e.ShipType;
                _lastInternalShipName = e.InternalShipName;
                _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent, e.ShipType, e.InternalShipName); // Pass internal name
            }));
        }

        private void OnLoadoutChanged(object? sender, LoadoutChangedEventArgs e)
        {
            // The event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                var newLoadout = e.Loadout;
                _cargoFormUI.UpdateShipLoadout(newLoadout);

                // The Loadout event is a primary source for cargo capacity.
                _cargoCapacity = newLoadout.CargoCapacity;

                // Only clear the cargo hold if the ship has actually changed (e.g., ShipyardSwap).
                // If it's the same ship, this is likely a re-scan on startup, and we should preserve the cargo data.
                if (_lastShipId.HasValue && newLoadout.ShipId != _lastShipId.Value)
                {
                    // After a loadout change, the previous cargo snapshot (count and items) is stale.
                    // We must clear it and wait for the next 'Cargo' event to provide the new state.
                    _lastCargoSnapshot = null;

                    // Update the UI to reflect the new capacity and the now-empty cargo state.
                    _cargoFormUI.UpdateCargoHeader(0, _cargoCapacity);

                    // Create a new, empty snapshot to clear the UI list and visualizer.
                    var emptySnapshot = new CargoSnapshot(new System.Collections.Generic.List<CargoItem>(), 0);
                    _cargoFormUI.UpdateCargoList(emptySnapshot);
                    _cargoFormUI.UpdateCargoDisplay(emptySnapshot, _cargoCapacity);
                }
                _lastShipId = (uint)newLoadout.ShipId;
            }));
        }

        private void OnInitialScanComplete(object? sender, EventArgs e)
        {
            // This event fires after the journal watcher has completed its first read on startup.
            // At this point, a 'Loadout' event may have cleared our cargo display.
            // We now force a re-read of Cargo.json to ensure the UI is synchronized with the
            // actual cargo contents, resolving the "empty on start" issue.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("[CargoForm] Journal initial scan complete. Re-reading cargo file to ensure sync.");
                _ = _cargoProcessorService.ProcessCargoFile(force: true);
            }));
        }

        private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            // It provides live updates for fuel, cargo, hull, etc.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _cargoFormUI.UpdateShipStatus(e.Status);
            }));
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (sender is not SessionTrackingService tracker) return;

            if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
            {
                if (!CanInvoke()) return;

                Invoke(new Action(() =>
                {
                    _cargoFormUI.UpdateSessionOverlay(tracker.TotalCargoCollected, tracker.CreditsEarned);
                }));
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;

            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _cargoFormUI.UpdateLocation(_lastLocation);
            }));
        }

        private void OnStationInfoUpdated(object? sender, StationInfoData e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastStationInfoData = e;
                _cargoFormUI.UpdateStationInfo(e);
            }));
        }

        private void OnSystemInfoUpdated(object? sender, SystemInfoData e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                // _lastSystemInfoData = e;
                _cargoFormUI.UpdateSystemInfo(e); // This call is preserved for future use
            }));
        }

        #endregion
    }
}