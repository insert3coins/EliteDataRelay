using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Models.Mining;
using EliteDataRelay.Models.Journal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public sealed class MiningTrackerService : IDisposable
    {
        private readonly IJournalWatcherService _journalWatcher;
        private readonly List<MiningSession> _sessions = new();
        private readonly HashSet<string> _sessionHashes = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _legacyHistoryFilePath;
        private readonly string _historyDirectory;
        private readonly object _fileSync = new();
        private readonly JsonSerializerOptions _writerOptions = new() { WriteIndented = true };
        private readonly JsonSerializerOptions _readerOptions = new() { PropertyNameCaseInsensitive = true };
        private MiningSession? _currentSession;
        private MiningProspector? _latestProspector;
        private bool _disposed;

        public MiningTrackerService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
            _legacyHistoryFilePath = Path.Combine(AppConfiguration.AppDataPath, "mining_sessions.json");
            _historyDirectory = Path.Combine(AppConfiguration.AppDataPath, "mining_sessions");
            LoadPersistedSessions();

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
            _journalWatcher.MaterialCollected += OnMaterialCollected;
            _journalWatcher.ProspectedAsteroid += OnProspectedAsteroid;
        }

        public event EventHandler? CurrentSessionUpdated;
        public event EventHandler? SessionsUpdated;
        public event EventHandler? LatestProspectorUpdated;
        public event EventHandler<bool>? LiveStateChanged;

        public bool IsLive { get; private set; }
        public MiningSession? CurrentSession => _currentSession;
        public IReadOnlyList<MiningSession> Sessions => _sessions;
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
            _journalWatcher.MaterialCollected -= OnMaterialCollected;
            _journalWatcher.ProspectedAsteroid -= OnProspectedAsteroid;
        }

        private void OnInitialScanComplete(object? sender, EventArgs e)
        {
            IsLive = true;
            LiveStateChanged?.Invoke(this, true);
            TriggerCurrentSessionEvent();
            TriggerSessionsEvent();
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

            var clone = _currentSession.Clone();
            _sessions.Add(clone);
            PersistSession(clone);

            _currentSession = null;
            _latestProspector = null;
            TriggerProspectorEvent();
            TriggerCurrentSessionEvent();
            TriggerSessionsEvent();
        }

        private void TriggerCurrentSessionEvent()
        {
            if (!IsLive)
            {
                return;
            }

            CurrentSessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerSessionsEvent()
        {
            if (!IsLive)
            {
                return;
            }

            SessionsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void TriggerProspectorEvent()
        {
            if (!IsLive)
            {
                return;
            }

            LatestProspectorUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void LoadPersistedSessions()
        {
            try
            {
                Directory.CreateDirectory(_historyDirectory);

                var existingFiles = Directory.EnumerateFiles(_historyDirectory, "*.json")
                                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                             .ToList();

                _sessions.Clear();
                _sessionHashes.Clear();
                var knownHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in existingFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        if (string.IsNullOrWhiteSpace(json)) continue;
                        var session = JsonSerializer.Deserialize<MiningSession>(json, _readerOptions);
                        if (session == null) continue;
                        var hash = BuildSessionHash(session);
                        if (knownHashes.Add(hash))
                        {
                            _sessions.Add(session);
                            _sessionHashes.Add(hash);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[MiningTrackerService] Failed to load session file '{file}': {ex.Message}");
                    }
                }

                _sessions.Sort((a, b) => DateTime.Compare(a.TimeStarted, b.TimeStarted));

                // Migrate legacy aggregated history if it still exists
                if (File.Exists(_legacyHistoryFilePath))
                {
                    try
                    {
                        var legacyJson = File.ReadAllText(_legacyHistoryFilePath);
                        if (!string.IsNullOrWhiteSpace(legacyJson))
                        {
                            var legacySessions = JsonSerializer.Deserialize<List<MiningSession>>(legacyJson, _readerOptions);
                            if (legacySessions != null)
                            {
                                foreach (var session in legacySessions)
                                {
                                    var hash = BuildSessionHash(session);
                                    if (knownHashes.Add(hash))
                                    {
                                        _sessions.Add(session);
                                        _sessionHashes.Add(hash);
                                        PersistSession(session);
                                    }
                                }
                                _sessions.Sort((a, b) => DateTime.Compare(a.TimeStarted, b.TimeStarted));
                            }
                        }

                        var backupPath = _legacyHistoryFilePath + ".bak";
                        File.Move(_legacyHistoryFilePath, backupPath, overwrite: true);
                        Trace.WriteLine($"[MiningTrackerService] Migrated legacy mining history to individual files. Backup saved at {backupPath}");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[MiningTrackerService] Failed to migrate legacy mining history: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MiningTrackerService] Failed to load mining history: {ex.Message}");
            }
        }

        private void PersistSession(MiningSession session)
        {
            try
            {
                lock (_fileSync)
                {
                    Directory.CreateDirectory(_historyDirectory);
                    var fileName = BuildSessionFileName(session);
                    var path = Path.Combine(_historyDirectory, fileName);
                    int counter = 1;
                    while (File.Exists(path))
                    {
                        var altName = $"{Path.GetFileNameWithoutExtension(fileName)}_{counter++}.json";
                        path = Path.Combine(_historyDirectory, altName);
                    }

                    var json = JsonSerializer.Serialize(session.Clone(), _writerOptions);
                    File.WriteAllText(path, json);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[MiningTrackerService] Failed to persist session: {ex.Message}");
            }
        }

        private static string BuildSessionHash(MiningSession session)
        {
            string start = session.TimeStarted == DateTime.MaxValue ? DateTime.MinValue.ToString("O") : session.TimeStarted.ToUniversalTime().ToString("O");
            string end = session.TimeFinished == DateTime.MaxValue ? DateTime.MaxValue.ToUniversalTime().ToString("O") : session.TimeFinished.ToUniversalTime().ToString("O");
            string system = session.StarSystem ?? string.Empty;
            string location = session.Location ?? string.Empty;
            return $"{start}|{end}|{system}|{location}|{session.AsteroidsProspected}|{session.AsteroidsCracked}";
        }

        private static string BuildSessionFileName(MiningSession session)
        {
            var start = session.TimeStarted == DateTime.MaxValue ? DateTime.UtcNow : session.TimeStarted.ToUniversalTime();
            var end = session.TimeFinished == DateTime.MaxValue ? DateTime.UtcNow : session.TimeFinished.ToUniversalTime();
            string identifier = $"{start:yyyyMMdd_HHmmss}_{end:yyyyMMdd_HHmmss}";
            string system = SanitizePathComponent(session.StarSystem ?? "Unknown");
            string location = SanitizePathComponent(session.Location ?? "Unknown");
            return $"session_{identifier}_{system}_{location}.json";
        }

        private static string SanitizePathComponent(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value.Replace(' ', '_');
        }
    }
}
