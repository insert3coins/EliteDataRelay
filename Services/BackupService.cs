using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public class BackupService
    {
        private readonly SessionTrackingService _sessionTrackingService;
        private readonly HotspotFinderService _hotspotFinderService;
        private readonly List<string> _knownReports = new();
        private readonly object _sync = new();

        public BackupService(SessionTrackingService sessionTrackingService, HotspotFinderService hotspotFinderService)
        {
            _sessionTrackingService = sessionTrackingService ?? throw new ArgumentNullException(nameof(sessionTrackingService));
            _hotspotFinderService = hotspotFinderService ?? throw new ArgumentNullException(nameof(hotspotFinderService));
        }

        public void RegisterReport(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            lock (_sync)
            {
                if (!_knownReports.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    _knownReports.Add(path);
                }
            }
        }

        public string CreateBackup(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Directory must be provided", nameof(directory));

            Directory.CreateDirectory(directory);
            var snapshot = BuildSnapshot();
            var filePath = Path.Combine(directory, $"edr-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            SaveSnapshot(filePath, snapshot);
            return filePath;
        }

        public void SaveSnapshot(string path, BackupSnapshot snapshot)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(snapshot, options);
            File.WriteAllText(path, json);
        }

        public void RestoreBackup(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Backup file not found", path);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var snapshot = JsonSerializer.Deserialize<BackupSnapshot>(File.ReadAllText(path), options) ?? throw new InvalidOperationException("Invalid backup file");

            _sessionTrackingService.RestoreFromSnapshot(snapshot);
            _hotspotFinderService.RestoreBookmarks(snapshot.HotspotBookmarks);

            lock (_sync)
            {
                _knownReports.Clear();
                if (snapshot.Reports != null)
                {
                    _knownReports.AddRange(snapshot.Reports);
                }
            }
        }

        public BackupSnapshot BuildSnapshot()
        {
            lock (_sync)
            {
                var existingReports = _knownReports.Where(File.Exists).ToList();
                return _sessionTrackingService.CreateSnapshot(existingReports, _hotspotFinderService.GetBookmarksSnapshot());
            }
        }
    }
}
