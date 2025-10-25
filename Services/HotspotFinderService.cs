using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EliteDataRelay.Services
{
    public class HotspotFinderService
    {
        private readonly List<HotspotLocation> _hotspots = new();
        private readonly Dictionary<string, HotspotLocation> _bookmarks = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new();
        private readonly string _dataDirectory;
        private bool _loaded;

        public HotspotFinderService(string? dataDirectory = null)
        {
            _dataDirectory = dataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        }

        public IReadOnlyDictionary<string, HotspotLocation> Bookmarks
        {
            get
            {
                lock (_sync)
                {
                    return _bookmarks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone(), StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public IReadOnlyList<HotspotLocation> Search(HotspotSearchCriteria criteria)
        {
            EnsureLoaded();

            IEnumerable<HotspotLocation> query = _hotspots;

            if (!string.IsNullOrWhiteSpace(criteria.Mineral))
            {
                query = query.Where(h => h.Mineral.Contains(criteria.Mineral!, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(criteria.RingType))
            {
                query = query.Where(h => h.RingType.Contains(criteria.RingType!, StringComparison.OrdinalIgnoreCase));
            }

            if (criteria.MaxDistance.HasValue && !double.IsNaN(criteria.MaxDistance.Value))
            {
                query = query.Where(h => !double.IsNaN(h.DistanceFromStar) && h.DistanceFromStar <= criteria.MaxDistance.Value);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SystemContains))
            {
                query = query.Where(h => h.StarSystem.Contains(criteria.SystemContains!, StringComparison.OrdinalIgnoreCase));
            }

            return query.Take(200).Select(h => h.Clone()).ToList();
        }

        public void AddBookmark(string key, HotspotLocation location)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty", nameof(key));
            if (location == null) throw new ArgumentNullException(nameof(location));

            lock (_sync)
            {
                _bookmarks[key] = location.Clone();
            }
        }

        public bool RemoveBookmark(string key)
        {
            lock (_sync)
            {
                return _bookmarks.Remove(key);
            }
        }

        public Dictionary<string, HotspotLocation> GetBookmarksSnapshot()
        {
            lock (_sync)
            {
                return _bookmarks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone(), StringComparer.OrdinalIgnoreCase);
            }
        }

        public void RestoreBookmarks(Dictionary<string, HotspotLocation>? bookmarks)
        {
            lock (_sync)
            {
                _bookmarks.Clear();
                if (bookmarks == null) return;
                foreach (var kvp in bookmarks)
                {
                    _bookmarks[kvp.Key] = kvp.Value.Clone();
                }
            }
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;

            lock (_sync)
            {
                if (_loaded) return;

                try
                {
                    LoadHotspotData();
                }
                catch
                {
                    // swallow – service consumers can still use bookmarks even if dataset fails
                }
                finally
                {
                    _loaded = true;
                }
            }
        }

        private void LoadHotspotData()
        {
            var primaryPath = Path.Combine(_dataDirectory, "hotspots.json");
            var fallbackPath = Path.Combine(_dataDirectory, "hotspots-sample.json");
            string? pathToUse = null;

            if (File.Exists(primaryPath)) pathToUse = primaryPath;
            else if (File.Exists(fallbackPath)) pathToUse = fallbackPath;

            if (pathToUse == null) return;

            using var stream = File.OpenRead(pathToUse);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var hotspots = JsonSerializer.Deserialize<List<HotspotLocation>>(stream, options) ?? new List<HotspotLocation>();

            _hotspots.Clear();
            _hotspots.AddRange(hotspots.Select(h => h.Clone()));
        }
    }
}
