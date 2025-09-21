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
        private FileSystemWatcher? _watcher;
        private readonly System.Windows.Forms.Timer _pollTimer;
        private string? _currentJournalFile;
        private string? _lastStarSystem;
        private string? _lastCargoHash;
        private long _lastPosition;
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipIdent;
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

            _pollTimer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.PollingIntervalMs
            };
            _pollTimer.Tick += PollTimer_Tick;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring || string.IsNullOrEmpty(_journalDir) || !Directory.Exists(_journalDir)) return;

            InitializeFileSystemWatcher();
            SwitchToLatestJournal();
            _pollTimer.Start();
            _isMonitoring = true;
            Debug.WriteLine("[JournalWatcherService] Started monitoring");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _watcher?.Dispose();
            _watcher = null;
            _pollTimer.Stop();
            _isMonitoring = false;
            Debug.WriteLine("[JournalWatcherService] Stopped monitoring");
        }

        private void InitializeFileSystemWatcher()
        {
            _watcher = new FileSystemWatcher(_journalDir)
            {
                Filter = "Journal.*.log",
                NotifyFilter = NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _watcher.Created += OnJournalFileCreated;
        }

        private void OnJournalFileCreated(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"[JournalWatcherService] New journal file detected: {e.Name}");
            SwitchToLatestJournal();
        }

        private void SwitchToLatestJournal()
        {
            var latestJournal = FindLatestJournalFile();
            if (latestJournal != null && latestJournal != _currentJournalFile)
            {
                _currentJournalFile = latestJournal;
                _lastPosition = 0; // Reset position for new file
                Debug.WriteLine($"[JournalWatcherService] Switched to journal file: {_currentJournalFile}");
                ProcessNewJournalEntries(); // Process the whole file to find the last known cargo capacity
            }
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

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            ProcessNewJournalEntries();
        }

        private void ProcessNewJournalEntries()
        {
            if (_currentJournalFile == null || !File.Exists(_currentJournalFile))
            {
                SwitchToLatestJournal();
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
                            var loadoutEvent = JsonSerializer.Deserialize<LoadoutEvent>(line, options);
                            if (loadoutEvent != null)
                            {
                                if (loadoutEvent.CargoCapacity > 0)
                                {
                                    Debug.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                                    CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
                                }

                                var shipName = loadoutEvent.ShipName;
                                // The Loadout event contains the user-defined ship name and ident.
                                // It does not contain ShipLocalised, which was causing this to fail.
                                var shipIdent = loadoutEvent.ShipIdent;

                                // Check and update ship info
                                if (!string.IsNullOrEmpty(shipName) &&
                                    (shipName != _lastShipName || shipIdent != _lastShipIdent))
                                {
                                    _lastShipName = shipName;
                                    _lastShipIdent = shipIdent;
                                    Debug.WriteLine($"[JournalWatcherService] Found Ship Info in Loadout. Name: {shipName}, Ident: {shipIdent}");
                                    ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs(shipName, shipIdent));
                                }
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

                            var shipName = loadGameEvent.ShipName;
                            // Use ShipIdent for consistency with the Loadout event.
                            var shipIdent = loadGameEvent.ShipIdent;

                            // Check and update ship info
                            if (!string.IsNullOrEmpty(shipName) &&
                                (shipName != _lastShipName || shipIdent != _lastShipIdent))
                            {
                                _lastShipName = shipName;
                                _lastShipIdent = shipIdent;
                                Debug.WriteLine($"[JournalWatcherService] Found Ship Info in Game. Name: {shipName}, Ident: {shipIdent}");
                                ShipInfoChanged?.Invoke(this, new ShipInfoChangedEventArgs(shipName, shipIdent));
                            }
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
                                if (!string.IsNullOrEmpty(starSystem) && starSystem != _lastStarSystem)
                                {
                                    _lastStarSystem = starSystem;
                                    Debug.WriteLine($"[JournalWatcherService] Found Location event. StarSystem: {starSystem}");
                                    LocationChanged?.Invoke(this, new LocationChangedEventArgs(starSystem));
                                }
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

        public void Dispose()
        {
            StopMonitoring();
            _pollTimer?.Dispose();
            _watcher?.Dispose();
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

        public LocationChangedEventArgs(string starSystem) => StarSystem = starSystem;
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

        public ShipInfoChangedEventArgs(string shipName, string shipIdent)
        {
            ShipName = shipName;
            ShipIdent = shipIdent;
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
}