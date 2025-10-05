using System;
using System.Diagnostics;
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
        private bool CanInvoke() => IsHandleCreated && !IsDisposed && !Disposing;

        // A helper to safely invoke actions on the UI thread, reducing boilerplate.
        private void SafeInvoke(Action action)
        {
            if (CanInvoke())
            {
                Invoke(action);
            }
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            SafeInvoke(() =>
            {
                _lastCargoSnapshot = e.Snapshot; // Always cache the latest data

                // Only update the UI if we are not in the middle of the initial scan
                if (!_isInitializing)
                {
                    // --- UI Update ---
                    _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity); // This was the missing call
                    _cargoFormUI.UpdateCargoScrollBar();

                    // --- File Output ---
                    if (AppConfiguration.EnableFileOutput)
                    {
                        _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
                    }

                    // Auto-populate the trade commodity dropdown with items currently in cargo
                    // We must translate the internal names (e.g., "lowtemperaturediamonds") to friendly names ("Low Temperature Diamonds")
                    // that the EDSM API expects. We are no longer doing this.
                }
            });
        }


        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            // This event can be raised from a background thread.
            _cargoCapacity = e.CargoCapacity;
            
            // Only update the UI if we are not in the middle of the initial scan
            if (!_isInitializing)
            {
                // After updating capacity, we must invoke on the UI thread to update the display.
                // This ensures the UI reflects the new capacity immediately.
                SafeInvoke(() =>
                {
                    if (_lastCargoSnapshot != null)
                    {
                        _cargoFormUI.UpdateCargoDisplay(_lastCargoSnapshot, _cargoCapacity);
                        _cargoFormUI.UpdateCargoScrollBar();
                    }
                    else
                    {
                        _cargoFormUI.UpdateCargoDisplay(new CargoSnapshot(new System.Collections.Generic.List<CargoItem>(), 0), _cargoCapacity);
                    }
                });
            }
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                _lastBalance = e.Balance;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateBalance(e.Balance);

                    // Notify the session tracker of the new balance to update session stats.
                    _sessionTrackingService.UpdateBalance(e.Balance);
                }
            });
        }


        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                _lastCommanderName = e.CommanderName;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateCommanderName(e.CommanderName);
                }
            });
        }


        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                _lastShipName = e.ShipName;
                _lastShipIdent = e.ShipIdent;
                _lastShipType = e.ShipType;
                _lastInternalShipName = e.InternalShipName;

                if (!_isInitializing)
                {
                    // If we have a loadout, this is the most reliable time to update the ship UI,
                    // as it avoids being overwritten by other events like CargoProcessed.
                    if (_lastLoadout != null)
                    {
                        _cargoFormUI.UpdateShipLoadout(_lastLoadout); // This call is now correct
                        Trace.WriteLine($"[CargoForm] OnShipInfoChanged: Called UpdateShipLoadout with cached loadout for ship ID {_lastLoadout.ShipId}.");
                    }
                    _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent, e.ShipType, e.InternalShipName); // Pass internal name
                }
            });
        }


        private void OnLoadoutChanged(object? sender, LoadoutChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                var loadout = e.Loadout;
                _lastLoadout = loadout; // Store the latest loadout
                Trace.WriteLine($"[CargoForm] OnLoadoutChanged: Received and cached loadout for ship ID {loadout.ShipId}.");
                var newShipId = (uint)loadout.ShipId;

                // The Loadout event is a primary source for cargo capacity.
                _cargoCapacity = loadout.CargoCapacity;

                // If the ship has changed (e.g., after a ShipyardSwap), the old cargo data is invalid.
                // We clear the cargo display and wait for the next 'Cargo' event.
                if (_lastShipId.HasValue && newShipId != _lastShipId.Value)
                {
                    if (!_isInitializing)
                    {                        
                        var emptySnapshot = new CargoSnapshot(new System.Collections.Generic.List<CargoItem>(), 0);
                        _lastCargoSnapshot = emptySnapshot;
                        _cargoFormUI.UpdateCargoDisplay(emptySnapshot, _cargoCapacity);
                        _cargoFormUI.UpdateCargoScrollBar();
                    }
                }

                _lastShipId = newShipId;

                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateShipLoadout(loadout);
                }
            });
        }

        private void OnInitialScanComplete(object? sender, EventArgs e)
        {
            // This event fires after the journal watcher has completed its first read on startup.
            // At this point, a 'Loadout' event may have cleared our cargo display.
            // We now force a re-read of Cargo.json to ensure the UI is synchronized with the
            // actual cargo contents, resolving the "empty on start" issue.
            SafeInvoke(() =>
            {
                Debug.WriteLine("[CargoForm] Journal initial scan complete. Performing full UI refresh.");
                _isInitializing = false; // Initial scan is done, allow normal UI updates now.

                // Now that all initial data is cached, update the entire UI at once.
                RefreshAllUIData();

                // Finally, force a re-read of the cargo file to ensure it's perfectly in sync.
                _ = _cargoProcessorService.ProcessCargoFile(force: true); // The result will update the UI via OnCargoProcessed
            });
        }


        private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                _lastStatus = e.Status;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateShipStatus(e.Status);
                }
            });
        }


        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (sender is not SessionTrackingService tracker) return;

            SafeInvoke(() =>
            {
                if (!_isInitializing && AppConfiguration.EnableSessionTracking)
                {
                    // Update the general session stats on the cargo overlay if enabled
                    if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
                    {
                        _cargoFormUI.UpdateSessionOverlay(tracker.TotalCargoCollected, tracker.CreditsEarned);
                    }

                    // Always update the mining tab on the main UI and the dedicated mining overlay
                    _cargoFormUI.UpdateMiningStats();
                }
            });
        }


        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;

            SafeInvoke(() =>
            {
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateLocation(_lastLocation);

                }
            });
        }


        private void OnStationInfoUpdated(object? sender, StationInfoData e)
        {
            SafeInvoke(() =>
            {
                _lastStationInfoData = e;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateStationInfo(e);
                }
            });
        }


        private void OnSystemInfoUpdated(object? sender, SystemInfoData e)
        {
            SafeInvoke(() =>
            {
                _lastSystemInfoData = e;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateSystemInfo(e);
                }
            });
        }

        private void OnMaterialsChanged(object? sender, MaterialsEvent e)
        {
            SafeInvoke(() =>
            {
                _lastMaterials = e;

                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateMaterials(e);
                }
            });
        }

        #endregion
    }
}