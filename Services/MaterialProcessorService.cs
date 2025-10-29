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
    /// Service for processing material data from the Elite Dangerous Materials.json file.
    /// </summary>
    public class MaterialProcessorService : IMaterialProcessorService
    {
        private string? _lastMaterialsHash;

        /// <inheritdoc />
        public event EventHandler<MaterialsProcessedEventArgs>? MaterialsProcessed;

        /// <summary>
        /// Resets the internal state of the service, clearing the last known hash.
        /// This forces the next file processing to treat the data as new.
        /// </summary>
        public void Reset()
        {
            _lastMaterialsHash = null;
            Trace.WriteLine("[MaterialProcessorService] State has been reset.");
        }

        /// <inheritdoc />
        public bool ProcessMaterialsFile(bool force = false)
        {
            for (int attempt = 1; attempt <= AppConfiguration.FileReadMaxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(AppConfiguration.MaterialsPath))
                    {
                        if (attempt == AppConfiguration.FileReadMaxAttempts) Trace.WriteLine($"[MaterialProcessorService] Materials.json not found after {attempt} attempts.");
                        return false;
                    }

                    using var stream = new FileStream(AppConfiguration.MaterialsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                    if (stream.Length == 0)
                    {
                        Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                        continue;
                    }

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var materials = JsonSerializer.Deserialize<MaterialsEvent>(stream, options) ?? new MaterialsEvent();

                    string hash = ComputeHash(materials);
                    if (!force && hash == _lastMaterialsHash) return true;

                    _lastMaterialsHash = hash;

                    MaterialsProcessed?.Invoke(this, new MaterialsProcessedEventArgs(materials, hash));

                    Trace.WriteLine($"[MaterialProcessorService] Successfully processed materials with hash: {hash[..8]}...");
                    return true;
                }
                catch (IOException) when (attempt < AppConfiguration.FileReadMaxAttempts)
                {
                    Trace.WriteLine($"[MaterialProcessorService] File locked, retry attempt {attempt}/{AppConfiguration.FileReadMaxAttempts}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (JsonException jsonEx)
                {
                    Trace.WriteLine($"[MaterialProcessorService] JSON parsing error: {jsonEx.Message}");
                    Thread.Sleep(AppConfiguration.FileReadRetryDelayMs);
                }
                catch (Exception ex)
                {
                    Logger.Info($"[MaterialProcessorService] Unexpected error: {ex}");
                    break;
                }
            }
            return false;
        }

        private string ComputeHash(MaterialsEvent materials)
        {
            string json = JsonSerializer.Serialize(materials, new JsonSerializerOptions { WriteIndented = false });

            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
