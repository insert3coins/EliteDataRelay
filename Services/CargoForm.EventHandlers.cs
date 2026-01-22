using System;
using System.Diagnostics;
using System.Linq;
using System;
using System.IO;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Models.Mining;
using EliteDataRelay.Services;
using EliteDataRelay.UI;

namespace EliteDataRelay
{
    public partial class CargoForm
    {
        private System.Windows.Forms.Timer? _jumpOverlayRefreshTimer;
        private int _jumpOverlayRefreshAttempts;
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

        private void OnEdsmStatusChanged(object? sender, EdsmUploadStatus status)
        {
            SafeInvoke(() => _cargoFormUI.UpdateEdsmStatus(status));
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
                    

                    // Auto-populate the trade commodity dropdown with items currently in cargo
                    // We must translate the internal names (e.g., "lowtemperaturediamonds") to friendly names ("Low Temperature Diamonds")
                    // that the EDSM API expects. We are no longer doing this.
                }

                RefreshMiningOverlay();
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
                    
                    int index = 0;
                    if (_cargoCapacity is > 0)
                    {
                        double percentage = (double)webCount / _cargoCapacity.Value;
                        percentage = Math.Clamp(percentage, 0.0, 1.0);
                        index = (int)Math.Round(percentage * (UI.UIConstants.CargoSize.Length - 1));
                        index = Math.Clamp(index, 0, UI.UIConstants.CargoSize.Length - 1);
                    }
                    
                });

                SafeInvoke(RefreshMiningOverlay);
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
                    _edsmUploadService.EnqueueBalanceSnapshot(e.Balance);

                    // Web overlay
                    
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


        private DateTime _lastReboardUtc;

        private static bool IsSpecialModeInternal(string? internalName)
            => !string.IsNullOrEmpty(internalName) && (
                   internalName.Equals("SRV", StringComparison.OrdinalIgnoreCase)
                || internalName.Equals("OnFoot", StringComparison.OrdinalIgnoreCase)
                || internalName.Equals("Fighter", StringComparison.OrdinalIgnoreCase)
                || internalName.Equals("Taxi", StringComparison.OrdinalIgnoreCase)
                || internalName.Equals("Multicrew", StringComparison.OrdinalIgnoreCase));

        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            SafeInvoke(() =>
            {
                // Debounce transient mode updates for a short window after re-boarding to avoid late suit/SRV events
                var now = DateTime.UtcNow;
                bool isSpecial = IsSpecialModeInternal(e.InternalShipName);
                if (isSpecial && (now - _lastReboardUtc).TotalSeconds < 8)
                {
                    return; // ignore stale SRV/suit/fighter in the reboard window
                }

                if (!isSpecial)
                {
                    // Treat any non-special update as a reboard/baseline update
                    _lastReboardUtc = now;
                }

                _lastShipName = e.ShipName;
                _lastShipIdent = e.ShipIdent;
                _lastShipType = e.ShipType;
                _lastInternalShipName = e.InternalShipName;
                
                if (!_isInitializing)
                {
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

                // Send initial balance to EDSM if available
                if (_lastBalance.HasValue)
                {
                    _edsmUploadService.EnqueueBalanceSnapshot(_lastBalance.Value);
                }

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


                // If FSDCharging just started, show the Next Jump overlay when
                // initiating a hyperspace jump. Some setups don't include FSDTarget
                // immediately; in that case use NavRoute as a fallback for the target.
                // Ignore the very first Status event after start to avoid false positives
                if (_statusPrimed && !wasCharging && isCharging && AppConfiguration.EnableJumpOverlay)
                {
                    try
                    {
                        // Prefer FSDTarget; fall back to Status.Destination when it points to a system (Body == 0)
                        string? targetName = e.Status.FSDTarget?.Name;
                        if (string.IsNullOrWhiteSpace(targetName))
                        {
                            var dest = e.Status.Destination;
                            if (!string.IsNullOrWhiteSpace(dest?.Name) && dest.System.HasValue && dest.System.Value > 0 && (dest.Body ?? 0) == 0)
                            {
                                targetName = dest.Name;
                            }
                        }
                        var data = new NextJumpOverlayData
                        {
                            TargetSystemName = targetName,
                            StarClass = e.Status.FSDTarget?.StarClass,
                        };

                        var route = NavRouteService.TryReadSummary(_journalWatcherService.JournalDirectoryPath!, _lastLocation, _lastSystemAddress, 7, targetName);
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
                            // Prefer next hop from route for the target name to avoid showing current system
                            if (route.Hops.Count > 0)
                                data.TargetSystemName = route.Hops[0].Name;
                        }

                        // Attach last-fetched system info immediately if it matches target
                        try
                        {
                            var lastInfo = _systemInfoService.GetLastSystemInfo();
                            if (lastInfo != null && !string.IsNullOrWhiteSpace(data.TargetSystemName) &&
                                string.Equals(lastInfo.SystemName, data.TargetSystemName, StringComparison.OrdinalIgnoreCase))
                            {
                                data.SystemInfo = lastInfo;
                            }
                        }
                        catch { /* ignore info service issues */ }
                        // If still no target name, do not bail — show minimal overlay
                        _overlayService.ShowNextJumpOverlay(data);

                        // Kick a short refresh loop to allow NavRoute/FSDTarget to settle
                        try
                        {
                            _jumpOverlayRefreshTimer?.Stop();
                            _jumpOverlayRefreshTimer?.Dispose();
                        }
                        catch { }
                        _jumpOverlayRefreshAttempts = 0;
                        _jumpOverlayRefreshTimer = new System.Windows.Forms.Timer { Interval = 400 };
                        _jumpOverlayRefreshTimer.Tick += (s, ev) =>
                        {
                            const long Flag = 1L << 17; // FSDCharging
                            bool stillCharging = (_lastStatus != null) && ((_lastStatus.Flags & Flag) != 0);
                            if (!stillCharging || _jumpOverlayRefreshAttempts++ >= 8)
                            {
                                try { _jumpOverlayRefreshTimer?.Stop(); } catch { }
                                return;
                            }

                            try
                            {
                                var refresh = new NextJumpOverlayData
                                {
                                    // Update from FSDTarget or Destination as they become available
                                    TargetSystemName = _lastStatus?.FSDTarget?.Name,
                                    StarClass = _lastStatus?.FSDTarget?.StarClass,
                                };
                                if (string.IsNullOrWhiteSpace(refresh.TargetSystemName))
                                {
                                    var dest = _lastStatus?.Destination;
                                    if (!string.IsNullOrWhiteSpace(dest?.Name) && dest.System.HasValue && dest.System.Value > 0 && (dest.Body ?? 0) == 0)
                                    {
                                        refresh.TargetSystemName = dest.Name;
                                    }
                                }
                                var r = NavRouteService.TryReadSummary(_journalWatcherService.JournalDirectoryPath!, _lastLocation, _lastSystemAddress, 7, refresh.TargetSystemName);
                                if (r != null)
                                {
                                    refresh.Hops = r.Hops;
                                    refresh.CurrentJumpIndex = r.CurrentIndex;
                                    refresh.TotalJumps = r.Total;
                                    refresh.NextDistanceLy = r.NextDistanceLy;
                                    refresh.TotalRemainingLy = r.RemainingLy;
                                    if (!refresh.RemainingJumps.HasValue && r.CurrentIndex.HasValue)
                                        refresh.RemainingJumps = Math.Max(0, r.Total - (r.CurrentIndex.Value + 1));
                                    if (r.Hops.Count > 0)
                                        refresh.TargetSystemName = r.Hops[0].Name;
                                }
                                _overlayService.ShowNextJumpOverlay(refresh);
                            }
                            catch { }
                        };
                        _jumpOverlayRefreshTimer.Start();
                    }
                    catch { /* ignore overlay errors */ }
                }

                // If FSDCharging was active and is now off (without a jump), hide the overlay (canceled charge)
                if (_statusPrimed && wasCharging && !isCharging)
                {
                    try { _jumpOverlayRefreshTimer?.Stop(); } catch { }
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
            // Update the dedicated session overlay with the latest stats.
            _cargoFormUI.UpdateSessionOverlay(BuildSessionOverlayData());

                // Web overlay session info
                
            });
        }


        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _lastLocation = e.StarSystem;
            _lastSystemAddress = e.SystemAddress;
            _lastLocationTimestamp = e.Timestamp;

            // Always update the exploration data service with the latest system.
            _explorationDataService.HandleSystemChange(e.StarSystem, e.SystemAddress, e.Timestamp);
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
                // If we have a recent status and it's not charging, don't show yet
                const long StatusFlagFsdCharging = 1L << 17;
                if (_lastStatus != null && (_lastStatus.Flags & StatusFlagFsdCharging) == 0)
                {
                    return;
                }
                if (!AppConfiguration.EnableJumpOverlay) return;

                // If the Next Jump overlay is already visible from FSDCharging, avoid duplicating the update here
                var existing = _overlayService.GetOverlay(UI.OverlayForm.OverlayPosition.JumpInfo);
                if (existing != null && existing.Visible)
                {
                    return;
                }
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
                    // Always prefer next hop from route as target to avoid current system name
                    if (route.Hops.Count > 0)
                        data.TargetSystemName = route.Hops[0].Name;
                }

                // Attach last-fetched system info immediately if it matches target
                try
                {
                    var lastInfo = _systemInfoService.GetLastSystemInfo();
                    if (lastInfo != null && !string.IsNullOrWhiteSpace(data.TargetSystemName) &&
                        string.Equals(lastInfo.SystemName, data.TargetSystemName, StringComparison.OrdinalIgnoreCase))
                    {
                        data.SystemInfo = lastInfo;
                    }
                }
                catch { /* ignore info service issues */ }

                _overlayService.ShowNextJumpOverlay(data);
            });
        }

        private void OnJumpCompleted(object? sender, JumpCompletedEventArgs e)
        {
            // Hide Next Jump overlay on arrival (SrvSurvey-style)
            SafeInvoke(() =>
            {
                System.Diagnostics.Trace.WriteLine("[CargoForm] FSDJump detected. Hiding Next Jump overlay after delay.");
                _overlayService.HideNextJumpOverlayAfter(TimeSpan.FromSeconds(2));

                // Refresh EDSM traffic on arrival to avoid stale/empty data shown during FSD charge
                try { _systemInfoService.RequestFetch(e.SystemName); } catch { /* ignore info fetch issues */ }
            });
        }

        private void OnNextJumpSystemChanged(object? sender, NextJumpSystemChangedEventArgs e)
        {
            // Keep Next Jump overlay in sync mid-charge when target/route info updates (FSDTarget/NavRoute)
            SafeInvoke(() =>
            {
                if (!AppConfiguration.EnableJumpOverlay) return;
                // Only show when charging or already visible (match InfraSurveyor behavior)
                const long StatusFlagFsdCharging = 1L << 17; // 0x20000
                bool isCharging = _lastStatus != null && ((_lastStatus.Flags & StatusFlagFsdCharging) != 0);
                var existing = _overlayService.GetOverlay(UI.OverlayForm.OverlayPosition.JumpInfo);
                bool isVisible = existing != null && existing.Visible;
                if (!isCharging && !isVisible)
                {
                    // Not charging and overlay not showing: skip showing here; allow data services to update in background
                    return;
                }

                var data = new NextJumpOverlayData
                {
                    TargetSystemName = e.NextSystemName,
                    StarClass = e.StarClass,
                    JumpDistanceLy = e.JumpDistanceLy,
                    RemainingJumps = e.RemainingJumps
                };

                var route = NavRouteService.TryReadSummary(_journalWatcherService.JournalDirectoryPath!, _lastLocation, _lastSystemAddress, 7, e.NextSystemName);
                if (route != null)
                {
                    data.Hops = route.Hops;
                    data.CurrentJumpIndex = route.CurrentIndex;
                    data.TotalJumps = route.Total;
                    data.NextDistanceLy ??= route.NextDistanceLy;
                    data.TotalRemainingLy = route.RemainingLy;
                    if (!data.RemainingJumps.HasValue && route.CurrentIndex.HasValue)
                        data.RemainingJumps = Math.Max(0, route.Total - (route.CurrentIndex.Value + 1));
                    if (route.Hops.Count > 0)
                        data.TargetSystemName = route.Hops[0].Name;
                }

                _overlayService.ShowNextJumpOverlay(data);
            });
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

        private void OnMiningSessionUpdated(object? sender, EventArgs e)
        {
            SafeInvoke(RefreshMiningOverlay);
        }

        private void OnLatestProspectorUpdated(object? sender, EventArgs e)
        {
            SafeInvoke(() =>
            {
                _overlayService.UpdateProspectorOverlay(BuildProspectorOverlayData());
            });
        }

        private void OnMiningLiveStateChanged(object? sender, bool isLive)
        {
            SafeInvoke(() =>
            {
                if (!isLive)
                {
                    _overlayService.UpdateMiningOverlay(null);
                    _overlayService.UpdateProspectorOverlay(null);
                }
                else
                {
                    RefreshMiningOverlay();
                    _overlayService.UpdateProspectorOverlay(BuildProspectorOverlayData());
                }
            });
        }

        private void RefreshMiningOverlay()
        {
            _overlayService.UpdateMiningOverlay(BuildMiningOverlayData());
        }

        private MiningOverlayData? BuildMiningOverlayData()
        {
            var session = _miningTrackerService.CurrentSession;
            if (session == null || !session.Started)
            {
                return null;
            }

            var end = session.TimeFinished < DateTime.MaxValue ? session.TimeFinished : DateTime.UtcNow;
            if (end < session.TimeStarted) end = DateTime.UtcNow;
            var duration = end - session.TimeStarted;
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

            var oreItems = session.Items.Where(x => x.Type == MiningItemType.Ore);
            int totalRefined = oreItems.Sum(x => x.RefinedCount);
            double refinedPerHour = duration.TotalHours > 0 ? totalRefined / duration.TotalHours : 0;
            int materialsCollected = session.Items.Where(x => x.Type == MiningItemType.Material).Sum(x => x.CollectedCount);

            var rows = session.Items
                .OrderByDescending(x => x.Type == MiningItemType.Ore)
                .ThenByDescending(x => x.RefinedCount)
                .ThenByDescending(x => x.CollectedCount)
                .Take(8)
                .Select(item => new MiningOverlayCommodity
                {
                    Name = item.Name,
                    Type = item.Type,
                    RefinedCount = item.RefinedCount,
                    CollectedCount = item.CollectedCount,
                    Prospected = item.Prospected,
                    HitRate = session.AsteroidsProspected > 0 && item.Type == MiningItemType.Ore
                        ? (double)item.ContentHitCount / session.AsteroidsProspected * 100d
                        : 0d,
                    MinPercentage = item.MinPercentage,
                    MaxPercentage = item.MaxPercentage,
                    Motherlodes = item.MotherLoad,
                    LowContent = item.LowContent,
                    MedContent = item.MedContent,
                    HighContent = item.HighContent
                })
                .ToList();

            return new MiningOverlayData
            {
                Location = BuildMiningLocationLabel(session),
                Duration = duration,
                RefinedPerHour = refinedPerHour,
                ProspectorsFired = session.ProspectorsFired,
                CollectorsDeployed = session.CollectorsDeployed,
                AsteroidsProspected = session.AsteroidsProspected,
                AsteroidsCracked = session.AsteroidsCracked,
                TotalRefined = totalRefined,
                MaterialsCollected = materialsCollected,
                LowContent = session.LowContent,
                MedContent = session.MedContent,
                HighContent = session.HighContent,
                LimpetsRemaining = GetRemainingLimpets(),
                CargoFree = GetCargoFree(),
                CargoCapacity = _cargoCapacity,
                Commodities = rows
            };
        }

        private ProspectorOverlayData? BuildProspectorOverlayData()
        {
            var prospector = _miningTrackerService.LatestProspector;
            if (prospector == null)
            {
                return null;
            }

            var materials = prospector.Materials
                .OrderByDescending(m => m.Proportion)
                .Take(6)
                .Select(m => new ProspectorOverlayItem
                {
                    Name = m.Name,
                    Percentage = m.Proportion
                })
                .ToList();

            return new ProspectorOverlayData
            {
                Content = prospector.Content,
                RemainingPercent = prospector.Remaining,
                Motherlode = prospector.MotherlodeMaterial,
                IsDepleted = prospector.Remaining <= 0,
                Materials = materials
            };
        }

        private static string BuildMiningLocationLabel(MiningSession session)
        {
            var system = (session.StarSystem ?? "Unknown").Trim();
            var location = session.Location?.Trim();

            if (string.IsNullOrWhiteSpace(location))
            {
                return system;
                }

            if (!string.IsNullOrEmpty(system) &&
                location.StartsWith(system, StringComparison.OrdinalIgnoreCase))
            {
                var suffix = location.Substring(system.Length).Trim();
                suffix = suffix.TrimStart('-', '•').Trim();
                if (string.IsNullOrEmpty(suffix))
                {
                    return system;
                }

                return $"{system} • {suffix}";
            }

            return string.IsNullOrEmpty(system) ? location : $"{system} • {location}";
        }

        private int? GetRemainingLimpets()
        {
            var snapshot = _lastCargoSnapshot;
            if (snapshot?.Items == null) return null;
            var limpet = snapshot.Items.FirstOrDefault(item =>
                (!string.IsNullOrEmpty(item.Localised) && item.Localised.IndexOf("limpet", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(item.Name) && item.Name.IndexOf("limpet", StringComparison.OrdinalIgnoreCase) >= 0));
            return limpet?.Count;
        }

        private int? GetCargoFree()
        {
            if (!_cargoCapacity.HasValue) return null;
            var used = _lastCargoSnapshot?.Count ?? 0;
            int remaining = _cargoCapacity.Value - used;
            return remaining >= 0 ? remaining : 0;
        }

        #endregion
    }
}
