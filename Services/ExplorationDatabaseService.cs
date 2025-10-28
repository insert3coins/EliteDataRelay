using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service for persisting exploration data to SQLite database.
    /// </summary>
    public partial class ExplorationDatabaseService : IDisposable
    {
        private readonly string _databasePath;
        private SqliteConnection? _connection;

        public ExplorationDatabaseService()
        {
            // Store the database in the same folder as settings (uses AppConfiguration.AppDataPath)
            if (!Directory.Exists(AppConfiguration.AppDataPath))
            {
                Directory.CreateDirectory(AppConfiguration.AppDataPath);
            }

            _databasePath = Path.Combine(AppConfiguration.AppDataPath, "exploration.db");
            Debug.WriteLine($"[ExplorationDatabaseService] Database path: {_databasePath}");
        }

        /// <summary>
        /// Initializes the database and creates tables if they don't exist.
        /// </summary>
        public void Initialize()
        {
            _connection = new SqliteConnection($"Data Source={_databasePath}");
            _connection.Open();

            // Set PRAGMA for better performance
            // WAL mode is faster for concurrent reads/writes, and NORMAL synchronous is safe with background writes
            using (var pragmaCmd = _connection.CreateCommand())
            {
                pragmaCmd.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA cache_size = -32000;
                    PRAGMA temp_store = MEMORY;
                ";
                pragmaCmd.ExecuteNonQuery();
                Debug.WriteLine("[ExplorationDatabaseService] Set SQLite PRAGMA settings for optimized performance");
            }

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Systems (
                    SystemAddress INTEGER PRIMARY KEY,
                    SystemName TEXT NOT NULL,
                    TotalBodies INTEGER NOT NULL DEFAULT 0,
                    ScannedBodies INTEGER NOT NULL DEFAULT 0,
                    MappedBodies INTEGER NOT NULL DEFAULT 0,
                    FSSProgress REAL NOT NULL DEFAULT 0,
                    LastVisited TEXT NOT NULL,
                    FirstVisited TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Bodies (
                    BodyID INTEGER NOT NULL,
                    SystemAddress INTEGER NOT NULL,
                    BodyName TEXT NOT NULL,
                    BodyType TEXT NOT NULL,
                    DistanceFromArrival REAL,
                    Landable INTEGER,
                    WasDiscovered INTEGER NOT NULL,
                    WasMapped INTEGER NOT NULL,
                    IsMapped INTEGER NOT NULL DEFAULT 0,
                    TerraformState TEXT,
                    ProbesUsed INTEGER,
                    EfficiencyTarget INTEGER,
                    Signals TEXT,
                    BiologicalSignals TEXT,
                    PRIMARY KEY (BodyID, SystemAddress),
                    FOREIGN KEY (SystemAddress) REFERENCES Systems(SystemAddress) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_bodies_system ON Bodies(SystemAddress);
                CREATE INDEX IF NOT EXISTS idx_systems_last_visited ON Systems(LastVisited);
            ";

            cmd.ExecuteNonQuery();
            Debug.WriteLine($"[ExplorationDatabaseService] Database initialized at: {_databasePath}");

            // Start background writer for async/non-blocking database operations
            StartBackgroundWriter();
        }

        /// <summary>
        /// Saves or updates a system in the database.
        /// </summary>
        public void SaveSystem(SystemExplorationData system)
        {
            if (_connection == null)
            {
                Debug.WriteLine("[ExplorationDatabaseService] Cannot save - database not initialized");
                return;
            }

            if (system.SystemAddress == null)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Cannot save system '{system.SystemName}' - no SystemAddress");
                return;
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                // Check if system exists
                using (var checkCmd = _connection.CreateCommand())
                {
                    checkCmd.CommandText = "SELECT FirstVisited FROM Systems WHERE SystemAddress = @systemAddress";
                    checkCmd.Parameters.AddWithValue("@systemAddress", system.SystemAddress.Value);

                    var firstVisited = checkCmd.ExecuteScalar() as string;

                    // Insert or update system
                    using var cmd = _connection.CreateCommand();
                    if (firstVisited == null)
                    {
                        // New system
                        cmd.CommandText = @"
                            INSERT INTO Systems (SystemAddress, SystemName, TotalBodies, ScannedBodies, MappedBodies, FSSProgress, LastVisited, FirstVisited)
                            VALUES (@systemAddress, @systemName, @totalBodies, @scannedBodies, @mappedBodies, @fssProgress, @lastVisited, @firstVisited)
                        ";
                        // Preserve journal event time for initial visit
                        cmd.Parameters.AddWithValue("@firstVisited", system.LastVisited.ToString("o"));
                    }
                    else
                    {
                        // Update existing system
                        cmd.CommandText = @"
                            UPDATE Systems
                            SET SystemName = @systemName,
                                TotalBodies = @totalBodies,
                                ScannedBodies = @scannedBodies,
                                MappedBodies = @mappedBodies,
                                FSSProgress = @fssProgress,
                                LastVisited = @lastVisited
                            WHERE SystemAddress = @systemAddress
                        ";
                    }

                    cmd.Parameters.AddWithValue("@systemAddress", system.SystemAddress.Value);
                    cmd.Parameters.AddWithValue("@systemName", system.SystemName);
                    cmd.Parameters.AddWithValue("@totalBodies", system.TotalBodies);
                    cmd.Parameters.AddWithValue("@scannedBodies", system.ScannedBodies);
                    cmd.Parameters.AddWithValue("@mappedBodies", system.MappedBodies);
                    cmd.Parameters.AddWithValue("@fssProgress", system.FSSProgress);
                    cmd.Parameters.AddWithValue("@lastVisited", system.LastVisited.ToString("o"));

                    cmd.ExecuteNonQuery();
                }

                // Save bodies
                foreach (var body in system.Bodies)
                {
                    SaveBody(body, system.SystemAddress.Value);
                }

                transaction.Commit();

                // Force immediate write to disk
                using (var checkpointCmd = _connection.CreateCommand())
                {
                    checkpointCmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE);";
                    checkpointCmd.ExecuteNonQuery();
                }

                Debug.WriteLine($"[ExplorationDatabaseService] Saved system: {system.SystemName} ({system.Bodies.Count} bodies)");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ExplorationDatabaseService] Error saving system: {ex.Message}");
                Debug.WriteLine($"[ExplorationDatabaseService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Saves or updates a body in the database.
        /// </summary>
        private void SaveBody(ScannedBody body, long systemAddress)
        {
            if (_connection == null || body.BodyID == null)
                return;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Bodies
                (BodyID, SystemAddress, BodyName, BodyType, DistanceFromArrival, Landable,
                 WasDiscovered, WasMapped, IsMapped, TerraformState, ProbesUsed, EfficiencyTarget,
                 Signals, BiologicalSignals)
                VALUES
                (@bodyId, @systemAddress, @bodyName, @bodyType, @distance, @landable,
                 @wasDiscovered, @wasMapped, @isMapped, @terraformState, @probesUsed, @efficiencyTarget,
                 @signals, @bioSignals)
            ";

            cmd.Parameters.AddWithValue("@bodyId", body.BodyID.Value);
            cmd.Parameters.AddWithValue("@systemAddress", systemAddress);
            cmd.Parameters.AddWithValue("@bodyName", body.BodyName);
            cmd.Parameters.AddWithValue("@bodyType", body.BodyType);
            cmd.Parameters.AddWithValue("@distance", body.DistanceFromArrival.HasValue ? (object)body.DistanceFromArrival.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@landable", body.Landable.HasValue ? (object)(body.Landable.Value ? 1 : 0) : DBNull.Value);
            cmd.Parameters.AddWithValue("@wasDiscovered", body.WasDiscovered ? 1 : 0);
            cmd.Parameters.AddWithValue("@wasMapped", body.WasMapped ? 1 : 0);
            cmd.Parameters.AddWithValue("@isMapped", body.IsMapped ? 1 : 0);
            cmd.Parameters.AddWithValue("@terraformState", (object?)body.TerraformState ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@probesUsed", body.ProbesUsed.HasValue ? (object)body.ProbesUsed.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@efficiencyTarget", body.EfficiencyTarget.HasValue ? (object)body.EfficiencyTarget.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@signals", JsonSerializer.Serialize(body.Signals));
            cmd.Parameters.AddWithValue("@bioSignals", JsonSerializer.Serialize(body.BiologicalSignals));

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads a system from the database.
        /// </summary>
        public SystemExplorationData? LoadSystem(long systemAddress)
        {
            if (_connection == null)
                return null;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT SystemAddress, SystemName, TotalBodies, ScannedBodies, MappedBodies, FSSProgress, LastVisited
                FROM Systems
                WHERE SystemAddress = @systemAddress
            ";
            cmd.Parameters.AddWithValue("@systemAddress", systemAddress);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var system = new SystemExplorationData
            {
                SystemAddress = reader.GetInt64(0),
                SystemName = reader.GetString(1),
                TotalBodies = reader.GetInt32(2),
                ScannedBodies = reader.GetInt32(3),
                MappedBodies = reader.GetInt32(4),
                FSSProgress = reader.GetDouble(5),
                LastVisited = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind)
            };

            reader.Close();

            // Load bodies
            system.Bodies = LoadBodies(systemAddress);

            Debug.WriteLine($"[ExplorationDatabaseService] Loaded system from cache: {system.SystemName} ({system.Bodies.Count} bodies)");
            return system;
        }

        /// <summary>
        /// Loads all bodies for a system from the database.
        /// </summary>
        private List<ScannedBody> LoadBodies(long systemAddress)
        {
            var bodies = new List<ScannedBody>();

            if (_connection == null)
                return bodies;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT BodyID, BodyName, BodyType, DistanceFromArrival, Landable,
                       WasDiscovered, WasMapped, IsMapped, TerraformState, ProbesUsed,
                       EfficiencyTarget, Signals, BiologicalSignals
                FROM Bodies
                WHERE SystemAddress = @systemAddress
                ORDER BY DistanceFromArrival
            ";
            cmd.Parameters.AddWithValue("@systemAddress", systemAddress);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var body = new ScannedBody
                {
                    BodyID = reader.GetInt32(0),
                    BodyName = reader.GetString(1),
                    BodyType = reader.GetString(2),
                    DistanceFromArrival = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    Landable = reader.IsDBNull(4) ? null : reader.GetInt32(4) == 1,
                    WasDiscovered = reader.GetInt32(5) == 1,
                    WasMapped = reader.GetInt32(6) == 1,
                    IsMapped = reader.GetInt32(7) == 1,
                    TerraformState = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ProbesUsed = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    EfficiencyTarget = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    Signals = JsonSerializer.Deserialize<List<Signal>>(reader.GetString(11)) ?? new List<Signal>(),
                    BiologicalSignals = JsonSerializer.Deserialize<List<string>>(reader.GetString(12)) ?? new List<string>()
                };

                bodies.Add(body);
            }

            return bodies;
        }

        /// <summary>
        /// Gets all systems visited, ordered by last visited date.
        /// </summary>
        public List<SystemExplorationData> GetVisitedSystems(int limit = 100)
        {
            var systems = new List<SystemExplorationData>();

            if (_connection == null)
            {
                Debug.WriteLine("[ExplorationDatabaseService] GetVisitedSystems - connection is null!");
                return systems;
            }

            try
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT SystemAddress, SystemName, LastVisited, ScannedBodies, MappedBodies
                    FROM Systems
                    ORDER BY LastVisited DESC
                    LIMIT @limit
                ";
                cmd.Parameters.AddWithValue("@limit", limit);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    systems.Add(new SystemExplorationData
                    {
                        SystemAddress = reader.GetInt64(0),
                        SystemName = reader.GetString(1),
                        LastVisited = DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                        ScannedBodies = reader.GetInt32(3),
                        MappedBodies = reader.GetInt32(4)
                    });
                }

                Debug.WriteLine($"[ExplorationDatabaseService] GetVisitedSystems returned {systems.Count} systems");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Error getting visited systems: {ex.Message}");
            }

            return systems;
        }

        /// <summary>
        /// Gets total statistics across all cached systems.
        /// </summary>
        public (int TotalSystems, int TotalBodies, int TotalMapped) GetTotalStatistics()
        {
            if (_connection == null)
            {
                Debug.WriteLine("[ExplorationDatabaseService] GetTotalStatistics - connection is null!");
                return (0, 0, 0);
            }

            try
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        COUNT(*) as TotalSystems,
                        SUM(ScannedBodies) as TotalBodies,
                        SUM(MappedBodies) as TotalMapped
                    FROM Systems
                ";

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var stats = (
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
                    );
                    Debug.WriteLine($"[ExplorationDatabaseService] GetTotalStatistics: {stats.Item1} systems, {stats.Item2} bodies, {stats.Item3} mapped");
                    return stats;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Error getting statistics: {ex.Message}");
            }

            return (0, 0, 0);
        }

        /// <summary>
        /// Deletes systems older than the specified number of days.
        /// </summary>
        public int PruneOldSystems(int daysToKeep = 90)
        {
            if (_connection == null)
                return 0;

            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM Systems
                WHERE LastVisited < @cutoffDate
            ";
            cmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("o"));

            int deleted = cmd.ExecuteNonQuery();
            if (deleted > 0)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Pruned {deleted} systems older than {daysToKeep} days");
            }

            return deleted;
        }

        /// <summary>
        /// Flushes all pending writes to disk.
        /// </summary>
        public void Flush()
        {
            if (_connection == null) return;

            try
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                cmd.ExecuteNonQuery();
                Debug.WriteLine("[ExplorationDatabaseService] Flushed database to disk");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Error flushing database: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Stop background writer and flush pending writes
            StopBackgroundWriter();

            if (_connection != null)
            {
                try
                {
                    // Ensure all data is written to disk before closing
                    Flush();
                    _connection.Close();
                    Debug.WriteLine("[ExplorationDatabaseService] Database connection closed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExplorationDatabaseService] Error during disposal: {ex.Message}");
                }
                finally
                {
                    _connection?.Dispose();
                    _connection = null;
                }
            }
        }
    }
}
