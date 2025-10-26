using EliteDataRelay.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteDataRelay.Services
{
    public class SessionTrackingService : IDisposable
    {
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;

        private long _initialBalance;
        private CargoSnapshot? _initialCargoSnapshot;
        private CargoSnapshot? _previousCargoSnapshot;
        private DateTime? _sessionStartTime;
        private DateTime? _miningStartTime;
        private readonly ConcurrentDictionary<string, int> _refinedCommodities = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> _collectedCommodities = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MiningSessionRecord> _sessionHistory = new();
        private readonly object _historyLock = new();

        public long TotalCargoCollected { get; private set; }
        public long CreditsEarned { get; private set; }
        public bool IsMiningSessionActive { get; private set; }
        public bool IsMainSessionActive => _sessionStartTime.HasValue;
        public long MiningProfit { get; private set; } // Placeholder for now
        public int LimpetsUsed { get; private set; }
        public IReadOnlyDictionary<string, int> CollectedCommodities => _collectedCommodities;
        public TimeSpan MiningDuration => _miningStartTime.HasValue ? DateTime.UtcNow - _miningStartTime.Value : TimeSpan.Zero;
        public IReadOnlyDictionary<string, int> RefinedCommodities => _refinedCommodities;
        public int TotalRefinedCount => _refinedCommodities.Values.Sum();
        public TimeSpan SessionDuration => _sessionStartTime.HasValue ? DateTime.UtcNow - _sessionStartTime.Value : TimeSpan.Zero;
        public int CargoCapacity { get; private set; }
        public int CurrentCargoCount { get; private set; }
        public double CargoFillPercent => CargoCapacity <= 0 ? 0 : (double)CurrentCargoCount / CargoCapacity * 100d;
        public bool IsCargoHoldFull => CargoCapacity > 0 && CurrentCargoCount >= CargoCapacity;
        public MiningSessionPreferences Preferences { get; } = new();
        public IReadOnlyList<MiningSessionRecord> SessionHistory
        {
            get
            {
                lock (_historyLock)
                {
                    return _sessionHistory.Select(record => record.Clone()).ToList();
                }
            }
        }

        // Compatibility property for older controls if needed.
        public bool IsMining => IsMiningSessionActive;

        public event EventHandler? SessionUpdated;
        public event EventHandler? SessionHistoryUpdated;
        public event EventHandler<MiningSessionRecord>? SessionCompleted;
        public event EventHandler? PreferencesChanged;
        public event EventHandler<MiningNotificationEventArgs>? MiningNotificationRaised;

        public SessionTrackingService(ICargoProcessorService cargoProcessorService, IJournalWatcherService journalWatcherService)
        {
            _cargoProcessorService = cargoProcessorService;
            _journalWatcherService = journalWatcherService;

            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.LaunchDrone += OnLaunchDrone;
            _journalWatcherService.MiningRefined += OnMiningRefined;
            _journalWatcherService.CargoCapacityChanged += OnCargoCapacityChanged;
        }

        public void StartSession(long initialBalance, CargoSnapshot? initialCargoSnapshot)
        {
            _initialBalance = initialBalance;
            _initialCargoSnapshot = initialCargoSnapshot;
            _previousCargoSnapshot = initialCargoSnapshot; // Set the initial "previous" state

            _sessionStartTime = DateTime.UtcNow;

            // Reset counters
            TotalCargoCollected = 0;
            CreditsEarned = 0;
            LimpetsUsed = 0;
            MiningProfit = 0;
            _refinedCommodities.Clear();
            _collectedCommodities.Clear();
            CurrentCargoCount = initialCargoSnapshot?.Count ?? 0;

            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopSession()
        {
            if (_sessionStartTime.HasValue)
            {
                var record = CreateCurrentSessionRecord(DateTime.UtcNow);
                if (record != null)
                {
                    lock (_historyLock)
                    {
                        _sessionHistory.Add(record);
                    }

                    // Raise the history updated event first to ensure the UI grid is populated
                    // before the live stats are reset by the StopMiningSession call.
                    SessionHistoryUpdated?.Invoke(this, EventArgs.Empty);
                    SessionCompleted?.Invoke(this, record.Clone());
                }
            }

            _initialCargoSnapshot = null;
            _previousCargoSnapshot = null;
            _sessionStartTime = null;
            StopMiningSession(); // This will reset live counters and raise SessionUpdated
        }

        public void UpdateBalance(long newBalance)
        {
            if (_initialBalance > 0)
            {
                CreditsEarned = newBalance - _initialBalance;
                SessionUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the CargoProcessed event, which fires when Cargo.json changes.
        /// This is the most reliable way to track collected cargo.
        /// </summary>
        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            if (_previousCargoSnapshot == null) return;

            var currentSnapshot = e.Snapshot;
            var previousSnapshot = _previousCargoSnapshot;

            var previousCargoDict = previousSnapshot.Items.ToDictionary(c => c.Name, c => c.Count);

            foreach (var currentItem in currentSnapshot.Items)
            {
                previousCargoDict.TryGetValue(currentItem.Name, out var previousCount);

                var diff = currentItem.Count - previousCount;

                if (diff > 0)
                {
                    if (string.Equals(currentItem.Name, "drones", StringComparison.OrdinalIgnoreCase)) continue;

                    TotalCargoCollected += diff;
                    _collectedCommodities.AddOrUpdate(currentItem.Name, diff, (key, existingCount) => existingCount + diff);
                }
            }

            CurrentCargoCount = currentSnapshot.Count;

            _previousCargoSnapshot = currentSnapshot;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StartMiningSession()
        {
            if (IsMiningSessionActive) return;
            IsMiningSessionActive = true;
            _miningStartTime = DateTime.UtcNow;
            PublishNotification("Mining session started.", MiningNotificationType.Info, false);
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopMiningSession()
        {
            if (!IsMiningSessionActive) return;
            IsMiningSessionActive = false;
            _miningStartTime = null;
            PublishNotification("Mining session stopped.", MiningNotificationType.Info, false);
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnLaunchDrone(object? sender, LaunchDroneEventArgs e)
        {
            if (IsMiningSessionActive)
            {
                LimpetsUsed++;
                SessionUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnMiningRefined(object? sender, MiningRefinedEventArgs e)
        {
            _refinedCommodities.AddOrUpdate(e.CommodityType, 1, (key, count) => count + 1);
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            CargoCapacity = e.CargoCapacity;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public MiningSessionRecord? GetCurrentSessionRecord()
        {
            // This creates a snapshot of the current, ongoing session.
            return CreateCurrentSessionRecord(DateTime.UtcNow);
        }

        private MiningSessionRecord? CreateCurrentSessionRecord(DateTime sessionEnd)
        {
            if (!_sessionStartTime.HasValue) return null;

            return new MiningSessionRecord
            {
                SessionStart = _sessionStartTime.Value,
                SessionEnd = sessionEnd,
                SessionDurationSeconds = (sessionEnd - _sessionStartTime.Value).TotalSeconds,
                MiningDurationSeconds = MiningDuration.TotalSeconds,
                LimpetsUsed = LimpetsUsed,
                CreditsEarned = CreditsEarned,
                TotalCargoCollected = TotalCargoCollected,
                FinalCargoFillPercent = CargoFillPercent,
                CargoHoldFullAtEnd = IsCargoHoldFull,
                RefinedCommodities = new Dictionary<string, int>(_refinedCommodities, StringComparer.OrdinalIgnoreCase),
                CollectedCommodities = new Dictionary<string, int>(_collectedCommodities, StringComparer.OrdinalIgnoreCase)
            };
        }

        public string GenerateHtmlReport(IEnumerable<MiningSessionRecord>? sessions = null, string? title = null)
        {
            // If a specific list of sessions is provided, use it directly.
            // Otherwise, use the full session history from the service.
            var data = (sessions != null)
                ? sessions.Select(r => r.Clone()).ToList()
                : this.SessionHistory.Select(r => r.Clone()).ToList();

            if (data.Count == 0)
            {
                return "<html><body><h1>No mining sessions recorded.</h1></body></html>";
            }

            title ??= "Elite Data Relay – Mining Session Report";
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(title)}</title>");
            sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',Tahoma,sans-serif;background:#040404;color:#eee;margin:0;padding:24px;}");
            sb.AppendLine("h1{color:#ff8800;margin-bottom:8px;} h2{color:#f0f0f0;} table{border-collapse:collapse;width:100%;margin-bottom:24px;} th,td{border:1px solid #222;padding:8px;text-align:left;} th{background:#111;color:#ff8800;} tr:nth-child(even){background:#0d0d0d;} .card{background:#111;border:1px solid #222;border-radius:6px;padding:16px;margin-bottom:24px;}");
            sb.AppendLine("canvas{max-width:100%;}");
            sb.AppendLine(".metrics{display:flex;gap:16px;flex-wrap:wrap;} .metric{flex:1 1 200px;background:#121212;border-radius:6px;padding:12px;border:1px solid #1f1f1f;}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>{System.Net.WebUtility.HtmlEncode(title)}</h1>");
            sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm} (Local Time)</p>");

            sb.AppendLine("<div class=\"card\"><h2>Session Overview</h2><div class=\"metrics\">");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Sessions</strong><br/>{data.Count}</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Credits</strong><br/>{data.Sum(r => r.CreditsEarned):N0} cr</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Refined</strong><br/>{data.Sum(r => r.RefinedCommodities.Values.Sum()):N0} units</div>");
            sb.AppendLine($"<div class=\"metric\"><strong>Total Limpets Used</strong><br/>{data.Sum(r => r.LimpetsUsed):N0}</div>");
            sb.AppendLine("</div></div>");

            sb.AppendLine("<div class=\"card\"><canvas id=\"creditsChart\"></canvas></div>");

            sb.AppendLine("<table><thead><tr><th>Start</th><th>End</th><th>Duration</th><th>Mining Time</th><th>Credits</th><th>Cargo</th><th>Limpets</th><th>Final Fill %</th></tr></thead><tbody>");
            foreach (var record in data)
            {
                sb.AppendLine($"<tr><td>{record.SessionStart.ToLocalTime():yyyy-MM-dd HH:mm}</td><td>{record.SessionEnd.ToLocalTime():yyyy-MM-dd HH:mm}</td><td>{record.SessionDuration}</td><td>{record.MiningDuration}</td><td>{record.CreditsEarned:N0}</td><td>{record.TotalCargoCollected:N0}</td><td>{record.LimpetsUsed}</td><td>{record.FinalCargoFillPercent:F1}%</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            var refinedTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in data)
            {
                foreach (var kvp in record.RefinedCommodities)
                {
                    if (refinedTotals.ContainsKey(kvp.Key)) refinedTotals[kvp.Key] += kvp.Value;
                    else refinedTotals[kvp.Key] = kvp.Value;
                }
            }

            if (refinedTotals.Count > 0)
            {
                sb.AppendLine("<div class=\"card\"><h2>Refined Commodities</h2><table><thead><tr><th>Commodity</th><th>Total Refined</th></tr></thead><tbody>");
                foreach (var kvp in refinedTotals.OrderByDescending(k => k.Value))
                {
                    sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(kvp.Key)}</td><td>{kvp.Value:N0}</td></tr>");
                }
                sb.AppendLine("</tbody></table></div>");
            }

            var labels = string.Join(',', data.Select(r => $"'{r.SessionStart.ToLocalTime():MM-dd HH:mm}'"));
            var credits = string.Join(',', data.Select(r => r.CreditsEarned));
            var cargo = string.Join(',', data.Select(r => r.TotalCargoCollected));
            sb.AppendLine("<script>");
            sb.AppendLine("const ctx=document.getElementById('creditsChart').getContext('2d');");
            sb.AppendLine("new Chart(ctx,{type:'bar',data:{labels:[" + labels + "],datasets:[{label:'Credits Earned',data:[" + credits + "],backgroundColor:'rgba(255,136,0,0.6)',borderColor:'rgba(255,136,0,1)',borderWidth:1},{label:'Cargo Collected',data:[" + cargo + "],backgroundColor:'rgba(0,180,255,0.5)',borderColor:'rgba(0,180,255,1)',borderWidth:1}]},options:{responsive:true,plugins:{tooltip:{callbacks:{label:function(context){return context.dataset.label+': '+context.parsed.y.toLocaleString();}}}},scales:{y:{ticks:{color:'#ccc'},grid:{color:'#222'}},x:{ticks:{color:'#ccc'},grid:{color:'#222'}}}}});");
            sb.AppendLine("</script>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        public void PublishCustomNotification(string message, MiningNotificationType type = MiningNotificationType.Info, bool persistent = false)
            => PublishNotification(message, type, persistent);

        private void PublishNotification(string message, MiningNotificationType type, bool persistent)
        {
            if (!Preferences.AnnouncementsEnabled && type != MiningNotificationType.CargoFull) return;

            MiningNotificationRaised?.Invoke(this, new MiningNotificationEventArgs(type, message, DateTime.UtcNow, persistent));
        }

        public BackupSnapshot CreateSnapshot(IEnumerable<string>? reportPaths = null, IReadOnlyDictionary<string, HotspotLocation>? hotspotBookmarks = null)
        {
            var snapshot = new BackupSnapshot
            {
                CreatedOn = DateTime.UtcNow,
                Preferences = Preferences.Clone(),
                Reports = reportPaths?.ToList() ?? new List<string>(),
                HotspotBookmarks = hotspotBookmarks != null
                    ? new Dictionary<string, HotspotLocation>(hotspotBookmarks, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, HotspotLocation>(StringComparer.OrdinalIgnoreCase)
            };

            lock (_historyLock)
            {
                snapshot.SessionHistory = _sessionHistory.Select(record => record.Clone()).ToList();
            }

            return snapshot;
        }

        public void RestoreFromSnapshot(BackupSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            Preferences.ApplyFrom(snapshot.Preferences ?? new MiningSessionPreferences());
            lock (_historyLock)
            {
                _sessionHistory.Clear();
                if (snapshot.SessionHistory != null)
                {
                    foreach (var record in snapshot.SessionHistory)
                    {
                        _sessionHistory.Add(record.Clone());
                    }
                }
            }

            PreferencesChanged?.Invoke(this, EventArgs.Empty);
            SessionHistoryUpdated?.Invoke(this, EventArgs.Empty);
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyPreferencesChanged() => PreferencesChanged?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            _cargoProcessorService.CargoProcessed -= OnCargoProcessed;
            _journalWatcherService.LaunchDrone -= OnLaunchDrone;
            _journalWatcherService.MiningRefined -= OnMiningRefined;
            _journalWatcherService.CargoCapacityChanged -= OnCargoCapacityChanged;
            GC.SuppressFinalize(this);
        }
    }
}
