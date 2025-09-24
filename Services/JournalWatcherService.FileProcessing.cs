using EliteDataRelay.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public partial class JournalWatcherService
    {
        private void ProcessStatusFile()
        {
            var statusFilePath = Path.Combine(_journalDir, "Status.json");
            if (!File.Exists(statusFilePath))
            {
                return;
            }

            try
            {
                // Use a FileStream with ReadWrite share to avoid exceptions if the game is writing to the file.
                using var fs = new FileStream(statusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                string content = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                string hash = ComputeHash(content);
                if (hash == _lastStatusHash)
                {
                    return;
                }

                _lastStatusHash = hash;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var statusEvent = JsonSerializer.Deserialize<StatusFile>(content, options);

                if (statusEvent != null)
                {
                    Debug.WriteLine($"[JournalWatcherService] Found Status.json update. Fuel: {statusEvent.Fuel?.FuelMain}, Cargo: {statusEvent.Cargo}, Hull: {statusEvent.HullHealth:P1}");
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(statusEvent));

                    // Also handle balance changes to replace StatusWatcherService
                    if (statusEvent.Balance.HasValue && statusEvent.Balance.Value != _lastKnownBalance)
                    {
                        _lastKnownBalance = statusEvent.Balance.Value;
                        BalanceChanged?.Invoke(this, new BalanceChangedEventArgs(_lastKnownBalance));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JournalWatcherService] Error processing Status.json: {ex}");
            }
        }

        private string ComputeHash(string content)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            byte[] hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}