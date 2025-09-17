using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
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
        private bool _isMonitoring;

        /// <summary>
        /// Event raised when the cargo capacity is found in a Loadout event.
        /// </summary>
        public event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;

        /// <summary>
        /// Event raised when a 'Cargo' event is read from the journal.
        /// </summary>
        public event EventHandler<CargoInventoryEventArgs>? CargoInventoryChanged;

        /// <summary>
        /// Event raised when the player's location (StarSystem) changes.
        /// </summary>
        public event EventHandler<LocationChangedEventArgs>? LocationChanged;

        /// <summary>
        /// Gets whether the monitoring service is currently active.
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Gets the path to the journal directory being monitored.
        /// </summary>
        public string JournalDirectoryPath => _journalDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="JournalWatcherService"/> class.
        /// </summary>
        public JournalWatcherService()
        {
            _journalDir = AppConfiguration.JournalPath;

            _pollTimer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.PollingIntervalMs
            };
            _pollTimer.Tick += PollTimer_Tick;
        }

        /// <summary>
        /// Starts monitoring the journal files for relevant events.
        /// </summary>
        public async void StartMonitoring()
        {
            if (_isMonitoring || string.IsNullOrEmpty(_journalDir) || !Directory.Exists(_journalDir)) return;

            InitializeFileSystemWatcher();
            await SwitchToLatestJournal();
            _pollTimer.Start();
            _isMonitoring = true;
            Debug.WriteLine("[JournalWatcherService] Started monitoring");
        }

        /// <summary>
        /// Stops monitoring the journal files.
        /// </summary>
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

        private async void OnJournalFileCreated(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"[JournalWatcherService] New journal file detected: {e.Name}");
            await SwitchToLatestJournal();
        }

        private async Task SwitchToLatestJournal()
        {
            var latestJournal = FindLatestJournalFile();
            if (latestJournal != null && latestJournal != _currentJournalFile)
            {
                _currentJournalFile = latestJournal;
                _lastPosition = 0; // Reset position for new file
                Debug.WriteLine($"[JournalWatcherService] Switched to journal file: {_currentJournalFile}");
                await ProcessNewJournalEntries(); // Process the whole file to find the last known cargo capacity
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

        private async void PollTimer_Tick(object? sender, EventArgs e)
        {
            await ProcessNewJournalEntries();
        }

        private async Task ProcessNewJournalEntries()
        {
            if (_currentJournalFile == null || !File.Exists(_currentJournalFile))
            {
                await SwitchToLatestJournal();
                return;
            }

            try
            {
                using var fs = new FileStream(_currentJournalFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length <= _lastPosition) return;

                fs.Seek(_lastPosition, SeekOrigin.Begin);

                using var reader = new StreamReader(fs);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // A more robust way of checking events rather than string.Contains
                    using var jsonDoc = JsonDocument.Parse(line);
                    if (!jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                    {
                        continue;
                    }

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    string? eventType = eventElement.GetString();

                    if (eventType == "Loadout" && jsonDoc.RootElement.TryGetProperty("CargoCapacity", out _))
                    {
                        var loadoutEvent = JsonSerializer.Deserialize<LoadoutEvent>(line, options);
                        if (loadoutEvent?.CargoCapacity > 0)
                        {
                            Debug.WriteLine($"[JournalWatcherService] Found Loadout event. CargoCapacity: {loadoutEvent.CargoCapacity}");
                            CargoCapacityChanged?.Invoke(this, new CargoCapacityEventArgs(loadoutEvent.CargoCapacity));
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
                _lastPosition = fs.Position;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Error polling journal file: {ex}");
            }
        }

        /// <summary>
        /// Releases the resources used by the service.
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _pollTimer?.Dispose();
            _watcher?.Dispose();
        }

        /// <summary>
        /// Computes a SHA256 hash of the cargo snapshot to detect duplicates.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to hash.</param>
        /// <returns>A Base64-encoded string representing the hash.</returns>
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
        /// Gets the cargo snapshot from the journal event.
        /// </summary>
        public CargoSnapshot Snapshot { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CargoInventoryEventArgs"/> class.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot.</param>
        public CargoInventoryEventArgs(CargoSnapshot snapshot) => Snapshot = snapshot;
    }

    /// <summary>
    /// Provides data for the <see cref="IJournalWatcherService.LocationChanged"/> event.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current star system name.
        /// </summary>
        public string StarSystem { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationChangedEventArgs"/> class.
        /// </summary>
        /// <param name="starSystem">The name of the star system.</param>
        public LocationChangedEventArgs(string starSystem) => StarSystem = starSystem;
    }
}