using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
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
        private readonly object _systemsLock = new object();
        private bool _isStarted;

        public event EventHandler? SystemsUpdated;
        public event EventHandler<JournalScanProgressEventArgs>? JournalScanProgressed;
        public event EventHandler<JournalScanCompletedEventArgs>? JournalScanCompleted;
        public IReadOnlyList<StarSystem> VisitedSystems
        {
            get
            {
                lock (_systemsLock)
                {
                    return _systems.Values.ToList();
                }
            }
        }

        public VisitedSystemsService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
            _filePath = Path.Combine(AppConfiguration.AppDataPath, "systemsvisited.db");
            InitializeDatabase();
        }

        public void Start()
        {
            if (_isStarted) return;
            LoadSystemsFromDb();
            SystemsUpdated?.Invoke(this, EventArgs.Empty);
            _journalWatcher.LocationChanged += OnLocationChanged;
            _journalWatcher.ScanEvent += OnScan;
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _journalWatcher.LocationChanged -= OnLocationChanged;
            _journalWatcher.ScanEvent -= OnScan;
            _isStarted = false;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (e.StarPos?.Length == 3)
            {
                AddOrUpdateSystem(e.StarSystem, e.StarPos, true);
            }
        }

        private void OnScan(object? sender, ScanEventArgs e)
        {
            AddOrUpdateBody(e.StarSystem, e.ScanData, e.StarPos);
        }

        private void AddOrUpdateSystem(string name, double[] coords, bool triggerUpdate)
        {
            if (string.IsNullOrWhiteSpace(name) || coords.Length != 3) return;

            bool systemAdded = false;
            lock (_systemsLock)
            {
                if (!_systems.ContainsKey(name))
                {
                    var newSystem = new StarSystem { Name = name, X = coords[0], Y = coords[1], Z = coords[2] };
                    _systems[name] = newSystem;
                    systemAdded = true;
                }
            }

            if (systemAdded)
            {
                using (var connection = new SqliteConnection($"Data Source={_filePath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT OR IGNORE INTO Systems (Name, X, Y, Z) VALUES ($name, $x, $y, $z)";
                    command.Parameters.AddWithValue("$name", name);
                    command.Parameters.AddWithValue("$x", coords[0]);
                    command.Parameters.AddWithValue("$y", coords[1]);
                    command.Parameters.AddWithValue("$z", coords[2]);
                    command.ExecuteNonQuery();
                }
                Debug.WriteLine($"[VisitedSystemsService] Added new system: {name}");
                if (triggerUpdate)
                {
                    SystemsUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool AddOrUpdateBody(string systemName, ScanEvent scanData, double[]? starPos = null)
        {
            lock (_systemsLock)
            {
                if (!_systems.TryGetValue(systemName, out var system))
                {
                    if (starPos?.Length == 3)
                    {
                        AddOrUpdateSystem(systemName, starPos, false);
                        // After AddOrUpdateSystem, the 'system' object is now in the dictionary.
                        // We must retrieve it to continue.
                        if (!_systems.TryGetValue(systemName, out system))
                        {
                            // This should not happen, but as a safeguard:
                            return false;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[VisitedSystemsService] Discarding Scan event for '{scanData.BodyName}' because system '{systemName}' is unknown and no coordinates were provided.");
                        return false;
                    }
                }

                if (system.Bodies.Any(b => b.BodyName.Equals(scanData.BodyName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return false; // Body already exists, no update needed.
                }

                var newBody = new SystemBody
                {
                    BodyName = scanData.BodyName,
                    StarType = scanData.StarType,
                    PlanetClass = scanData.PlanetClass ?? string.Empty,
                    WasDiscovered = scanData.WasDiscovered.GetValueOrDefault(),
                    WasMapped = scanData.WasMapped.GetValueOrDefault(),
                    Landable = scanData.Landable.GetValueOrDefault(),
                    TerraformState = scanData.TerraformState ?? string.Empty
                };
                system.Bodies.Add(newBody);

                using (var connection = new SqliteConnection($"Data Source={_filePath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT OR IGNORE INTO Bodies (SystemName, BodyName, StarType, PlanetClass, WasDiscovered, WasMapped, TerraformState, Landable) " +
                                          "VALUES ($systemName, $bodyName, $starType, $planetClass, $wasDiscovered, $wasMapped, $terraformState, $landable)";
                    command.Parameters.AddWithValue("$systemName", systemName);
                    command.Parameters.AddWithValue("$bodyName", newBody.BodyName);
                    command.Parameters.AddWithValue("$starType", newBody.StarType != null ? (object)newBody.StarType : DBNull.Value);
                    command.Parameters.AddWithValue("$planetClass", (object)newBody.PlanetClass ?? DBNull.Value);
                    command.Parameters.AddWithValue("$wasDiscovered", newBody.WasDiscovered);
                    command.Parameters.AddWithValue("$wasMapped", newBody.WasMapped);
                    command.Parameters.AddWithValue("$terraformState", (object)newBody.TerraformState ?? DBNull.Value);
                    command.Parameters.AddWithValue("$landable", (object)newBody.Landable ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
                return true;
            }
        }

        public async Task ScanAllJournalsAsync()
        {
            await Task.Run(() =>
            {
                int systemsAdded = 0;
                int bodiesAdded = 0;
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

                    // Use GetFiles to immediately retrieve the full list of files, making the process more robust.
                    var journalFiles = Directory.GetFiles(journalPath, "Journal.*.log")
                                                .OrderBy(f => f)
                                                .ToList();
                    int totalFiles = journalFiles.Count;
                    int filesProcessed = 0;

                    Debug.WriteLine($"[VisitedSystemsService] Found {totalFiles} journal files to scan.");

                    var tempSystems = new Dictionary<string, StarSystem>(StringComparer.InvariantCultureIgnoreCase);
                    var tempBodies = new List<(string SystemName, ScanEvent ScanData, double[]? StarPos)>();

                    foreach (var file in journalFiles)
                    {
                        filesProcessed++;
                        JournalScanProgressed?.Invoke(this, new JournalScanProgressEventArgs(filesProcessed, totalFiles));
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
                                                if (!string.IsNullOrEmpty(name) && !_systems.ContainsKey(name) && !tempSystems.ContainsKey(name))
                                                {
                                                    tempSystems[name] = new StarSystem { Name = name, X = coords[0], Y = coords[1], Z = coords[2] };
                                                }
                                            }
                                        }
                                        else if (eventType == "Scan")
                                        {
                                            if (jsonDoc.RootElement.TryGetProperty("StarSystem", out var scanSystemElement))
                                            {
                                                var systemName = scanSystemElement.GetString() ?? "";
                                                var scanEvent = JsonSerializer.Deserialize<ScanEvent>(line);

                                                double[]? starPos = null;
                                                if (jsonDoc.RootElement.TryGetProperty("StarPos", out var starPosElement) && starPosElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    starPos = starPosElement.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                                                }

                                                if (scanEvent != null && !string.IsNullOrEmpty(systemName))
                                                {
                                                    // Use the null-forgiving operator to assure the compiler that scanEvent is not null here.
                                                    tempBodies.Add((systemName, scanEvent!, starPos));
                                                    if (starPos != null && !_systems.ContainsKey(systemName) && !tempSystems.ContainsKey(systemName))
                                                    {
                                                        tempSystems[systemName] = new StarSystem { Name = systemName, X = starPos[0], Y = starPos[1], Z = starPos[2] };
                                                    }
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

                    // --- Bulk insert into database ---
                    if (tempSystems.Any() || tempBodies.Any())
                    {
                        using (var connection = new SqliteConnection($"Data Source={_filePath}"))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                if (tempSystems.Any())
                                {
                                    var systemCommand = connection.CreateCommand();
                                    systemCommand.CommandText = "INSERT OR IGNORE INTO Systems (Name, X, Y, Z) VALUES ($name, $x, $y, $z)";
                                    var nameParam = systemCommand.Parameters.Add("$name", SqliteType.Text);
                                    var xParam = systemCommand.Parameters.Add("$x", SqliteType.Real);
                                    var yParam = systemCommand.Parameters.Add("$y", SqliteType.Real);
                                    var zParam = systemCommand.Parameters.Add("$z", SqliteType.Real);

                                    foreach (var system in tempSystems.Values)
                                    {
                                        nameParam.Value = system.Name;
                                        xParam.Value = system.X;
                                        yParam.Value = system.Y;
                                        zParam.Value = system.Z;
                                        systemsAdded += systemCommand.ExecuteNonQuery();
                                    }
                                }

                                var bodyCommand = connection.CreateCommand();
                                bodyCommand.CommandText = "INSERT OR IGNORE INTO Bodies (SystemName, BodyName, StarType, PlanetClass, WasDiscovered, WasMapped, TerraformState, Landable) " +
                                                          "VALUES ($systemName, $bodyName, $starType, $planetClass, $wasDiscovered, $wasMapped, $terraformState, $landable)";
                                bodyCommand.Parameters.Add("$systemName", SqliteType.Text);
                                bodyCommand.Parameters.Add("$bodyName", SqliteType.Text);
                                bodyCommand.Parameters.Add("$starType", SqliteType.Text);
                                bodyCommand.Parameters.Add("$planetClass", SqliteType.Text);
                                bodyCommand.Parameters.Add("$wasDiscovered", SqliteType.Integer);
                                bodyCommand.Parameters.Add("$wasMapped", SqliteType.Integer);
                                bodyCommand.Parameters.Add("$terraformState", SqliteType.Text);
                                bodyCommand.Parameters.Add("$landable", SqliteType.Integer);

                                foreach (var (systemName, scanData, _) in tempBodies)
                                {
                                    bodyCommand.Parameters["$systemName"].Value = systemName;
                                    bodyCommand.Parameters["$bodyName"].Value = scanData.BodyName;
                                    bodyCommand.Parameters["$starType"].Value = (object?)scanData.StarType ?? DBNull.Value;
                                    bodyCommand.Parameters["$planetClass"].Value = (object?)scanData.PlanetClass ?? DBNull.Value;
                                    bodyCommand.Parameters["$wasDiscovered"].Value = scanData.WasDiscovered ?? false;
                                    bodyCommand.Parameters["$wasMapped"].Value = scanData.WasMapped ?? false;
                                    bodyCommand.Parameters["$terraformState"].Value = (object?)scanData.TerraformState ?? DBNull.Value;
                                    bodyCommand.Parameters["$landable"].Value = scanData.Landable ?? false;
                                    bodiesAdded += bodyCommand.ExecuteNonQuery();
                                }
                                transaction.Commit();
                            }
                        }
                    }

                    Debug.WriteLine($"[VisitedSystemsService] Full scan complete. Inserted {systemsAdded} new systems and {bodiesAdded} new bodies into the database.");
                    LoadSystemsFromDb(); // Reload all data from the DB to update the in-memory cache
                    SystemsUpdated?.Invoke(this, EventArgs.Empty);
                    JournalScanCompleted?.Invoke(this, new JournalScanCompletedEventArgs(totalFiles, systemsAdded, bodiesAdded));
                }
                catch (Exception ex)
                {
                    string errorMessage = $"A critical error occurred during the full journal scan: {ex.Message}";
                    Debug.WriteLine($"[VisitedSystemsService] {errorMessage}");
                    JournalScanCompleted?.Invoke(this, new JournalScanCompletedEventArgs(errorMessage));
                }
            });
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={_filePath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    CREATE TABLE IF NOT EXISTS Systems (
                        Name TEXT PRIMARY KEY,
                        X REAL NOT NULL,
                        Y REAL NOT NULL,
                        Z REAL NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Bodies (
                        BodyName TEXT NOT NULL,
                        SystemName TEXT NOT NULL,
                        StarType TEXT,
                        PlanetClass TEXT,
                        WasDiscovered INTEGER NOT NULL,
                        WasMapped INTEGER NOT NULL,
                        TerraformState TEXT,
                        Landable INTEGER NOT NULL,
                        PRIMARY KEY (SystemName, BodyName),
                        FOREIGN KEY (SystemName) REFERENCES Systems(Name)
                    );
                    ";
                command.ExecuteNonQuery();
            }

            // One-time migration from old JSON file
            var jsonPath = Path.Combine(AppConfiguration.AppDataPath, "systemsvisited.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    Debug.WriteLine("[VisitedSystemsService] Found old systemsvisited.json. Migrating to database...");
                    var json = File.ReadAllText(jsonPath);
                    var systems = JsonSerializer.Deserialize<List<StarSystem>>(json);
                    if (systems != null)
                    {
                        using (var connection = new SqliteConnection($"Data Source={_filePath}"))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                // Bulk insert systems
                                var systemCommand = connection.CreateCommand();
                                systemCommand.CommandText = "INSERT OR IGNORE INTO Systems (Name, X, Y, Z) VALUES ($name, $x, $y, $z)";
                                var nameParam = systemCommand.Parameters.Add("$name", SqliteType.Text);
                                var xParam = systemCommand.Parameters.Add("$x", SqliteType.Real);
                                var yParam = systemCommand.Parameters.Add("$y", SqliteType.Real);
                                var zParam = systemCommand.Parameters.Add("$z", SqliteType.Real);

                                foreach (var system in systems)
                                {
                                    nameParam.Value = system.Name;
                                    xParam.Value = system.X;
                                    yParam.Value = system.Y;
                                    zParam.Value = system.Z;
                                    systemCommand.ExecuteNonQuery();
                                }

                                // Bulk insert bodies
                                var bodyCommand = connection.CreateCommand();
                                bodyCommand.CommandText = "INSERT OR IGNORE INTO Bodies (SystemName, BodyName, StarType, PlanetClass, WasDiscovered, WasMapped, TerraformState, Landable) " +
                                                          "VALUES ($systemName, $bodyName, $starType, $planetClass, $wasDiscovered, $wasMapped, $terraformState, $landable)";
                                bodyCommand.Parameters.Add("$systemName", SqliteType.Text);
                                bodyCommand.Parameters.Add("$bodyName", SqliteType.Text);
                                bodyCommand.Parameters.Add("$starType", SqliteType.Text);
                                bodyCommand.Parameters.Add("$planetClass", SqliteType.Text);
                                bodyCommand.Parameters.Add("$wasDiscovered", SqliteType.Integer);
                                bodyCommand.Parameters.Add("$wasMapped", SqliteType.Integer);
                                bodyCommand.Parameters.Add("$terraformState", SqliteType.Text);
                                bodyCommand.Parameters.Add("$landable", SqliteType.Integer);

                                foreach (var system in systems)
                                {
                                    bodyCommand.Parameters["$systemName"].Value = system.Name;
                                    foreach (var body in system.Bodies)
                                    {
                                        bodyCommand.Parameters["$bodyName"].Value = body.BodyName;
                                        bodyCommand.Parameters["$starType"].Value = (object?)body.StarType ?? DBNull.Value;
                                        bodyCommand.Parameters["$planetClass"].Value = (object?)body.PlanetClass ?? DBNull.Value;
                                        bodyCommand.Parameters["$wasDiscovered"].Value = body.WasDiscovered;
                                        bodyCommand.Parameters["$wasMapped"].Value = body.WasMapped;
                                        bodyCommand.Parameters["$terraformState"].Value = (object?)body.TerraformState ?? DBNull.Value;
                                        bodyCommand.Parameters["$landable"].Value = body.Landable;
                                        bodyCommand.ExecuteNonQuery();
                                    }
                                }
                                transaction.Commit();
                            }
                        }
                        // Rename the old file to prevent re-migration on next startup.
                        File.Move(jsonPath, jsonPath + ".migrated_to_db");
                        Debug.WriteLine("[VisitedSystemsService] Migration complete. Renamed old JSON file.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[VisitedSystemsService] Failed to migrate from JSON: {ex.Message}");
                }
            }
        }

        private void LoadSystemsFromDb()
        {
            using (var connection = new SqliteConnection($"Data Source={_filePath}"))
            {
                connection.Open();
                lock (_systemsLock)
                {
                    _systems.Clear();

                    // Use a single, more efficient JOIN query to load all systems and their associated bodies at once.
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT s.Name, s.X, s.Y, s.Z,
                               b.BodyName, b.StarType, b.PlanetClass, b.WasDiscovered, b.WasMapped, b.TerraformState, b.Landable
                        FROM Systems s
                        LEFT JOIN Bodies b ON s.Name = b.SystemName
                        ORDER BY s.Name, b.BodyName";

                    using (var reader = command.ExecuteReader())
                    {
                        StarSystem? currentSystem = null;
                        while (reader.Read())
                        {
                            var systemName = reader.GetString(0);

                            // If we're on a new system in the result set, create it and add it to our dictionary.
                            if (!_systems.TryGetValue(systemName, out currentSystem))
                            {
                                currentSystem = new StarSystem
                                {
                                    Name = systemName,
                                    X = reader.GetDouble(1),
                                    Y = reader.GetDouble(2),
                                    Z = reader.GetDouble(3)
                                };
                                _systems[systemName] = currentSystem;
                            }

                            // Check if there is body data in this row (due to the LEFT JOIN).
                            // BodyName is column 4 (0-indexed).
                            if (!reader.IsDBNull(4))
                            {
                                var body = new SystemBody
                                {
                                    BodyName = reader.GetString(4),
                                    StarType = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    PlanetClass = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    WasDiscovered = reader.GetBoolean(7),
                                    WasMapped = reader.GetBoolean(8),
                                    TerraformState = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    Landable = reader.GetBoolean(10)
                                };
                                // Ensure currentSystem is not null before adding a body.
                                // This is a safeguard, as the logic above should always provide a valid system.
                                currentSystem?.Bodies.Add(body);
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}