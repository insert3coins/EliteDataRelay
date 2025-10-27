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
            //Trace.WriteLine("[CargoProcessorService] State has been reset.");
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

                    // Read all bytes once; compute hash on bytes and deserialize directly from bytes to reduce allocations.
                    byte[] fileBytes = File.ReadAllBytes(AppConfiguration.CargoPath);

                    // An empty file is a common state. Treat it like a file lock and retry with minimal delay.
                    if (fileBytes.Length == 0)
                    {
                        Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                        continue;
                    }

                    // Fingerprint guard - skip duplicate file content
                    string hash = ComputeHash(fileBytes);
                    if (!force && hash == _lastInventoryHash) return true;

                    // Deserialize JSON directly from bytes for better performance.
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(fileBytes, options) ?? new CargoSnapshot();
                    
                    _lastInventoryHash = hash;

                    // Notify subscribers of successful processing
                    CargoProcessed?.Invoke(this, new CargoProcessedEventArgs(snapshot, hash));
                    
                    Trace.WriteLine($"[CargoProcessorService] Successfully processed cargo snapshot with hash: {hash[..8]}...");
                    return true; // Success
                }
                catch (IOException) when (attempt < AppConfiguration.FileReadMaxAttempts)
                {
                    // File still locked – wait before retrying
                    //Trace.WriteLine($"[CargoProcessorService] File locked, retry attempt {attempt}/{AppConfiguration.FileReadMaxAttempts}");
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
        /// Compute SHA256 hash of the raw file content for duplicate detection.
        /// </summary>
        /// <param name="content">The raw string content of the file to hash.</param>
        /// <returns>Base64-encoded SHA256 hash</returns>
        private string ComputeHash(byte[] bytes)
        {
            // Hash raw bytes (faster and avoids string allocations)
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }

        /// <summary>
        /// Async version of ProcessCargoFile to avoid blocking threads and improve responsiveness.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ProcessCargoFileAsync(bool force = false)
        {
            for (int attempt = 1; attempt <= AppConfiguration.FileReadMaxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(AppConfiguration.CargoPath))
                    {
                        if (attempt == AppConfiguration.FileReadMaxAttempts)
                            Trace.WriteLine($"[CargoProcessorService] Cargo.json not found after {attempt} attempts.");
                        return false;
                    }

                    byte[] fileBytes = await File.ReadAllBytesAsync(AppConfiguration.CargoPath).ConfigureAwait(false);

                    if (fileBytes.Length == 0)
                    {
                        await System.Threading.Tasks.Task.Delay(AppConfiguration.FileReadRetryDelayMs).ConfigureAwait(false);
                        continue;
                    }

                    string hash = ComputeHash(fileBytes);
                    if (!force && hash == _lastInventoryHash) return true;

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(fileBytes, options) ?? new CargoSnapshot();

                    _lastInventoryHash = hash;
                    CargoProcessed?.Invoke(this, new CargoProcessedEventArgs(snapshot, hash));
                    Trace.WriteLine($"[CargoProcessorService] (async) processed cargo snapshot {hash[..8]}...");
                    return true;
                }
                catch (IOException) when (attempt < AppConfiguration.FileReadMaxAttempts)
                {
                    //Trace.WriteLine($"[CargoProcessorService] (async) File locked, retry {attempt}/{AppConfiguration.FileReadMaxAttempts}");
                    await System.Threading.Tasks.Task.Delay(AppConfiguration.FileReadRetryDelayMs).ConfigureAwait(false);
                }
                catch (JsonException jsonEx)
                {
                    Trace.WriteLine($"[CargoProcessorService] (async) JSON parsing error: {jsonEx.Message}");
                    await System.Threading.Tasks.Task.Delay(AppConfiguration.FileReadRetryDelayMs).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CargoProcessorService] (async) Unexpected error: {ex}");
                    break;
                }
            }
            return false;
        }
    }
}
