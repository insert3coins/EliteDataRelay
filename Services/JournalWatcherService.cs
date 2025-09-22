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

        public void Dispose()
        {
            StopMonitoring();
            _pollTimer?.Dispose();
        }
    }
}