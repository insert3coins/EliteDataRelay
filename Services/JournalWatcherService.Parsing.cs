using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public partial class JournalWatcherService
    {
        /// <summary>
        /// Resets the internal state of the watcher. This clears the last known file position,
        /// hashes, and other cached data, forcing a full re-read on the next poll.
        /// </summary>
        public void Reset()
        {
            _currentJournalFile = null;
            _lastPosition = 0;
            _lastStarSystem = null;
            _lastCargoHash = null;
            _lastStatusHash = null;
            _lastShipName = null;
            _lastShipIdent = null;
            _lastShipType = null;
            _lastCommanderName = null;
            _lastKnownBalance = 0;

            Debug.WriteLine("[JournalWatcherService] State has been reset.");
        }

        private void ProcessNewJournalEntries()
        {
            // On each poll, first check if there's a newer journal file than the one we're watching.
            var latestJournal = FindLatestJournalFile();
            if (latestJournal != null && latestJournal != _currentJournalFile)
            {
                Debug.WriteLine($"[JournalWatcherService] New journal file detected: {Path.GetFileName(latestJournal)}. Switching.");
                _currentJournalFile = latestJournal;
                _lastPosition = 0; // Reset position for the new file.
            }

            // If we have no file to watch, there's nothing to do.
            if (_currentJournalFile == null || !File.Exists(_currentJournalFile))
            {
                return;
            }


            try
            {
                using var fs = new FileStream(_currentJournalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length <= _lastPosition) return;

                fs.Seek(_lastPosition, SeekOrigin.Begin);

                using var reader = new StreamReader(fs);
                // Read all new lines into a list to process them.
                // This allows us to make multiple passes if needed.
                var newLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        newLines.Add(line);
                    }
                }

                if (!newLines.Any())
                {
                    _lastPosition = fs.Position;
                    return;
                }

                // --- First Pass: Location Events ---
                // It's critical to process location changes first. This establishes the context (i.e., the current SystemAddress)
                // for all other events in this batch, preventing race conditions where a signal is discovered
                // before the application knows it has jumped to a new system.
                foreach (var journalLine in newLines)
                {
                    try
                    {
                        // A more robust way of checking events rather than string.Contains
                        using var jsonDoc = JsonDocument.Parse(journalLine);
                        if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                        {
                            continue;
                        }

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        string? eventType = eventElement.GetString();

                        if (eventType == "Location" || eventType == "FSDJump" || eventType == "CarrierJump")
                        {
                            if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var starSystemElement) &&
                                starSystemElement.GetString() is string starSystem &&
                                !string.IsNullOrEmpty(starSystem))
                            {
                                double[]? starPos = null;
                                if (jsonDoc.RootElement.TryGetProperty("StarPos", out var starPosElement) && starPosElement.ValueKind == JsonValueKind.Array)
                                {
                                    starPos = starPosElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                                }

                                long? systemAddress = null;
                                if (jsonDoc.RootElement.TryGetProperty("SystemAddress", out var systemAddressElement) && systemAddressElement.TryGetInt64(out var sa))
                                {
                                    systemAddress = sa;
                                }

                                // The FSDJump event signals a new system, which means we should clear old system data.
                                // The Location event can fire when docking, so we don't want to clear data then,
                                // but we still want to treat the first-ever location as a new system.
                                bool isNewSystem = (eventType == "FSDJump" || eventType == "CarrierJump") || _lastStarSystem == null;

                                // If the system name has changed, it's definitely a new system.
                                // This is the primary time we want to process the full station list.
                                if (isNewSystem || starSystem != _lastStarSystem)
                                {
                                    _lastStarSystem = starSystem;
                                    Debug.WriteLine($"[JournalWatcherService] Found Location/Jump event. StarSystem: {starSystem}, IsNewSystem: {isNewSystem}");
                                    // Always pass the full station list when the system name changes.
                                    LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), isNewSystem, systemAddress));
                                }
                                else
                                {
                                    // For subsequent "Location" events within the same system (e.g., dropping from supercruise),
                                    // we still need to provide an update to ensure the SystemAddress is current, but we don't
                                    // need to re-process the station list.
                                    LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), false, systemAddress));
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[JournalWatcherService] Failed to parse journal line in location pass: {journalLine}. Error: {ex.Message}");
                    }
                }

                // --- Second Pass: All Other Events ---
                // Now that the location context is guaranteed to be up-to-date, process all other events.
                foreach (var journalLine in newLines)
                {
                    try
                    {
                        // A more robust way of checking events rather than string.Contains
                        using var jsonDoc = JsonDocument.Parse(journalLine);
                        if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                        {
                            continue;
                        }

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        string? eventType = eventElement.GetString();

                        // Skip location events as they were handled in the first pass
                        if (eventType == "Location" || eventType == "FSDJump" || eventType == "CarrierJump")
                        {
                            continue;
                        }

                        if (eventType == "Loadout")
                        {
                            var loadoutEvent = JsonSerializer.Deserialize<ShipLoadout>(journalLine, options);
                            if (loadoutEvent != null)
                            {
                                if (loadoutEvent.CargoCapacity > 0)
                                {
                                    Debug.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                                    CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
                                }

                                // Raise the full loadout event for the new Ship tab
                                LoadoutChanged?.Invoke(this, new LoadoutChangedEventArgs(loadoutEvent));

                                // The Loadout event is the source of truth for the ship's current state.
                                // We use the last known ship type as Loadout doesn't include a localized name.
                                UpdateShipInformation(loadoutEvent.ShipName, loadoutEvent.ShipIdent, _lastShipType, loadoutEvent.Ship);
                            }
                        }
                        else if (eventType == "LoadGame")
                        {
                            var loadGameEvent = JsonSerializer.Deserialize<LoadGameEvent>(journalLine, options);
                            if (loadGameEvent == null) continue;

                            // Check and update ship info
                            if (!string.IsNullOrEmpty(loadGameEvent.Commander) && loadGameEvent.Commander != _lastCommanderName)
                            {
                                _lastCommanderName = loadGameEvent.Commander;
                                Debug.WriteLine($"[JournalWatcherService] Found Commander Name: {_lastCommanderName}");
                                CommanderNameChanged?.Invoke(this, new CommanderNameChangedEventArgs(_lastCommanderName));
                            }

                            // Get the internal ship name first, as it's not in the strongly-typed model.
                            string? internalShipName = jsonDoc.RootElement.TryGetProperty("Ship", out var shipProp) ? shipProp.GetString() : null;

                            // Get the ship type, fallback to the non-localised name if needed.
                            var shipType = !string.IsNullOrEmpty(loadGameEvent.ShipLocalised) ? loadGameEvent.ShipLocalised
                                : Capitalize(internalShipName);
                            var shipName = loadGameEvent.ShipName;
                            var shipIdent = loadGameEvent.ShipIdent;
                            UpdateShipInformation(shipName, shipIdent, shipType, internalShipName);
                        }
                        else if (eventType == "Cargo")
                        {
                            var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(journalLine, options);
                            if (snapshot != null)
                            {
                                string hash = ComputeHash(snapshot);
                                if (hash == _lastCargoHash) continue;

                                _lastCargoHash = hash;
                                Debug.WriteLine($"[JournalWatcherService] Found Cargo event. Inventory count: {snapshot.Count}");
                                CargoInventoryChanged?.Invoke(this, new CargoInventoryEventArgs(snapshot));
                            }
                        }
                        else if (eventType == "Materials")
                        {
                            var materialsEvent = JsonSerializer.Deserialize<MaterialsEvent>(journalLine, options);
                            if (materialsEvent != null)
                            {
                                Debug.WriteLine($"[JournalWatcherService] Found Materials event.");
                                MaterialsEvent?.Invoke(this, new MaterialsEventArgs(materialsEvent));
                            }
                        }
                        else if (eventType == "MaterialCollected")
                        {
                            var collectedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(journalLine, options);
                            if (collectedEvent != null)
                            {
                                Debug.WriteLine($"[JournalWatcherService] Found MaterialCollected event for {collectedEvent.Name}.");
                                MaterialCollectedEvent?.Invoke(this, new MaterialCollectedEventArgs(collectedEvent));
                            }
                        }
                        else if (eventType == "MaterialDiscarded")
                        {
                            var discardedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(journalLine, options);
                            if (discardedEvent != null)
                            {
                                MaterialDiscardedEvent?.Invoke(this, new MaterialCollectedEventArgs(discardedEvent));
                            }
                        }
                        else if (eventType == "MaterialTrade")
                        {
                            var tradeEvent = JsonSerializer.Deserialize<MaterialTradeEvent>(journalLine, options);
                            if (tradeEvent != null)
                            {
                                MaterialTradeEvent?.Invoke(this, new MaterialTradeEventArgs(tradeEvent));
                            }
                        }
                        else if (eventType == "EngineerCraft")
                        {
                            var craftEvent = JsonSerializer.Deserialize<EngineerCraftEvent>(journalLine, options);
                            if (craftEvent != null)
                            {
                                EngineerCraftEvent?.Invoke(this, new EngineerCraftEventArgs(craftEvent));
                            }
                        }
                        else if (eventType == "ShipyardSwap")
                        {
                            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(journalLine, options);
                            if (swapEvent != null)
                            {
                                // A ship swap provides the new localized name. The subsequent Loadout event will provide the rest.
                                var newShipType = !string.IsNullOrEmpty(swapEvent.ShipTypeLocalised) ? swapEvent.ShipTypeLocalised : Capitalize(swapEvent.ShipType);
                                _lastShipType = newShipType; // Update our cached ship type immediately.
                                Debug.WriteLine($"[JournalWatcherService] Detected ShipyardSwap. New ship type: {newShipType}");
                            }
                        }
                        else if (eventType == "ModuleSell" || eventType == "ModuleBuy" || eventType == "ModuleStore" || eventType == "ModuleRetrieve" || eventType == "ModuleSwap")
                        {
                            // These events indicate a loadout change. The subsequent 'Loadout' event is the source of truth.
                            // We don't need to take action here, but acknowledging the event is useful for debugging.
                            Debug.WriteLine($"[JournalWatcherService] Detected module change event: {eventType}. Awaiting next Loadout.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[JournalWatcherService] Failed to parse journal line: {journalLine}. Error: {ex.Message}");
                        // Continue to the next line instead of breaking the loop.
                    }
                }
                _lastPosition = fs.Position;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Error polling journal file: {ex}");
            }
        }

        private void ProcessStatusFile()
        {
            var statusFilePath = Path.Combine(_journalDir, "Status.json");
            if (!File.Exists(statusFilePath))
            {
                return;
            }

            try
            {
                string content = File.ReadAllText(statusFilePath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                string hash = ComputeHash(content);
                if (hash == _lastStatusHash)
                {
                    return;
                }

                _lastStatusHash = hash;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var statusEvent = JsonSerializer.Deserialize<StatusFile>(content, options);

                if (statusEvent != null)
                {
                    Debug.WriteLine($"[JournalWatcherService] Found Status.json update. Fuel: {statusEvent.Fuel?.FuelMain}, Cargo: {statusEvent.Cargo}, Hull: {statusEvent.HullHealth:P1}");
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(statusEvent));

                    // Also handle balance changes to replace StatusWatcherService
                    if (statusEvent.Balance.HasValue && statusEvent.Balance.Value != _lastKnownBalance)
                    {
                        _lastKnownBalance = statusEvent.Balance.Value;
                        BalanceChanged?.Invoke(this, new BalanceChangedEventArgs(_lastKnownBalance));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Error processing Status.json: {ex}");
            }
        }

        private void UpdateShipInformation(string? shipName, string? shipIdent, string? shipType, string? internalShipName)
        {
            // Only raise an update event if something has actually changed.
            if (!string.IsNullOrEmpty(shipType) &&
                (shipName != _lastShipName || shipIdent != _lastShipIdent || shipType != _lastShipType))
            {
                _lastShipName = shipName;
                _lastShipIdent = shipIdent;
                _lastShipType = shipType;
                Debug.WriteLine($"[JournalWatcherService] Ship Info Updated. Name: {shipName}, Ident: {shipIdent}, Type: {shipType}");
                ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs(shipName ?? "N/A", shipIdent ?? "N/A", shipType ?? "Unknown", internalShipName ?? "unknown"));
            }
        }

        private string? Capitalize(string? s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Compute SHA256 hash of cargo snapshot for duplicate detection.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to hash.</param>
        /// <returns>Base64-encoded SHA256 hash.</returns>
        private string ComputeHash(CargoSnapshot snapshot)
        {
            string json = JsonSerializer.Serialize(
                new
                {
                    snapshot.Count,
                    snapshot.Inventory
                },
                new JsonSerializerOptions { WriteIndented = false, PropertyNameCaseInsensitive = true });

            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private string ComputeHash(string content)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}