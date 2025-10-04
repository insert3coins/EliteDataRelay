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
        private bool CanInvoke() => !IsDisposed && !Disposing && IsHandleCreated;

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            Invoke(new Action(() =>
            {
                _lastCargoSnapshot = e.Snapshot; // Always cache the latest data

                // Only update the UI if we are not in the middle of the initial scan
                if (!_isInitializing)
                {
                    // --- File Output ---
                    if (AppConfiguration.EnableFileOutput)
                    {
                        _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
                    }

                    _cargoFormUI.UpdateCargoHeader(e.Snapshot.Count, _cargoCapacity);
                    _cargoFormUI.UpdateCargoList(e.Snapshot);
                    _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);

                    // Auto-populate the trade commodity dropdown with items currently in cargo
                    // We must translate the internal names (e.g., "lowtemperaturediamonds") to friendly names ("Low Temperature Diamonds")
                    // that the EDSM API expects.
                    var cargoCommodities = e.Snapshot.Items.Select(i => ItemNameService.TranslateCommodityName(i.Name) ?? i.Name).Distinct();

                    var allCommodities = cargoCommodities.Union(ItemNameService.GetAllCommodityNames()).OrderBy(c => c);
                    _cargoFormUI.PopulateCommodities(allCommodities);
                }
            }));
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
                        _cargoFormUI.UpdateCargoHeader(0, _cargoCapacity);
                    }
                }));
            }
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastBalance = e.Balance;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateBalance(e.Balance);

                    // Notify the session tracker of the new balance to update session stats.
                    _sessionTrackingService.UpdateBalance(e.Balance);
                }
            }));
        }

        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastCommanderName = e.CommanderName;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateCommanderName(e.CommanderName);
                }
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
            }));
        }

        private void OnLoadoutChanged(object? sender, LoadoutChangedEventArgs e)
        {
            // The event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
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
                        _lastCargoSnapshot = null;
                        _cargoFormUI.UpdateCargoHeader(0, _cargoCapacity);
                        var emptySnapshot = new CargoSnapshot(new System.Collections.Generic.List<CargoItem>(), 0);
                        _cargoFormUI.UpdateCargoList(emptySnapshot);
                        _cargoFormUI.UpdateCargoDisplay(emptySnapshot, _cargoCapacity);
                    }
                }

                _lastShipId = newShipId;

                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateShipLoadout(loadout);
                }
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
                Debug.WriteLine("[CargoForm] Journal initial scan complete. Performing full UI refresh.");
                _isInitializing = false; // Initial scan is done, allow normal UI updates now.

                // Now that all initial data is cached, update the entire UI at once.
                RefreshAllUIData();

                // Finally, force a re-read of the cargo file to ensure it's perfectly in sync.
                _ = _cargoProcessorService.ProcessCargoFile(force: true); // The result will update the UI via OnCargoProcessed
            }));
        }

        private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            // It provides live updates for fuel, cargo, hull, etc.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastStatus = e.Status;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateShipStatus(e.Status);
                }
            }));
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (sender is not SessionTrackingService tracker) return;

            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                if (!_isInitializing)
                {
                    // Update the general session stats on the cargo overlay if enabled
                    if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
                    {
                        _cargoFormUI.UpdateSessionOverlay(tracker.TotalCargoCollected, tracker.CreditsEarned);
                    }

                    // Always update the mining tab on the main UI and the dedicated mining overlay
                    _cargoFormUI.UpdateMiningStats();
                }
            }));
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;

            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateLocation(_lastLocation);

                    // When location changes, clear old trade results
                    _cargoFormUI.UpdateTradeResults(new System.Collections.Generic.List<Models.Market.MarketInfo>(), true); // Clears the list
                    _cargoFormUI.SetTradeStatus("Location changed. Select a commodity and search.");

                }
            }));
        }

        private async void OnTradeFindBestSellClicked(object? sender, EventArgs e)
        {
            var commodity = _cargoFormUI.GetSelectedTradeCommodity();
            if (string.IsNullOrWhiteSpace(commodity))
            {
                _cargoFormUI.SetTradeStatus("Please select a commodity to search for.");
                return;
            }

            Trace.WriteLine($"[TradeSearch] 'Find Best Sell' clicked for commodity: {commodity}");
            _cargoFormUI.StartTradeSearchAnimation();
            _cargoFormUI.SetTradeStatus($"Searching for best sell price for {commodity} within 50 LY of {_lastLocation}...");

            try
            {
                var results = await _marketDataService.FindBestSellLocationsAsync(_lastLocation, commodity);

                Trace.WriteLine($"[TradeSearch] Service returned {results.Count} potential sell locations. Updating UI.");
                if (CanInvoke())
                {
                    Invoke(new Action(() => _cargoFormUI.UpdateTradeResults(results, isSellSearch: true)));
                }
            }
            finally
            {
                _cargoFormUI.StopTradeSearchAnimation();
                // Re-enable buttons via the commodity selection logic
                Invoke(new Action(() => _cargoFormUI.OnTradeCommodityChanged(null, EventArgs.Empty)));
            }
        }

        private async void OnTradeFindBestBuyClicked(object? sender, EventArgs e)
        {
            var commodity = _cargoFormUI.GetSelectedTradeCommodity();
            if (string.IsNullOrWhiteSpace(commodity))
            {
                _cargoFormUI.SetTradeStatus("Please select a commodity to search for.");
                return;
            }

            Trace.WriteLine($"[TradeSearch] 'Find Best Buy' clicked for commodity: {commodity}");
            _cargoFormUI.StartTradeSearchAnimation();
            _cargoFormUI.SetTradeStatus($"Searching for best buy price for {commodity} within 50 LY of {_lastLocation}...");

            try
            {
                var results = await _marketDataService.FindBestBuyLocationsAsync(_lastLocation, commodity);

                Trace.WriteLine($"[TradeSearch] Service returned {results.Count} potential buy locations. Updating UI.");
                if (CanInvoke())
                {
                    Invoke(new Action(() => _cargoFormUI.UpdateTradeResults(results, isSellSearch: false)));
                }
            }
            finally
            {
                _cargoFormUI.StopTradeSearchAnimation();
                // Re-enable buttons via the commodity selection logic
                Invoke(new Action(() => _cargoFormUI.OnTradeCommodityChanged(null, EventArgs.Empty)));
            }
        }

        private void OnStationInfoUpdated(object? sender, StationInfoData e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                _lastStationInfoData = e;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateStationInfo(e);
                }
            }));
        }

        private void OnSystemInfoUpdated(object? sender, SystemInfoData e)
        {
            // This event is raised from a background thread, so we must invoke on the UI thread.
            if (!CanInvoke()) return;

            Invoke(new Action(() =>
            {
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateSystemInfo(e);
                }
            }));
        }

        #endregion
    }
}