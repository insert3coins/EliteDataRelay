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
        private void ProcessNewJournalEntries()
        {
            // On each poll, first check if there's a newer journal file than the one we're watching.
            var latestJournal = FindLatestJournalFile();
            if (latestJournal != null && latestJournal != _currentJournalFile)
            {
                Trace.WriteLine($"[JournalWatcherService] New journal file detected: {Path.GetFileName(latestJournal)}. Switching.");
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
                // Read all new lines and parse them into a list of JsonDocument objects.
                // This avoids parsing the same string twice in our two-pass system.
                var newEntries = new List<(JsonDocument doc, string line)>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        try
                        {
                            newEntries.Add((JsonDocument.Parse(line), line));
                        }
                        catch (JsonException ex)
                        {
                            Trace.WriteLine($"[JournalWatcherService] Failed to parse journal line, skipping: {line}. Error: {ex.Message}");
                        }
                    }
                }

                if (!newEntries.Any())
                {
                    _lastPosition = fs.Position;
                    return;
                }

                // Use a finally block to ensure we dispose of all the JsonDocument objects
                // we created, preventing memory leaks.
                try
                { 
                // Create serializer options once and reuse to avoid allocations in the loop.
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // --- First Pass: Location Events ---
                // It's critical to process location changes first. This establishes the context (i.e., the current SystemAddress)
                // for all other events in this batch, preventing race conditions where a signal is discovered
                // before the application knows it has jumped to a new system.
                foreach (var (jsonDoc, journalLine) in newEntries)
                {
                    try
                    {
                        if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                        {
                            continue;
                        }

                        string? eventType = eventElement.GetString();

                        if (eventType == "LoadGame")
                        {
                            ProcessLoadGameEvent(jsonDoc, journalLine, options);
                        }
                        // Handle the case where we load into the game already docked.
                        // The 'Location' event will contain all the necessary station info.
                        if (eventType == "Location" &&
                            jsonDoc.RootElement.TryGetProperty("Docked", out var dockedElement) &&
                            dockedElement.GetBoolean())
                        {
                            var dockedEvent = JsonSerializer.Deserialize<DockedEvent>(journalLine, options);
                            if (dockedEvent != null)
                            {
                                _lastDockedEventArgs = new DockedEventArgs(dockedEvent);
                                Docked?.Invoke(this, _lastDockedEventArgs);
                            }
                        }

                        if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var starSystemElement) && starSystemElement.GetString() is string starSystem && !string.IsNullOrEmpty(starSystem))
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

                            // A "new system" is detected if it's a jump event, the first location event,
                            // or if the system name has changed from the last known one.
                            bool isNewSystem = (eventType == "FSDJump" || eventType == "CarrierJump") || _lastStarSystem == null || starSystem != _lastStarSystem;

                            if (isNewSystem)
                            {
                                _lastStarSystem = starSystem;
                                _lastLocationArgs = new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), true, systemAddress); // This line was missing
                                Debug.WriteLine($"[JournalWatcherService] Found new system event ({eventType}). StarSystem: {starSystem}");
                                LocationChanged?.Invoke(this, _lastLocationArgs);
                            }
                            else // This only applies to subsequent "Location" events in the same system.
                            {
                                _lastLocationArgs = new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), false, systemAddress);
                                LocationChanged?.Invoke(this, _lastLocationArgs);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Trace.WriteLine($"[JournalWatcherService] Failed to parse journal line in location pass: {journalLine}. Error: {ex.Message}");
                    }
                }

                // --- Second Pass: All Other Events ---
                // Now that the location context is guaranteed to be up-to-date, process all other events.
                foreach (var (jsonDoc, journalLine) in newEntries)
                {
                    try
                    {
                        if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                        {
                            continue;
                        }

                        string? eventType = eventElement.GetString();

                        // Skip location events as they were handled in the first pass
                        if (eventType == "Location" || eventType == "FSDJump" || eventType == "CarrierJump")
                        {
                            continue;
                        }
                        else if (eventType == "Loadout")
                        {
                            ProcessLoadoutEvent(journalLine, options);
                        }
                        else if (eventType == "Materials")
                        {
                            HandleMaterialsEvent(jsonDoc.RootElement);
                        }
                        else if (eventType == "ShipyardSwap")
                        {
                            ProcessShipyardSwapEvent(journalLine, options);
                        }
                        else if (eventType == "ShipyardNew")
                        {
                            ProcessShipyardNewEvent(journalLine, options);
                        }
                        else if (eventType == "ModuleSell" || eventType == "ModuleBuy" || eventType == "ModuleStore" || eventType == "ModuleRetrieve" || eventType == "ModuleSwap")
                        {
                            // These events indicate a loadout change. The subsequent 'Loadout' event is the source of truth.
                            // We don't need to take action here, but acknowledging the event is useful for debugging.
                            Trace.WriteLine($"[JournalWatcherService] Detected module change event: {eventType}. Awaiting next Loadout.");
                        }
                        else if (eventType == "CollectCargo")
                        {
                            // The 'Type' property contains the internal name of the commodity.
                            var commodity = jsonDoc.RootElement.TryGetProperty("Type", out var nameElement)
                                ? nameElement.GetString() : null;
                            if (commodity != null)
                            {
                                CargoCollected?.Invoke(this, new CargoCollectedEventArgs(commodity));
                            }
                        }
                        else if (eventType == "MiningRefined")
                        {
                            // Prefer the human-readable name, but fall back to the internal name if it's not available.
                            var commodity = jsonDoc.RootElement.TryGetProperty("Type_Localised", out var locElement) && !string.IsNullOrEmpty(locElement.GetString())
                                ? locElement.GetString()
                                : (jsonDoc.RootElement.TryGetProperty("Type", out var typeElement) ? typeElement.GetString() : null);
                            if (commodity != null)
                            {
                                MiningRefined?.Invoke(this, new MiningRefinedEventArgs(commodity));
                            }
                        }
                        else if (eventType == "LaunchDrone")
                        {
                            var type = jsonDoc.RootElement.TryGetProperty("Type", out var typeElement) ? typeElement.GetString() : null;
                            if (type != null)
                            {
                                LaunchDrone?.Invoke(this, new LaunchDroneEventArgs(type));
                            }
                        }
                        else if (eventType == "BuyDrones")
                        {
                            var buyEvent = JsonSerializer.Deserialize<BuyDronesEvent>(journalLine, options);
                            if (buyEvent != null)
                            {
                                BuyDrones?.Invoke(this, new BuyDronesEventArgs(buyEvent.Count, (long)buyEvent.TotalCost));
                            }
                        }
                        else if (eventType == "MarketBuy")
                        {
                            var buyEvent = JsonSerializer.Deserialize<MarketBuyEvent>(journalLine, options);
                            if (buyEvent != null)
                            {
                                MarketBuy?.Invoke(this, new MarketBuyEventArgs(buyEvent.Type, buyEvent.Count, buyEvent.TotalCost));
                            }
                        }
                        else if (eventType == "Docked")
                        {
                            var dockedEvent = JsonSerializer.Deserialize<DockedEvent>(journalLine, options);
                            if (dockedEvent != null)
                            {
                                _lastDockedEventArgs = new DockedEventArgs(dockedEvent);
                                Docked?.Invoke(this, _lastDockedEventArgs);
                            }
                        }
                        else if (eventType == "Undocked")
                        {
                            // The Undocked event just has a "StationName" property.
                            var stationName = jsonDoc.RootElement.TryGetProperty("StationName", out var nameElement) ? nameElement.GetString() : "Unknown";
                            // Clear the last docked state when we undock.
                            // This is crucial for the initial scan to correctly determine the player is no longer docked.
                            if (_lastDockedEventArgs != null)
                            { // This line was missing
                                Debug.WriteLine($"[JournalWatcherService] Undocked from {_lastDockedEventArgs.DockedEvent.StationName}. Clearing docked state.");
                            }
                            _lastDockedEventArgs = null;
                            Undocked?.Invoke(this, new UndockedEventArgs(stationName ?? "Unknown"));
                        }
                    }
                    catch (JsonException ex)
                    {
                        Trace.WriteLine($"[JournalWatcherService] Failed to parse journal line: {journalLine}. Error: {ex.Message}");
                        // Continue to the next line instead of breaking the loop.
                    }
                }
                }
                finally
                {
                    foreach (var (doc, _) in newEntries)
                    {
                        doc.Dispose();
                    }
                }
                _lastPosition = fs.Position;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JournalWatcherService] Error polling journal file: {ex}");
            }
        }

        private void UpdateShipInformation(string? shipName, string? shipIdent, string? shipType, string? internalShipName)
        {
            // Only raise an update event if something has actually changed.
            // The internalShipName is the most reliable indicator of a ship change.
            if (!string.IsNullOrEmpty(internalShipName) &&
                (shipName != _lastShipName || shipIdent != _lastShipIdent || shipType != _lastShipType || internalShipName != _lastInternalShipName))
            {
                _lastShipName = shipName;
                _lastShipIdent = shipIdent;
                _lastShipType = shipType;
                _lastInternalShipName = internalShipName; // This line was missing
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
    }
}