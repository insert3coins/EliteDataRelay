using EliteDataRelay.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Async extension methods for ExplorationDatabaseService to improve performance.
    /// Uses background write queue to prevent UI blocking during rapid scan events.
    /// </summary>
    public partial class ExplorationDatabaseService
    {
        private readonly ConcurrentQueue<SystemExplorationData> _writeQueue = new ConcurrentQueue<SystemExplorationData>();
        private readonly SemaphoreSlim _writeSignal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _backgroundCts = new CancellationTokenSource();
        private Task? _backgroundWriterTask;
        private bool _enableBackgroundWrites = true;

        /// <summary>
        /// Starts the background writer thread for async database writes.
        /// Call this during Initialize() to enable non-blocking database operations.
        /// </summary>
        public void StartBackgroundWriter()
        {
            if (_backgroundWriterTask != null)
                return; // Already started

            _enableBackgroundWrites = true;
            _backgroundWriterTask = Task.Run(BackgroundWriterLoop, _backgroundCts.Token);
            Debug.WriteLine("[ExplorationDatabaseService] Background writer started");
        }

        /// <summary>
        /// Stops the background writer and flushes pending writes.
        /// </summary>
        public void StopBackgroundWriter()
        {
            if (_backgroundWriterTask == null)
                return;

            _enableBackgroundWrites = false;
            _writeSignal.Release(); // Wake up the writer to exit

            try
            {
                _backgroundWriterTask.Wait(5000); // Wait up to 5 seconds
            }
            catch (AggregateException)
            {
                // Ignore cancellation exceptions
            }

            // Flush any remaining items
            FlushWriteQueue();

            Debug.WriteLine("[ExplorationDatabaseService] Background writer stopped");
        }

        /// <summary>
        /// Queues a system for async background write instead of blocking immediately.
        /// This is much faster during rapid scanning events.
        /// </summary>
        public void SaveSystemAsync(SystemExplorationData system)
        {
            if (!_enableBackgroundWrites || _backgroundWriterTask == null)
            {
                // Fallback to synchronous write if background writer not active
                SaveSystem(system);
                return;
            }

            _writeQueue.Enqueue(system);
            _writeSignal.Release(); // Signal the background writer
        }

        /// <summary>
        /// Background loop that processes queued writes.
        /// Batches multiple writes into single transaction for better performance.
        /// </summary>
        private async Task BackgroundWriterLoop()
        {
            var batch = new List<SystemExplorationData>();
            const int MaxBatchSize = 10;
            const int MaxBatchWaitMs = 500;

            while (_enableBackgroundWrites)
            {
                try
                {
                    // Wait for signal or timeout
                    await _writeSignal.WaitAsync(MaxBatchWaitMs, _backgroundCts.Token);

                    // Collect items from queue
                    batch.Clear();
                    while (_writeQueue.TryDequeue(out var system) && batch.Count < MaxBatchSize)
                    {
                        batch.Add(system);
                    }

                    // Write batch if we have items
                    if (batch.Count > 0)
                    {
                        WriteBatch(batch);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExplorationDatabaseService] Background writer error: {ex.Message}");
                }
            }

            Debug.WriteLine("[ExplorationDatabaseService] Background writer loop exited");
        }

        /// <summary>
        /// Writes multiple systems in a single transaction for better performance.
        /// </summary>
        private void WriteBatch(List<SystemExplorationData> systems)
        {
            if (_connection == null)
                return;

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    foreach (var system in systems)
                    {
                        SaveSystemInternal(system, transaction);
                    }

                    transaction.Commit();
                }

                stopwatch.Stop();
                Debug.WriteLine($"[ExplorationDatabaseService] Batch write completed: {systems.Count} systems in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExplorationDatabaseService] Batch write failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Internal save method that uses an existing transaction.
        /// </summary>
        private void SaveSystemInternal(SystemExplorationData system, SqliteTransaction transaction)
        {
            if (_connection == null)
                return;

            if (!system.SystemAddress.HasValue)
            {
                Debug.WriteLine("[ExplorationDatabaseService] Cannot save system without SystemAddress");
                return;
            }

            // Upsert system record
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Systems (SystemAddress, SystemName, TotalBodies, ScannedBodies, MappedBodies,
                                        FSSProgress, LastVisited, FirstVisited)
                    VALUES (@systemAddress, @systemName, @totalBodies, @scannedBodies, @mappedBodies,
                           @fssProgress, @lastVisited, @firstVisited)
                    ON CONFLICT(SystemAddress) DO UPDATE SET
                        SystemName = @systemName,
                        TotalBodies = @totalBodies,
                        ScannedBodies = @scannedBodies,
                        MappedBodies = @mappedBodies,
                        FSSProgress = @fssProgress,
                        LastVisited = @lastVisited;
                ";

                cmd.Parameters.AddWithValue("@systemAddress", system.SystemAddress.Value);
                cmd.Parameters.AddWithValue("@systemName", system.SystemName);
                cmd.Parameters.AddWithValue("@totalBodies", system.TotalBodies);
                cmd.Parameters.AddWithValue("@scannedBodies", system.ScannedBodies);
                cmd.Parameters.AddWithValue("@mappedBodies", system.MappedBodies);
                cmd.Parameters.AddWithValue("@fssProgress", system.FSSProgress);
                cmd.Parameters.AddWithValue("@lastVisited", system.LastVisited.ToString("o"));
                cmd.Parameters.AddWithValue("@firstVisited", system.LastVisited.ToString("o"));

                cmd.ExecuteNonQuery();
            }

            // Save all bodies for this system
            foreach (var body in system.Bodies)
            {
                SaveBodyInternal(body, system.SystemAddress.Value, transaction);
            }
        }

        /// <summary>
        /// Internal body save method that uses an existing transaction.
        /// </summary>
        private void SaveBodyInternal(ScannedBody body, long systemAddress, SqliteTransaction transaction)
        {
            if (_connection == null)
                return;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Bodies (BodyID, SystemAddress, BodyName, BodyType, DistanceFromArrival,
                                       Landable, WasDiscovered, WasMapped, IsMapped, TerraformState,
                                       ProbesUsed, EfficiencyTarget, Signals, BiologicalSignals)
                    VALUES (@bodyId, @systemAddress, @bodyName, @bodyType, @distance,
                           @landable, @wasDiscovered, @wasMapped, @isMapped, @terraformState,
                           @probesUsed, @efficiencyTarget, @signals, @bioSignals)
                    ON CONFLICT(BodyID, SystemAddress) DO UPDATE SET
                        BodyName = @bodyName,
                        BodyType = @bodyType,
                        DistanceFromArrival = @distance,
                        Landable = @landable,
                        WasDiscovered = @wasDiscovered,
                        WasMapped = @wasMapped,
                        IsMapped = @isMapped,
                        TerraformState = @terraformState,
                        ProbesUsed = @probesUsed,
                        EfficiencyTarget = @efficiencyTarget,
                        Signals = @signals,
                        BiologicalSignals = @bioSignals;
                ";

                cmd.Parameters.AddWithValue("@bodyId", body.BodyID);
                cmd.Parameters.AddWithValue("@systemAddress", systemAddress);
                cmd.Parameters.AddWithValue("@bodyName", body.BodyName);
                cmd.Parameters.AddWithValue("@bodyType", body.BodyType);
                cmd.Parameters.AddWithValue("@distance", body.DistanceFromArrival ?? 0.0);
                cmd.Parameters.AddWithValue("@landable", (body.Landable ?? false) ? 1 : 0);
                cmd.Parameters.AddWithValue("@wasDiscovered", body.WasDiscovered ? 1 : 0);
                cmd.Parameters.AddWithValue("@wasMapped", body.WasMapped ? 1 : 0);
                cmd.Parameters.AddWithValue("@isMapped", body.IsMapped ? 1 : 0);
                cmd.Parameters.AddWithValue("@terraformState", body.TerraformState ?? "");
                cmd.Parameters.AddWithValue("@probesUsed", body.ProbesUsed ?? 0);
                cmd.Parameters.AddWithValue("@efficiencyTarget", body.EfficiencyTarget ?? 0);

                // Serialize signals to JSON (only if not empty to reduce overhead)
                string signalsJson = (body.Signals != null && body.Signals.Count > 0)
                    ? JsonSerializer.Serialize(body.Signals)
                    : "[]";
                string bioSignalsJson = (body.BiologicalSignals != null && body.BiologicalSignals.Count > 0)
                    ? JsonSerializer.Serialize(body.BiologicalSignals)
                    : "[]";

                cmd.Parameters.AddWithValue("@signals", signalsJson);
                cmd.Parameters.AddWithValue("@bioSignals", bioSignalsJson);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Flushes any pending writes in the queue synchronously.
        /// Call before shutdown or when you need to ensure all data is written.
        /// </summary>
        public void FlushWriteQueue()
        {
            var batch = new List<SystemExplorationData>();

            while (_writeQueue.TryDequeue(out var system))
            {
                batch.Add(system);
            }

            if (batch.Count > 0)
            {
                WriteBatch(batch);
                Debug.WriteLine($"[ExplorationDatabaseService] Flushed {batch.Count} pending writes");
            }
        }
    }
}
