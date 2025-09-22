using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service for monitoring the Elite Dangerous journal for Loadout events to determine cargo capacity.
    /// </summary>
    public class JournalWatcherService : IJournalWatcherService, IDisposable
    {
        private readonly string _journalDir;
        private System.Threading.Timer? _pollTimer;
        private string? _currentJournalFile;
        private string? _lastStarSystem;
        private string? _lastCargoHash;
        private long _lastPosition;
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipIdent;
        private string? _lastShipType;
        private bool _isMonitoring;

        /// <summary>
        /// Event raised when the cargo capacity is found in a Loadout event.
        /// </summary>
        public event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;

        /// <summary>
        /// Event raised when the cargo inventory changes.
        /// </summary>
        public event EventHandler<CargoInventoryEventArgs>? CargoInventoryChanged;

        /// <summary>
        /// Event raised when the player's location (StarSystem) changes.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs>? LocationChanged;

        /// <summary>
        /// Event raised when the commander name is found.
        /// </summary>
        public event EventHandler<CommanderNameChangedEventArgs>? CommanderNameChanged;

        /// <summary>
        /// Event raised when a full ship loadout is available.
        /// </summary>
        public event EventHandler<LoadoutChangedEventArgs>? LoadoutChanged;

        /// <summary>
        /// Event raised when the ship information changes.
        /// </summary>
        public event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;

        /// <summary>
        /// Event raised for a full materials list snapshot.
        /// </summary>
        public event EventHandler<MaterialsEventArgs>? MaterialsEvent;

        /// <summary>
        /// Event raised when a material is collected.
        /// </summary>
        public event EventHandler<MaterialCollectedEventArgs>? MaterialCollectedEvent;

        /// <summary>
        /// Event raised when a material is discarded.
        /// </summary>
        public event EventHandler<MaterialCollectedEventArgs>? MaterialDiscardedEvent;

        /// <summary>
        /// Event raised when materials are traded or used in crafting.
        /// </summary>
        public event EventHandler<MaterialTradeEventArgs>? MaterialTradeEvent;
        public event EventHandler<EngineerCraftEventArgs>? EngineerCraftEvent;

        /// <summary>
        /// Event raised when a celestial body is scanned.
        /// </summary>
        public event EventHandler<ScanEventArgs>? ScanEvent;

        /// <summary>
        /// Event raised when a dockable body (station, outpost, etc.) is found.
        /// </summary>
        public event EventHandler<DockableBodyEventArgs>? DockableBodyFound;

        /// <summary>
        /// Gets whether the monitoring service is currently active.
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Gets the path to the journal directory being monitored.
        /// </summary>
        public string JournalDirectoryPath => _journalDir;

        public JournalWatcherService()
        {
            _journalDir = AppConfiguration.JournalPath;
            // Use a threading timer for background polling to avoid blocking the UI thread.
            _pollTimer = new System.Threading.Timer(PollTimer_Tick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMonitoring()
        {
            if (_isMonitoring || string.IsNullOrEmpty(_journalDir) || !Directory.Exists(_journalDir)) return;

            // Reset state and do an initial poll immediately to get the current state.
            // The timer will then continue at the configured interval.
            ResetState();
            PollTimer_Tick(null);

            _pollTimer?.Change(AppConfiguration.PollingIntervalMs, AppConfiguration.PollingIntervalMs); // Start polling after an initial delay.
            _isMonitoring = true;
            Debug.WriteLine("[JournalWatcherService] Started monitoring");
        }

        private void ResetState()
        {
            _currentJournalFile = null;
            _lastPosition = 0;
            _lastStarSystem = null;
            _lastCargoHash = null;
            _lastCommanderName = null;
            _lastShipName = null;
            _lastShipIdent = null;
            _lastShipType = null;
            Debug.WriteLine("[JournalWatcherService] Service state has been reset.");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite); // Stop the timer.
            _isMonitoring = false;
            Debug.WriteLine("[JournalWatcherService] Stopped monitoring");

            ResetState();
        }
        private string? FindLatestJournalFile()
        {
            try
            {
                return Directory.EnumerateFiles(_journalDir, "Journal.*.log")
                                .OrderByDescending(f => f)
                                .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Error finding latest journal file: {ex}");
                return null;
            }
        }

        private void PollTimer_Tick(object? state)
        {
            ProcessNewJournalEntries();
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
                                List<StationInfo>? stations = null;
                                if (jsonDoc.RootElement.TryGetProperty("Stations", out var stationsElement) && stationsElement.ValueKind == JsonValueKind.Array)
                                {
                                    stations = new List<StationInfo>();
                                    foreach (var stationJson in stationsElement.EnumerateArray())
                                    {
                                        stationJson.TryGetProperty("Name", out var nameElement);
                                        stationJson.TryGetProperty("StationType", out var typeElement);
                                        if (nameElement.GetString() is string name && typeElement.GetString() is string type)
                                        {
                                            stations.Add(new StationInfo { Name = name, Type = type });
                                        }
                                    }
                                }

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
                                    LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), isNewSystem, stations, systemAddress));
                                }
                                else
                                {
                                    // For subsequent "Location" events within the same system (e.g., dropping from supercruise),
                                    // we still need to provide an update to ensure the SystemAddress is current, but we don't
                                    // need to re-process the station list.
                                    LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem, starPos ?? Array.Empty<double>(), false, null, systemAddress));
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
                        else if (eventType == "Scan")
                        {
                            // The Scan event contains the system name, so we can pass it directly.
                            if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var systemElement) &&
                                systemElement.GetString() is string starSystem)
                            {
                                double[]? starPos = null;
                                if (jsonDoc.RootElement.TryGetProperty("StarPos", out var starPosElement) && starPosElement.ValueKind == JsonValueKind.Array)
                                {
                                    starPos = starPosElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                                }

                                var scanEvent = JsonSerializer.Deserialize<ScanEvent>(journalLine, options);
                                if (scanEvent != null) ScanEvent?.Invoke(this, new ScanEventArgs(starSystem, scanEvent, starPos));
                            }
                        }
                        else if (eventType == "DockableBody")
                        {
                            if (jsonDoc.RootElement.TryGetProperty("StationName", out var stationNameElement) &&
                                jsonDoc.RootElement.TryGetProperty("StationType", out var stationTypeElement) &&
                                jsonDoc.RootElement.TryGetProperty("SystemAddress", out var systemAddressElement) &&
                                stationNameElement.GetString() is string stationName &&
                                stationTypeElement.GetString() is string stationType &&
                                !string.IsNullOrEmpty(stationName))
                            {
                                long systemAddress = systemAddressElement.GetInt64();
                                Debug.WriteLine($"[JournalWatcherService] Found DockableBody: {stationName}");
                                DockableBodyFound?.Invoke(this, new DockableBodyEventArgs(stationName, stationType, systemAddress));
                            }
                        }
                        else if (eventType == "FSSSignalDiscovered")
                        {
                            if (jsonDoc.RootElement.TryGetProperty("IsStation", out var isStationElement) && isStationElement.GetBoolean())
                            {
                                if (jsonDoc.RootElement.TryGetProperty("SignalName", out var signalNameElement) &&                                    
                                    jsonDoc.RootElement.TryGetProperty("SystemAddress", out var systemAddressElement) &&
                                    signalNameElement.GetString() is string signalName &&
                                    !string.IsNullOrEmpty(signalName) &&
                                    systemAddressElement.TryGetInt64(out long systemAddress))
                                {
                                    // Prefer the localised name if it exists, otherwise fall back to the signal name.
                                    string stationName = signalName;
                                    if (jsonDoc.RootElement.TryGetProperty("SignalName_Localised", out var localisedNameElement) &&
                                        localisedNameElement.GetString() is string localisedName &&
                                        !string.IsNullOrEmpty(localisedName))
                                    {
                                        stationName = localisedName;
                                    }

                                    string stationType = "Station";
                                    if (jsonDoc.RootElement.TryGetProperty("SignalType", out var signalTypeElement) && signalTypeElement.GetString() is string type)
                                    {
                                        stationType = type;
                                    }

                                    Debug.WriteLine($"[JournalWatcherService] Found FSSSignalDiscovered (Station): {stationName}");
                                    DockableBodyFound?.Invoke(this, new DockableBodyEventArgs(stationName, stationType, systemAddress));
                                }
                            }
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

        public void Dispose()
        {
            StopMonitoring();
            _pollTimer?.Dispose();
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
    }

    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.CargoInventoryChanged"/> event.
    /// </summary>
    public class CargoInventoryEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the cargo snapshot.
        /// </summary>
        public CargoSnapshot Snapshot { get; }

        public CargoInventoryEventArgs(CargoSnapshot snapshot) => Snapshot = snapshot;
    }

    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.LocationChanged"/> event.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current star system.
        /// </summary>
        public string StarSystem { get; }
        public bool IsNewSystem { get; }
        public double[] StarPos { get; }
        public List<StationInfo>? Stations { get; }
        public long? SystemAddress { get; }

        public LocationChangedEventArgs(string starSystem, double[] starPos, bool isNewSystem, List<StationInfo>? stations, long? systemAddress)
        {
            StarSystem = starSystem;
            IsNewSystem = isNewSystem;
            StarPos = starPos;
            Stations = stations;
            SystemAddress = systemAddress;
        }
    }

    /// <summary>
    /// Provides data for the CommanderNameChanged event.
    /// </summary>
    public class CommanderNameChangedEventArgs : EventArgs
    {
        public string CommanderName { get; }
        public CommanderNameChangedEventArgs(string commanderName)
        {
            CommanderName = commanderName;
        }
    }

    /// <summary>
    /// Provides data for the ShipInfoChanged event.
    /// </summary>
    public class ShipInfoChangedEventArgs : EventArgs
    {
        public string ShipName { get; }
        public string ShipIdent { get; }
        public string ShipType { get; }
        public string InternalShipName { get; }

        public ShipInfoChangedEventArgs(string shipName, string shipIdent, string shipType, string internalShipName)
        {
            ShipName = shipName;
            ShipIdent = shipIdent;
            ShipType = shipType;
            InternalShipName = internalShipName;
        }
    }

    public class LoadoutChangedEventArgs : EventArgs
    {
        public ShipLoadout Loadout { get; }
        public LoadoutChangedEventArgs(ShipLoadout loadout)
        {
            Loadout = loadout;
        }
    }

    #region Material Event Args

    public class MaterialsEventArgs : EventArgs
    {
        public MaterialsEvent EventData { get; }
        public MaterialsEventArgs(MaterialsEvent eventData) => EventData = eventData;
    }

    public class MaterialCollectedEventArgs : EventArgs
    {
        public MaterialCollectedEvent EventData { get; }
        public MaterialCollectedEventArgs(MaterialCollectedEvent eventData) => EventData = eventData;
    }

    public class MaterialTradeEventArgs : EventArgs
    {
        public MaterialTradeEvent EventData { get; }
        public MaterialTradeEventArgs(MaterialTradeEvent eventData) => EventData = eventData;
    }

    public class EngineerCraftEventArgs : EventArgs
    {
        public EngineerCraftEvent EventData { get; }
        public EngineerCraftEventArgs(EngineerCraftEvent eventData) => EventData = eventData;
    }

    #endregion

    public class ScanEventArgs : EventArgs
    {
        public string StarSystem { get; }
        public ScanEvent ScanData { get; }
        public double[]? StarPos { get; }

        public ScanEventArgs(string starSystem, ScanEvent scanData, double[]? starPos)
        {
            StarSystem = starSystem;
            ScanData = scanData;
            StarPos = starPos;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.DockableBodyFound"/> event.
    /// </summary>
    public class DockableBodyEventArgs : EventArgs
    {
        public string StationName { get; }
        public string StationType { get; }
        public long SystemAddress { get; }

        public DockableBodyEventArgs(string stationName, string stationType, long systemAddress)
        {
            StationName = stationName;
            StationType = stationType;
            SystemAddress = systemAddress;
        }
    }

    public class StationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}