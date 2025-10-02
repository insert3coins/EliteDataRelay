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
    public partial class JournalWatcherService : IJournalWatcherService, IDisposable
    {
        private readonly string _journalDir;
        private FileSystemWatcher? _journalDirectoryWatcher;
        private System.Threading.Timer? _pollTimer;
        private string? _currentJournalFile;
        private string? _lastStarSystem;
        private string? _lastStatusHash;
        private long _lastPosition;
        private long _lastKnownBalance = -1;
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipIdent;
        private string? _lastShipType;
        private string? _lastInternalShipName;
        private LocationChangedEventArgs? _lastLocationArgs;
        private DockedEventArgs? _lastDockedEventArgs;
        private bool _isMonitoring;

        /// <summary>
        /// Event raised when the cargo capacity is found in a Loadout event.
        /// </summary>
        public event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;

        /// <summary>
        /// Event raised when the player's balance changes.
        /// </summary>
        public event EventHandler<BalanceChangedEventArgs>? BalanceChanged;

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
        /// Event raised when the Status.json file changes.
        /// </summary>
        public event EventHandler<StatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// Event raised when the ship information changes.
        /// </summary>
        public event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;

        /// <summary>
        /// Event raised when the player docks at a station or carrier.
        /// </summary>
        public event EventHandler<DockedEventArgs>? Docked;

        /// <summary>
        /// Event raised when the player undocks from a station or carrier.
        /// </summary>
        public event EventHandler<UndockedEventArgs>? Undocked;

        /// <summary>
        /// Event raised after the initial poll is complete when monitoring starts.
        /// </summary>
        public event EventHandler? InitialScanComplete;

        /// <summary>
        /// Event raised when cargo is collected.
        /// </summary>
        public event EventHandler<CargoCollectedEventArgs>? CargoCollected;

        /// <summary>
        /// Event raised when a commodity is refined.
        /// </summary>
        public event EventHandler<MiningRefinedEventArgs>? MiningRefined;

        /// <summary>
        /// Event raised when a limpet drone is launched.
        /// </summary>
        public event EventHandler<LaunchDroneEventArgs>? LaunchDrone;

        /// <summary>
        /// Event raised when commodities are sold on the market.
        /// </summary>
        public event EventHandler<MarketSellEventArgs>? MarketSell;

        /// <summary>
        /// Event raised when limpet drones are purchased.
        /// </summary>
        public event EventHandler<BuyDronesEventArgs>? BuyDrones;

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
            Reset();
            PollTimer_Tick(null);
            InitialScanComplete?.Invoke(this, EventArgs.Empty);

            // Set up a FileSystemWatcher for immediate detection of new journal files.
            // This is more responsive than relying solely on the polling timer.
            _journalDirectoryWatcher = new FileSystemWatcher(_journalDir)
            {
                Filter = "Journal.*.log",
                NotifyFilter = NotifyFilters.FileName, // We only care about new files being created.
                EnableRaisingEvents = true
            };
            _journalDirectoryWatcher.Created += OnJournalFileCreated;

            _pollTimer?.Change(AppConfiguration.PollingIntervalMs, AppConfiguration.PollingIntervalMs); // Start polling after an initial delay.
            _isMonitoring = true;
            Debug.WriteLine("[JournalWatcherService] Started monitoring");
        }

        /// <summary>
        /// Resets the internal state of the watcher. This clears the last known file position,
        /// hashes, and other cached data, forcing a full re-read on the next poll.
        /// </summary>
        public void Reset()
        {
            _currentJournalFile = null;
            _lastPosition = 0;
            _lastStarSystem = null;
            _lastStatusHash = null;
            _lastShipName = null;
            _lastShipIdent = null;
            _lastShipType = null;
            _lastInternalShipName = null;
            _lastKnownBalance = -1;
            _lastCommanderName = null;
            _lastLocationArgs = null;
            _lastDockedEventArgs = null;
            Debug.WriteLine("[JournalWatcherService] State has been reset.");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite); // Stop the timer.
            _journalDirectoryWatcher?.Dispose();
            _journalDirectoryWatcher = null;

            _isMonitoring = false;
            Debug.WriteLine("[JournalWatcherService] Stopped monitoring");

            Reset();
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
            ProcessStatusFile();
        }

        /// <summary>
        /// Handles the event when a new journal file is created. This triggers an
        /// immediate poll to process the new file, rather than waiting for the next timer tick.
        /// </summary>
        private void OnJournalFileCreated(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"[JournalWatcherService] FileSystemWatcher detected new journal: {e.Name}. Triggering immediate poll.");
            // Run the poll on a background thread to avoid holding up the FileSystemWatcher event.
            ThreadPool.QueueUserWorkItem(_ => PollTimer_Tick(null));
        }

        public void Dispose()
        {
            StopMonitoring();
            _pollTimer?.Dispose();
        }

        public LocationChangedEventArgs? GetLastKnownLocation()
        {
            return _lastLocationArgs;
        }

        public DockedEventArgs? GetLastKnownDockedState()
        {
            return _lastDockedEventArgs;
        }
    }
}