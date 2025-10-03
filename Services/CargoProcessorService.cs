using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service for processing cargo data from the Elite Dangerous cargo file
    /// </summary>
    public class CargoProcessorService : ICargoProcessorService
    {
        private string? _lastInventoryHash;

        /// <summary>
        /// Event raised when new cargo data has been successfully processed
        /// </summary>
        public event EventHandler<CargoProcessedEventArgs>? CargoProcessed;

        /// <summary>
        /// Resets the internal state of the service, clearing the last known hash.
        /// This forces the next file processing to treat the data as new.
        /// </summary>
        public void Reset()
        {
            _lastInventoryHash = null;
            Trace.WriteLine("[CargoProcessorService] State has been reset.");
        }

        /// <summary>
        /// Process the cargo file and extract cargo snapshot data
        /// </summary>
        public bool ProcessCargoFile(bool force = false)
        {
            for (int attempt = 1; attempt <= AppConfiguration.FileReadMaxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(AppConfiguration.CargoPath))
                    {
                        // On the last attempt, if the file still doesn't exist, log it.
                        if (attempt == AppConfiguration.FileReadMaxAttempts) Trace.WriteLine($"[CargoProcessorService] Cargo.json not found after {attempt} attempts.");
                        return false;
                    }

                    // Open file with shared read-write to handle file locking
                    using var stream = new FileStream(AppConfiguration.CargoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    // An empty file is a common state when the game is about to write to it.
                    // Treat it like a file lock and retry after a short delay. This is more
                    // efficient than letting the JSON parser throw an exception.
                    if (stream.Length == 0)
                    {
                        Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                        continue;
                    }

                    // Deserialize JSON directly from the stream for better performance.
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(stream, options) ?? new CargoSnapshot(new(), 0);

                    // Fingerprint guard – skip duplicate snapshots
                    string hash = ComputeHash(snapshot);
                    if (!force && hash == _lastInventoryHash) return true;
                    
                    _lastInventoryHash = hash;

                    // Notify subscribers of successful processing
                    CargoProcessed?.Invoke(this, new CargoProcessedEventArgs(snapshot, hash));
                    
                    Trace.WriteLine($"[CargoProcessorService] Successfully processed cargo snapshot with hash: {hash[..8]}...");
                    return true; // Success
                }
                catch (IOException) when (attempt < AppConfiguration.FileReadMaxAttempts)
                {
                    // File still locked – wait before retrying
                    Trace.WriteLine($"[CargoProcessorService] File locked, retry attempt {attempt}/{AppConfiguration.FileReadMaxAttempts}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (JsonException jsonEx)
                {
                    // Malformed JSON – ignore for now and try again later
                    Trace.WriteLine($"[CargoProcessorService] JSON parsing error: {jsonEx.Message}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CargoProcessorService] Unexpected error: {ex}");
                    break; // Unexpected error – stop retrying
                }
            }
            return false;
        }

        /// <summary>
        /// Compute SHA256 hash of cargo snapshot for duplicate detection
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to hash</param>
        /// <returns>Base64-encoded SHA256 hash</returns>
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
            byte[] bytes = Encoding.UTF8.GetBytes(json); // This line was missing
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}