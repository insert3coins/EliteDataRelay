using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public class VisitedSystemsService : IVisitedSystemsService
    {
        private readonly IJournalWatcherService _journalWatcher;
        private readonly string _filePath;
        private readonly Dictionary<string, StarSystem> _systems = new Dictionary<string, StarSystem>(StringComparer.InvariantCultureIgnoreCase);
        private bool _isStarted;

        public event EventHandler? SystemsUpdated;
        public event EventHandler<JournalScanCompletedEventArgs>? JournalScanCompleted;
        public IReadOnlyList<StarSystem> VisitedSystems => _systems.Values.ToList();

        public VisitedSystemsService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
            // Use the centralized path from AppConfiguration
            _filePath = Path.Combine(AppConfiguration.AppDataPath, "systemsvisited.json");
        }

        public void Start()
        {
            if (_isStarted) return;
            LoadSystems();
            _journalWatcher.LocationChanged += OnLocationChanged;
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _journalWatcher.LocationChanged -= OnLocationChanged;
            SaveSystems();
            _isStarted = false;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (e.StarPos?.Length == 3)
            {
                AddOrUpdateSystem(e.StarSystem, e.StarPos, true);
            }
        }

        private void AddOrUpdateSystem(string name, double[] coords, bool triggerUpdate)
        {
            if (string.IsNullOrWhiteSpace(name) || coords.Length != 3) return;

            if (!_systems.ContainsKey(name))
            {
                var newSystem = new StarSystem
                {
                    Name = name,
                    X = coords[0],
                    Y = coords[1],
                    Z = coords[2]
                };
                _systems[name] = newSystem;
                Debug.WriteLine($"[VisitedSystemsService] Added new system: {name}");
                if (triggerUpdate)
                {
                    SystemsUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public async Task ScanAllJournalsAsync()
        {
            await Task.Run(() =>
            {
                int filesScanned = 0;
                int systemsAdded = 0;
                try
                {
                    var journalPath = _journalWatcher.JournalDirectoryPath;
                    if (string.IsNullOrWhiteSpace(journalPath) || !Directory.Exists(journalPath))
                    {
                        string errorMessage = $"Journal scan failed. Directory path is invalid or does not exist: '{journalPath}'";
                        Debug.WriteLine($"[VisitedSystemsService] {errorMessage}");
                        JournalScanCompleted?.Invoke(this, new JournalScanCompletedEventArgs(errorMessage));
                        return;
                    }

                    Debug.WriteLine($"[VisitedSystemsService] Starting full journal scan in directory: '{journalPath}'");
                    // Use GetFiles to immediately retrieve the full list of files, making the process more robust.
                    var journalFiles = Directory.GetFiles(journalPath, "Journal.*.log")
                                                .OrderBy(f => f)
                                                .ToList();
                    filesScanned = journalFiles.Count;

                    Debug.WriteLine($"[VisitedSystemsService] Found {journalFiles.Count} journal files to scan.");

                    foreach (var file in journalFiles)
                    {
                        Debug.WriteLine($"[VisitedSystemsService] Scanning file: {Path.GetFileName(file)}");
                        try
                        {
                            var lines = File.ReadLines(file);
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                try
                                {
                                    using var jsonDoc = JsonDocument.Parse(line);
                                    if (jsonDoc.RootElement.TryGetProperty("event", out var eventElement))
                                    {
                                        string? eventType = eventElement.GetString();
                                        if (eventType == "Location" || eventType == "FSDJump" || eventType == "CarrierJump")
                                        {
                                            if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var systemElement) &&
                                                jsonDoc.RootElement.TryGetProperty("StarPos", out var posElement) &&
                                                posElement.ValueKind == JsonValueKind.Array)
                                            {
                                                var name = systemElement.GetString();
                                                var coords = posElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                                                if (!string.IsNullOrEmpty(name) && !_systems.ContainsKey(name))
                                                {
                                                    AddOrUpdateSystem(name, coords, false);
                                                    systemsAdded++;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (JsonException) { /* Ignore malformed lines */ }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[VisitedSystemsService] Error reading journal file {file}: {ex.Message}");
                        }
                    }
                    Debug.WriteLine($"[VisitedSystemsService] Full scan complete. Added {systemsAdded} new systems.");
                    SaveSystems();
                    SystemsUpdated?.Invoke(this, EventArgs.Empty);
                    JournalScanCompleted?.Invoke(this, new JournalScanCompletedEventArgs(filesScanned, systemsAdded));
                }
                catch (Exception ex)
                {
                    string errorMessage = $"A critical error occurred during the full journal scan: {ex.Message}";
                    Debug.WriteLine($"[VisitedSystemsService] {errorMessage}");
                    JournalScanCompleted?.Invoke(this, new JournalScanCompletedEventArgs(errorMessage));
                }
            });
        }

        private void LoadSystems()
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                var json = File.ReadAllText(_filePath);
                var systems = JsonSerializer.Deserialize<List<StarSystem>>(json);
                if (systems == null) return;

                _systems.Clear();
                foreach (var system in systems)
                {
                    _systems[system.Name] = system;
                }
                Debug.WriteLine($"[VisitedSystemsService] Loaded {_systems.Count} systems from file.");
                SystemsUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VisitedSystemsService] Failed to load systems: {ex.Message}");
            }
        }

        private void SaveSystems()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_systems.Values.ToList(), options);
                File.WriteAllText(_filePath, json);
                Debug.WriteLine($"[VisitedSystemsService] Saved {_systems.Count} systems to file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VisitedSystemsService] Failed to save systems: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}