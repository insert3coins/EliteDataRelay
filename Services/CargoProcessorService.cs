using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.Services
{
    public class CargoProcessorService : ICargoProcessorService
    {
        private string? _lastInventoryHash;

        public event EventHandler<CargoProcessedEventArgs>? CargoProcessed;

        public void ProcessCargoFile()
        {
            for (int attempt = 1; attempt <= AppConfiguration.FileReadMaxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(AppConfiguration.CargoPath)) return;

                    // Open file with shared read-write to handle file locking
                    using var stream = new FileStream(AppConfiguration.CargoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string json = reader.ReadToEnd();

                    // Guard against empty or partially written files
                    var trimmed = json.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                        continue;
                    }

                    // Quick sanity check – it must start with "{" or "["
                    if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                    {
                        Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                        continue;
                    }

                    // Deserialize JSON to CargoSnapshot
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var snapshot = JsonSerializer.Deserialize<CargoSnapshot>(json, options);
                    if (snapshot == null) return;

                    // Fingerprint guard – skip duplicate snapshots
                    string hash = ComputeHash(snapshot);
                    if (hash == _lastInventoryHash) return;
                    
                    _lastInventoryHash = hash;

                    // Notify subscribers of successful processing
                    CargoProcessed?.Invoke(this, new CargoProcessedEventArgs(snapshot, hash));
                    
                    Debug.WriteLine($"[CargoProcessorService] Successfully processed cargo snapshot with hash: {hash[..8]}...");
                    break; // Success
                }
                catch (IOException) when (attempt < AppConfiguration.FileReadMaxAttempts)
                {
                    // File still locked – wait before retrying
                    Debug.WriteLine($"[CargoProcessorService] File locked, retry attempt {attempt}/{AppConfiguration.FileReadMaxAttempts}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (JsonException jsonEx)
                {
                    // Malformed JSON – ignore for now and try again later
                    Debug.WriteLine($"[CargoProcessorService] JSON parsing error: {jsonEx.Message}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CargoProcessorService] Unexpected error: {ex}");
                    break; // Unexpected error – stop retrying
                }
            }
        }

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
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}