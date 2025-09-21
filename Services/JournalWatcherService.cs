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
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    try
                    {
                        // A more robust way of checking events rather than string.Contains
                        using var jsonDoc = JsonDocument.Parse(line);
                        if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                        {
                            continue;
                        }

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        string? eventType = eventElement.GetString();

                        if (eventType == "Loadout")
                        {
                            var loadoutEvent = JsonSerializer.Deserialize<ShipLoadout>(line, options);
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
                            var loadGameEvent = JsonSerializer.Deserialize<LoadGameEvent>(line, options);
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
                            var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(line, options);
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
                            var materialsEvent = JsonSerializer.Deserialize<MaterialsEvent>(line, options);
                            if (materialsEvent != null)
                            {
                                Debug.WriteLine($"[JournalWatcherService] Found Materials event.");
                                MaterialsEvent?.Invoke(this, new MaterialsEventArgs(materialsEvent));
                            }
                        }
                        else if (eventType == "MaterialCollected")
                        {
                            var collectedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(line, options);
                            if (collectedEvent != null)
                            {
                                Debug.WriteLine($"[JournalWatcherService] Found MaterialCollected event for {collectedEvent.Name}.");
                                MaterialCollectedEvent?.Invoke(this, new MaterialCollectedEventArgs(collectedEvent));
                            }
                        }
                        else if (eventType == "MaterialDiscarded")
                        {
                            var discardedEvent = JsonSerializer.Deserialize<MaterialCollectedEvent>(line, options);
                            if (discardedEvent != null)
                            {
                                MaterialDiscardedEvent?.Invoke(this, new MaterialCollectedEventArgs(discardedEvent));
                            }
                        }
                        else if (eventType == "MaterialTrade")
                        {
                            var tradeEvent = JsonSerializer.Deserialize<MaterialTradeEvent>(line, options);
                            if (tradeEvent != null)
                            {
                                MaterialTradeEvent?.Invoke(this, new MaterialTradeEventArgs(tradeEvent));
                            }
                        }
                        else if (eventType == "EngineerCraft")
                        {
                            var craftEvent = JsonSerializer.Deserialize<EngineerCraftEvent>(line, options);
                            if (craftEvent != null)
                            {
                                EngineerCraftEvent?.Invoke(this, new EngineerCraftEventArgs(craftEvent));
                            }
                        }
                        else if (eventType == "Location" || eventType == "FSDJump" || eventType == "CarrierJump")
                        {
                            if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var starSystemElement))
                            {
                                var starSystem = starSystemElement.GetString();
                                if (jsonDoc.RootElement.TryGetProperty("StarPos", out var starPosElement) &&
                                    starPosElement.ValueKind == JsonValueKind.Array)
                                {
                                    var starPos = starPosElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                                    if (!string.IsNullOrEmpty(starSystem) && starSystem != _lastStarSystem)
                                    {
                                        _lastStarSystem = starSystem;
                                        Debug.WriteLine($"[JournalWatcherService] Found Location event. StarSystem: {starSystem}");
                                        LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem, starPos));
                                    }
                                }
                            }
                        }
                        else if (eventType == "ShipyardSwap")
                        {
                            var swapEvent = JsonSerializer.Deserialize<ShipyardSwapEvent>(line, options);
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

                                var scanEvent = JsonSerializer.Deserialize<ScanEvent>(line, options);
                                if (scanEvent != null) ScanEvent?.Invoke(this, new ScanEventArgs(starSystem, scanEvent, starPos));
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[JournalWatcherService] Failed to parse journal line: {line}. Error: {ex.Message}");
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
        public double[] StarPos { get; }

        public LocationChangedEventArgs(string starSystem, double[] starPos)
        {
            StarSystem = starSystem;
            StarPos = starPos;
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
}