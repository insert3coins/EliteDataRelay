using EliteDataRelay.Models;
using EliteDataRelay.Models.Mining;
using EliteDataRelay.Models.Journal;
using System;
using System.Linq;

namespace EliteDataRelay.Services
{
    public sealed class MiningTrackerService : IDisposable
    {
        private readonly IJournalWatcherService _journalWatcher;
        private MiningSession? _currentSession;
        private MiningSession? _lastCompletedSession;
        private MiningProspector? _latestProspector;
        private bool _disposed;

        public MiningTrackerService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));

            _journalWatcher.InitialScanComplete += OnInitialScanComplete;
            _journalWatcher.SupercruiseExit += OnSupercruiseExit;
            _journalWatcher.SupercruiseEntry += OnSupercruiseEntry;
            _journalWatcher.LocationChanged += OnLocationChanged;
            _journalWatcher.JumpInitiated += OnJumpInitiated;
            _journalWatcher.JumpCompleted += OnJumpCompleted;
            _journalWatcher.MusicTrackChanged += OnMusicTrackChanged;
            _journalWatcher.Shutdown += OnShutdown;
            _journalWatcher.FileheaderRead += OnFileheaderRead;
            _journalWatcher.AsteroidCracked += OnAsteroidCracked;
            _journalWatcher.LaunchDrone += OnLaunchDrone;
            _journalWatcher.MiningRefined += OnMiningRefined;
            _journalWatcher.CargoCollected += OnCargoCollected;
            _journalWatcher.MaterialCollected += OnMaterialCollected;
            _journalWatcher.ProspectedAsteroid += OnProspectedAsteroid;
        }

        public event EventHandler? CurrentSessionUpdated;
        public event EventHandler? LatestProspectorUpdated;
        public event EventHandler<bool>? LiveStateChanged;

        public bool IsLive { get; private set; }
        public MiningSession? CurrentSession => _currentSession;
        public MiningSession? LastKnownSession => _currentSession ?? _lastCompletedSession;
        public MiningProspector? LatestProspector => _latestProspector;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _journalWatcher.InitialScanComplete -= OnInitialScanComplete;
            _journalWatcher.SupercruiseExit -= OnSupercruiseExit;
            _journalWatcher.SupercruiseEntry -= OnSupercruiseEntry;
            _journalWatcher.LocationChanged -= OnLocationChanged;
            _journalWatcher.JumpInitiated -= OnJumpInitiated;
            _journalWatcher.JumpCompleted -= OnJumpCompleted;
            _journalWatcher.MusicTrackChanged -= OnMusicTrackChanged;
            _journalWatcher.Shutdown -= OnShutdown;
            _journalWatcher.FileheaderRead -= OnFileheaderRead;
            _journalWatcher.AsteroidCracked -= OnAsteroidCracked;
            _journalWatcher.LaunchDrone -= OnLaunchDrone;
            _journalWatcher.MiningRefined -= OnMiningRefined;
            _journalWatcher.CargoCollected -= OnCargoCollected;
            _journalWatcher.MaterialCollected -= OnMaterialCollected;
            _journalWatcher.ProspectedAsteroid -= OnProspectedAsteroid;
        }

        private void OnInitialScanComplete(object? sender, EventArgs e)
        {
            IsLive = true;
            LiveStateChanged?.Invoke(this, true);
            TriggerCurrentSessionEvent();
            TriggerProspectorEvent();
        }

        private void OnSupercruiseExit(object? sender, SupercruiseExitEventArgs e)
        {
            if (!IsMiningBody(e.BodyType))
            {
                // Leaving any previous session if we drop somewhere else.
                CheckSession(e.Timestamp);
                return;
            }

            _currentSession = new MiningSession(e.StarSystem, e.Body, e.SystemAddress, e.BodyId);
            _latestProspector = null;
            TriggerProspectorEvent();
            TriggerCurrentSessionEvent();
        }

        private void OnSupercruiseEntry(object? sender, SupercruiseEntryEventArgs e)
        {
            CheckSession(e.Timestamp);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            // JournalWatcher raises LocationChanged for every event containing StarSystem;
            // only end the session when we actually enter a new system.
            if (!e.IsNewSystem)
            {
                return;
            }
            CheckSession(e.Timestamp);
        }

        private void OnJumpInitiated(object? sender, JumpInitiatedEventArgs e)
        {
            CheckSession(DateTime.UtcNow);
        }

        private void OnJumpCompleted(object? sender, JumpCompletedEventArgs e)
        {
            CheckSession(DateTime.UtcNow);
        }

        private void OnMusicTrackChanged(object? sender, MusicTrackEventArgs e)
        {
            if (string.Equals(e.Track, "MainMenu", StringComparison.OrdinalIgnoreCase))
            {
                CheckSession(e.Timestamp);
            }
        }

        private void OnShutdown(object? sender, ShutdownEventArgs e)
        {
            CheckSession(e.Timestamp);
        }

        private void OnFileheaderRead(object? sender, FileheaderEventArgs e)
        {
            CheckSession(e.Timestamp);
        }

        private void OnAsteroidCracked(object? sender, AsteroidCrackedEventArgs e)
        {
            if (_currentSession == null) return;
            _currentSession.CheckStartTime(e.Timestamp);
            _currentSession.AsteroidsCracked++;
            TriggerCurrentSessionEvent();
        }

        private void OnLaunchDrone(object? sender, LaunchDroneEventArgs e)
        {
            if (_currentSession == null) return;
            if (IsCollectorDrone(e.Type))
            {
                _currentSession.CollectorsDeployed++;
            }
            else if (string.Equals(e.Type, "Prospector", StringComparison.OrdinalIgnoreCase))
            {
                _currentSession.ProspectorsFired++;
            }
            _currentSession.CheckStartTime(DateTime.UtcNow);
            TriggerCurrentSessionEvent();
        }

        private static bool IsCollectorDrone(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;
            return string.Equals(type, "Collector", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(type, "Collection", StringComparison.OrdinalIgnoreCase);
        }

        private void OnMiningRefined(object? sender, MiningRefinedEventArgs e)
        {
            if (_currentSession == null) return;
            _currentSession.CheckStartTime(DateTime.UtcNow);
            _currentSession.AddOre(e.CommodityType);
            TriggerCurrentSessionEvent();
        }

        private void OnMaterialCollected(object? sender, MaterialCollectedEventArgs e)
        {
            if (_currentSession == null) return;
            _currentSession.AddMaterial(e);
            TriggerCurrentSessionEvent();
        }

        private void OnCargoCollected(object? sender, CargoCollectedEventArgs e)
        {
            if (_currentSession == null) return;

            var friendly = MiningNameHelper.NormalizeName(e.Commodity);
            if (string.IsNullOrWhiteSpace(friendly))
            {
                return;
            }

            var known = _currentSession.Items.FirstOrDefault(item =>
                string.Equals(item.Name, friendly, StringComparison.OrdinalIgnoreCase));

            if (known == null || known.Type != MiningItemType.Ore)
            {
                return;
            }

            known.CollectedCount++;
            TriggerCurrentSessionEvent();
        }

        private void OnProspectedAsteroid(object? sender, ProspectedAsteroidEventArgs e)
        {
            if (_currentSession == null) return;

            _currentSession.CheckStartTime(e.Timestamp);
            _currentSession.AddAsteroid(e);

            if (_latestProspector != null)
            {
                _currentSession.AddProspector(_latestProspector);
            }

            var materials = e.Materials.Select(m => new MiningMaterial(MiningNameHelper.NormalizeName(m.Name, m.LocalisedName), m.Proportion)).ToList();
            var content = MapContent(e.Content);
            var motherlode = string.IsNullOrWhiteSpace(e.MotherlodeMaterial) ? null : MiningNameHelper.NormalizeName(e.MotherlodeMaterial);
            _latestProspector = new MiningProspector(materials, content, motherlode, e.Remaining);

            TriggerCurrentSessionEvent();
            TriggerProspectorEvent();
        }

        private static MiningContent MapContent(string content)
        {
            return content switch
            {
                "$AsteroidMaterialContent_High;" => MiningContent.High,
                "$AsteroidMaterialContent_Medium;" => MiningContent.Medium,
                _ => MiningContent.Low
            };
        }

        private static bool IsMiningBody(string? bodyType)
        {
            if (string.IsNullOrWhiteSpace(bodyType))
            {
                return false;
            }

            return bodyType.Equals("PlanetaryRing", StringComparison.OrdinalIgnoreCase)
                || bodyType.Equals("AsteroidCluster", StringComparison.OrdinalIgnoreCase)
                || bodyType.Equals("AsteroidBeltCluster", StringComparison.OrdinalIgnoreCase)
                || bodyType.Equals("AsteroidBelt", StringComparison.OrdinalIgnoreCase);
        }

        private void CheckSession(DateTime timestamp)
        {
            if (_currentSession == null)
            {
                return;
            }

            if (!_currentSession.HasData)
            {
                _currentSession = null;
                _latestProspector = null;
                TriggerProspectorEvent();
                TriggerCurrentSessionEvent();
                return;
            }

            _currentSession.TimeFinished = timestamp;
            if (_latestProspector != null)
            {
                _currentSession.AddProspector(_latestProspector);
            }

            _lastCompletedSession = _currentSession.Clone();
            _currentSession = null;
            _latestProspector = null;
            TriggerProspectorEvent();
            TriggerCurrentSessionEvent();
        }

        private void TriggerCurrentSessionEvent()
        {
            if (!IsLive)
            {
                return;
            }

            CurrentSessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerProspectorEvent()
        {
            if (!IsLive)
            {
                return;
            }

            LatestProspectorUpdated?.Invoke(this, EventArgs.Empty);
        }

    }
}
