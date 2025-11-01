using System;
using System.Diagnostics;
using System.Linq;
using System;
using System.IO;
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

                    // Legacy text file output removed

                    // Web overlay
                    _webOverlayService.UpdateCargo(e.Snapshot.Count, _cargoCapacity);
                    _webOverlayService.UpdateCargoList(e.Snapshot.Items);
                    // compute cargo size text, mirror desktop logic
                    int count = e.Snapshot.Count;
                    int index = 0;
                    if (_cargoCapacity is > 0)
                    {
                        double percentage = (double)count / _cargoCapacity.Value;
                        percentage = Math.Clamp(percentage, 0.0, 1.0);
                        index = (int)Math.Round(percentage * (UI.UIConstants.CargoSize.Length - 1));
                        index = Math.Clamp(index, 0, UI.UIConstants.CargoSize.Length - 1);
                    }
                    _webOverlayService.UpdateCargoSize(UI.UIConstants.CargoSize[index]);

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

                    // Web overlay
                    var webCount = _lastCargoSnapshot?.Count ?? 0;
                    _webOverlayService.UpdateCargo(webCount, _cargoCapacity);
                    int index = 0;
                    if (_cargoCapacity is > 0)
                    {
                        double percentage = (double)webCount / _cargoCapacity.Value;
                        percentage = Math.Clamp(percentage, 0.0, 1.0);
                        index = (int)Math.Round(percentage * (UI.UIConstants.CargoSize.Length - 1));
                        index = Math.Clamp(index, 0, UI.UIConstants.CargoSize.Length - 1);
                    }
                    _webOverlayService.UpdateCargoSize(UI.UIConstants.CargoSize[index]);
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

                    // Web overlay
                    _webOverlayService.UpdateBalance(e.Balance);
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
                    _webOverlayService.UpdateCommander(e.CommanderName);
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
                    _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent, e.ShipType, e.InternalShipName); // Pass internal name
                    _webOverlayService.UpdateShip(e.ShipType);
                    _webOverlayService.UpdateShipIconFromInternalName(e.InternalShipName);
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

                // Set exploration current system exactly once, then re-enable events
                if (!string.IsNullOrWhiteSpace(_lastLocation))
                {
                    _explorationDataService.HandleSystemChange(_lastLocation, _lastSystemAddress, _lastLocationTimestamp ?? DateTime.UtcNow);
                }
                _explorationDataService.SuppressEvents = false;
                var currentSystem = _explorationDataService.GetCurrentSystemData();
                var session = _explorationDataService.GetSessionData();
                if (currentSystem != null)
                {
                    _cargoFormUI.UpdateExplorationCurrentSystem(currentSystem);
                    _overlayService.UpdateExplorationData(currentSystem);
                }
                _overlayService.UpdateExplorationSessionData(session);

                // Finally, force a re-read of the cargo file to ensure it's perfectly in sync.
                _ = _cargoProcessorService.ProcessCargoFileAsync(force: true); // The result will update the UI via OnCargoProcessed
            });
        }


        private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                const long StatusFlagFsdCharging = 1L << 17; // 0x20000
                bool wasCharging = _lastStatus != null && ((_lastStatus.Flags & StatusFlagFsdCharging) != 0);
                bool isCharging = (e.Status.Flags & StatusFlagFsdCharging) != 0;

                _lastStatus = e.Status;
                if (!_isInitializing)
                {
                    _cargoFormUI.UpdateShipStatus(e.Status);
                }


                // If FSDCharging just started, show Next Jump overlay immediately (SrvSurvey-style fallback)
                // Ignore the very first Status event after start to avoid false positives
                if (_statusPrimed && !wasCharging && isCharging && AppConfiguration.EnableJumpOverlay)
                {
                    try
                    {
                        string? targetName = e.Status.FSDTarget?.Name;
                        var data = new NextJumpOverlayData
                        {
                            TargetSystemName = targetName,
                            StarClass = e.Status.FSDTarget?.StarClass,
                        };

                        var route = NavRouteService.TryReadSummary(_journalWatcherService.JournalDirectoryPath!, _lastLocation, _lastSystemAddress);
                        if (route != null)
                        {
                            data.Hops = route.Hops;
                            data.CurrentJumpIndex = route.CurrentIndex;
                            data.TotalJumps = route.Total;
                            data.NextDistanceLy = route.NextDistanceLy;
                            data.TotalRemainingLy = route.RemainingLy;
                            if (!data.JumpDistanceLy.HasValue && route.NextDistanceLy.HasValue)
                                data.JumpDistanceLy = route.NextDistanceLy;
                            if (!data.RemainingJumps.HasValue && route.CurrentIndex.HasValue)
                                data.RemainingJumps = Math.Max(0, route.Total - (route.CurrentIndex.Value + 1));
                            if (string.IsNullOrWhiteSpace(data.TargetSystemName) && route.Hops.Count > 0)
                                data.TargetSystemName = route.Hops[0].Name;
                        }

                        _overlayService.ShowNextJumpOverlay(data);
                    }
                    catch { /* ignore overlay errors */ }
                }

                // If FSDCharging was active and is now off (without a jump), hide the overlay (canceled charge)
                if (_statusPrimed && wasCharging && !isCharging)
                {
                    _overlayService.HideNextJumpOverlay();
                    _overlayService.HideNextJumpOverlay();
                }

                // Mark status primed after processing first event
                if (!_statusPrimed) _statusPrimed = true;
            });
        }


        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (sender is not SessionTrackingService tracker) return;

            SafeInvoke(() =>
            {
                // Update the mining tab on the main UI.
                _cargoFormUI.RefreshMiningStats();

                // Update the session stats on the cargo overlay if enabled.
                _cargoFormUI.UpdateSessionOverlay((int)tracker.TotalCargoCollected, tracker.CreditsEarned);

                // Web overlay session info
                _webOverlayService.UpdateSession(tracker.TotalCargoCollected, tracker.CreditsEarned);
            });
        }


        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;
            _lastSystemAddress = e.SystemAddress;
            _lastLocationTimestamp = e.Timestamp;

            // Update exploration service with system change
            // Suppress during initial scan to avoid iterating historical systems.
            if (!_isInitializing)
            {
                _explorationDataService.HandleSystemChange(e.StarSystem, e.SystemAddress, e.Timestamp);
            }
            // Next jump overlay removed: no action here

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

        private void OnJumpInitiated(object? sender, JumpInitiatedEventArgs e)
        {
            // Show Next Jump overlay when FSD starts charging (SrvSurvey-style)
            SafeInvoke(() =>
            {
                // Additional guard: ignore StartJump-triggered show until we have processed at least one Status.json update
                if (!_statusPrimed) { return; }

                // If we have a recent status and it's not charging, don't show yet
                const long StatusFlagFsdCharging = 1L << 17;
                if (_lastStatus != null && (_lastStatus.Flags & StatusFlagFsdCharging) == 0)
                {
                    return;
                }
                if (!AppConfiguration.EnableJumpOverlay) return;
                var targetName = e.TargetSystemName;
                var data = new NextJumpOverlayData
                {
                    TargetSystemName = targetName,
                    StarClass = e.StarClass,
                    JumpDistanceLy = e.JumpDistanceLy,
                    SystemInfo = null
                };

                // Enrich with current NavRoute summary
                var route = NavRouteService.TryReadSummary(_journalWatcherService.JournalDirectoryPath!, _lastLocation, _lastSystemAddress);
                if (route != null)
                {
                    data.Hops = route.Hops;
                    data.CurrentJumpIndex = route.CurrentIndex;
                    data.TotalJumps = route.Total;
                    data.NextDistanceLy = route.NextDistanceLy;
                    data.TotalRemainingLy = route.RemainingLy;
                    if (!data.JumpDistanceLy.HasValue && route.NextDistanceLy.HasValue)
                        data.JumpDistanceLy = route.NextDistanceLy;
                    if (!data.RemainingJumps.HasValue && route.CurrentIndex.HasValue)
                        data.RemainingJumps = Math.Max(0, route.Total - (route.CurrentIndex.Value + 1));

                    // If StartJump didn't provide a system name, fall back to the next hop from NavRoute
                    if (string.IsNullOrWhiteSpace(targetName) && route.Hops.Count > 0)
                    {
                        data.TargetSystemName = route.Hops[0].Name;
                    }
                }

                _overlayService.ShowNextJumpOverlay(data);
            });
        }

        private void OnJumpCompleted(object? sender, JumpCompletedEventArgs e)
        {
            // Hide Next Jump overlay on arrival (SrvSurvey-style)
            SafeInvoke(() => { System.Diagnostics.Trace.WriteLine("[CargoForm] FSDJump detected. Hiding Next Jump overlay after delay."); _overlayService.HideNextJumpOverlayAfter(TimeSpan.FromSeconds(2)); });
        }

        private void OnMultiSellExplorationData(object? sender, MultiSellExplorationDataEvent e)
        {
            // Update exploration service with on-foot data sales
            _explorationDataService.HandleMultiSellExplorationData(e);
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
