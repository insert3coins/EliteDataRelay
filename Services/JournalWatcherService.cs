using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using JournalEvents = EliteDataRelay.Models.Journal;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service for monitoring the Elite Dangerous journal for Loadout events to determine cargo capacity.
    /// </summary>
    public partial class JournalWatcherService : IJournalWatcherService, IDisposable
    {

        private readonly string _journalDir;
        private FileSystemWatcher? _journalDirectoryWatcher;
        private FileSystemWatcher? _navRouteWatcher;
        private System.Threading.Timer? _navRouteDebounce;
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
        private string? _lastShipLocalised;

        // Preserve the mothership details while temporarily in SRV/Fighter/On-Foot
        private string? _homeShipName;
        private string? _homeShipIdent;
        private string? _homeShipType;
        private string? _homeInternalShipName;
        private string? _homeShipLocalised;

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
        /// Event raised when limpet drones are purchased.
        /// </summary>
        public event EventHandler<BuyDronesEventArgs>? BuyDrones;

        /// <summary>
        /// Event raised when commodities are purchased from the market.
        /// </summary>
        public event EventHandler<MarketBuyEventArgs>? MarketBuy;
        public event EventHandler<JournalEvents.MaterialCollectedEventArgs>? MaterialCollected;
        public event EventHandler<JournalEvents.AsteroidCrackedEventArgs>? AsteroidCracked;
        public event EventHandler<JournalEvents.ProspectedAsteroidEventArgs>? ProspectedAsteroid;
        public event EventHandler<JournalEvents.SupercruiseExitEventArgs>? SupercruiseExit;
        public event EventHandler<JournalEvents.SupercruiseEntryEventArgs>? SupercruiseEntry;
        public event EventHandler<JournalEvents.MusicTrackEventArgs>? MusicTrackChanged;
        public event EventHandler<JournalEvents.ShutdownEventArgs>? Shutdown;
        public event EventHandler<JournalEvents.FileheaderEventArgs>? FileheaderRead;

        /// <summary>
        /// Event raised when an FSS discovery scan is performed.
        /// </summary>
        public event EventHandler<FSSDiscoveryScanEvent>? FSSDiscoveryScan;

        /// <summary>
        /// Event raised when a body is scanned.
        /// </summary>
        public event EventHandler<ScanEvent>? BodyScanned;

        /// <summary>
        /// Event raised when a detailed surface scan (SAA) is completed.
        /// </summary>
        public event EventHandler<SAAScanCompleteEvent>? SAAScanComplete;

        /// <summary>
        /// Event raised when FSS identifies signals on a body.
        /// </summary>
        public event EventHandler<FSSBodySignalsEvent>? FSSBodySignals;

        /// <summary>
        /// Event raised when SAA mapping finds signals on a body.
        /// </summary>
        public event EventHandler<SAASignalsFoundEvent>? SAASignalsFound;

        /// <summary>
        /// Event raised when FSS reports all bodies found in a system.
        /// </summary>
        public event EventHandler<FSSAllBodiesFoundEvent>? FSSAllBodiesFound;

        /// <summary>
        /// Event raised for legacy DiscoveryScan (pre-FSS) body count.
        /// </summary>
        public event EventHandler<DiscoveryScanEvent>? DiscoveryScan;

        /// <summary>
        /// Event raised when FSS discovers a non-body signal (USS/POI).
        /// </summary>
        public event EventHandler<FSSSignalDiscoveredEvent>? FSSSignalDiscovered;

        /// <summary>
        /// Event raised when a Nav Beacon scan provides system body count.
        /// </summary>
        public event EventHandler<NavBeaconScanEvent>? NavBeaconScan;

        /// <summary>
        /// Event raised when first footfall is achieved (Odyssey).
        /// </summary>
        public event EventHandler<FirstFootfallEvent>? FirstFootfall;

        /// <summary>
        /// Event raised when organic scan is performed (Odyssey).
        /// </summary>
        public event EventHandler<ScanOrganicEvent>? ScanOrganic;

        /// <summary>
        /// Event raised when organic data is sold (Vista Genomics).
        /// </summary>
        public event EventHandler<SellOrganicDataEvent>? SellOrganicData;

        /// <summary>
        /// Event raised for general Codex entries.
        /// </summary>
        public event EventHandler<CodexEntryEvent>? CodexEntry;

        /// <summary>
        /// Event raised when exploration data is sold.
        /// </summary>
        public event EventHandler<SellExplorationDataEvent>? SellExplorationData;

        /// <summary>
        /// Event raised when on-foot exploration data is sold.
        /// </summary>
        public event EventHandler<MultiSellExplorationDataEvent>? MultiSellExplorationData;

        /// <summary>
        /// Event raised when landing on a planet surface.
        /// </summary>
        public event EventHandler<TouchdownEvent>? Touchdown;
        public event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;

        /// <summary>
        /// Event raised when a new jump target is selected (FSDTarget event).
        /// </summary>
        public event EventHandler<NextJumpSystemChangedEventArgs>? NextJumpSystemChanged;

        /// <summary>
        /// Event raised when FSD starts charging for a hyperspace jump.
        /// </summary>
        public event EventHandler<JumpInitiatedEventArgs>? JumpInitiated;

        /// <summary>
        /// Event raised when the hyperspace jump completes (FSDJump event processed).
        /// </summary>
        public event EventHandler<JumpCompletedEventArgs>? JumpCompleted;

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
            TrySeedLastKnownLocation();
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

            // Watch NavRoute.json to keep next-hop info in sync (SrvSurvey-style)
            _navRouteWatcher = new FileSystemWatcher(_journalDir)
            {
                Filter = "NavRoute.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _navRouteWatcher.Changed += OnNavRouteChanged;
            _navRouteWatcher.Created += OnNavRouteChanged;

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
            _lastShipLocalised = null;
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
            _navRouteWatcher?.Dispose();
            _navRouteWatcher = null;
            _navRouteDebounce?.Dispose();
            _navRouteDebounce = null;

            _isMonitoring = false;
            Debug.WriteLine("[JournalWatcherService] Stopped monitoring");

            Reset();
        }

        private void OnNavRouteChanged(object sender, FileSystemEventArgs e)
        {
            // debounce rapid writes
            _navRouteDebounce?.Dispose();
            _navRouteDebounce = new System.Threading.Timer(_ =>
            {
                try { ProcessNavRouteFile(); }
                catch (Exception ex) { Debug.WriteLine($"[JournalWatcherService] NavRoute processing error: {ex.Message}"); }
            }, null, 50, System.Threading.Timeout.Infinite);
        }

        private void ProcessNavRouteFile()
        {
            var path = Path.Combine(_journalDir, "NavRoute.json");
            if (!File.Exists(path)) return;

            // handle file lock with small retries
            string json = string.Empty;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.UTF8);
                    json = sr.ReadToEnd();
                    break;
                }
                catch { System.Threading.Thread.Sleep(20); }
            }
            if (string.IsNullOrWhiteSpace(json)) return;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Route", out var routeEl) || routeEl.ValueKind != JsonValueKind.Array) return;

            int count = routeEl.GetArrayLength();
            if (count == 0) return;

            // find current system index in route
            string? currentName = _lastLocationArgs?.StarSystem ?? _lastStarSystem;
            long? currentAddr = _lastLocationArgs?.SystemAddress;
            int currentIdx = -1;
            for (int i = 0; i < count; i++)
            {
                var el = routeEl[i];
                string? name = el.TryGetProperty("StarSystem", out var ns) ? ns.GetString() : null;
                long? addr = null;
                if (el.TryGetProperty("SystemAddress", out var sa) && sa.TryGetInt64(out var sav)) addr = sav;
                if ((currentAddr.HasValue && addr.HasValue && addr.Value == currentAddr.Value) ||
                    (!string.IsNullOrEmpty(currentName) && string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase)))
                {
                    currentIdx = i;
                    break;
                }
            }

            int nextIdx;
            if (currentIdx >= 0 && currentIdx + 1 < count)
            {
                nextIdx = currentIdx + 1;
            }
            else if (currentIdx < 0 && count >= 2)
            {
                // If we cannot identify the current system, assume route[0] is current and route[1] is next
                nextIdx = 1;
            }
            else
            {
                nextIdx = 0;
            }
            var nextEl = routeEl[nextIdx];
            string? nextName = nextEl.TryGetProperty("StarSystem", out var nn) ? nn.GetString() : null;
            string? starClass = nextEl.TryGetProperty("StarClass", out var sc) ? sc.GetString() : null;

            // compute remaining jumps
            int? remaining = (currentIdx >= 0) ? (count - (currentIdx + 1)) : (count >= 1 ? count - 1 : (int?)null); // remaining including next
            if (remaining.HasValue && remaining.Value < 0) remaining = 0;

            // compute distance from current to next using StarPos if available
            double? jumpDist = null;
            try
            {
                if (currentIdx >= 0)
                {
                    var curEl = routeEl[currentIdx];
                    if (curEl.TryGetProperty("StarPos", out var cp) && nextEl.TryGetProperty("StarPos", out var np) &&
                        cp.ValueKind == JsonValueKind.Array && np.ValueKind == JsonValueKind.Array && cp.GetArrayLength() == 3 && np.GetArrayLength() == 3)
                    {
                        double cx = cp[0].GetDouble(); double cy = cp[1].GetDouble(); double cz = cp[2].GetDouble();
                        double nx = np[0].GetDouble(); double ny = np[1].GetDouble(); double nz = np[2].GetDouble();
                        jumpDist = Math.Sqrt(Math.Pow(nx - cx, 2) + Math.Pow(ny - cy, 2) + Math.Pow(nz - cz, 2));
                    }
                }
                else if (count >= 2)
                {
                    // Fallback: assume distance from route[0] to route[1]
                    var curEl = routeEl[0];
                    if (curEl.TryGetProperty("StarPos", out var cp) && nextEl.TryGetProperty("StarPos", out var np) &&
                        cp.ValueKind == JsonValueKind.Array && np.ValueKind == JsonValueKind.Array && cp.GetArrayLength() == 3 && np.GetArrayLength() == 3)
                    {
                        double cx = cp[0].GetDouble(); double cy = cp[1].GetDouble(); double cz = cp[2].GetDouble();
                        double nx = np[0].GetDouble(); double ny = np[1].GetDouble(); double nz = np[2].GetDouble();
                        jumpDist = Math.Sqrt(Math.Pow(nx - cx, 2) + Math.Pow(ny - cy, 2) + Math.Pow(nz - cz, 2));
                    }
                }
            }
            catch { /* ignore math/json issues */ }

            if (!string.IsNullOrEmpty(nextName))
            {
                var args = new NextJumpSystemChangedEventArgs(nextName!, starClass, jumpDist, remaining);
                NextJumpSystemChanged?.Invoke(this, args);
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

        private void PollTimer_Tick(object? state)
        {
            ProcessNewJournalEntries();
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

        private void TrySeedLastKnownLocation()
        {
            try
            {
                var latest = FindLatestJournalFile();
                if (latest == null || !File.Exists(latest))
                {
                    return;
                }

                _currentJournalFile = latest;
                foreach (var line in File.ReadLines(latest).Reverse())
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (ApplyLocationSeed(doc.RootElement))
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // ignore malformed lines
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Failed to seed last known location: {ex}");
            }
        }

        private bool ApplyLocationSeed(JsonElement root)
        {
            if (!root.TryGetProperty("event", out var evtProp))
            {
                return false;
            }

            var evtType = evtProp.GetString();
            if (evtType != "Location" && evtType != "FSDJump")
            {
                return false;
            }

            if (!root.TryGetProperty("StarSystem", out var starSystemProp))
            {
                return false;
            }

            var starSystem = starSystemProp.GetString();
            if (string.IsNullOrWhiteSpace(starSystem))
            {
                return false;
            }

            double[] starPos = Array.Empty<double>();
            if (root.TryGetProperty("StarPos", out var starPosElement) && starPosElement.ValueKind == JsonValueKind.Array)
            {
                try
                {
                    starPos = starPosElement.EnumerateArray().Select(p => p.GetDouble()).ToArray();
                }
                catch { starPos = Array.Empty<double>(); }
            }

            long? systemAddress = null;
            if (root.TryGetProperty("SystemAddress", out var addrElement) && addrElement.TryGetInt64(out var addr))
            {
                systemAddress = addr;
            }

            DateTime timestamp = DateTime.UtcNow;
            if (root.TryGetProperty("timestamp", out var tsElement) && tsElement.TryGetDateTime(out var ts))
            {
                timestamp = ts;
            }

            _lastStarSystem = starSystem;
            _lastLocationArgs = new LocationChangedEventArgs(starSystem!, starPos, true, systemAddress, timestamp);
            LocationChanged?.Invoke(this, _lastLocationArgs);
            return true;
        }
    }

}
