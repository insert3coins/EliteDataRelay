using EliteDataRelay.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;

namespace EliteDataRelay.Services
{
    public class SystemInfoService : ISystemInfoService
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IJournalWatcherService _journalWatcher;
        private bool _isStarted;
        private SystemInfoData? _lastSystemInfo; // Stores the last successfully fetched system info
        private CancellationTokenSource? _fetchCancellationTokenSource; // Manages cancellation for the actual API fetch
        private readonly object _lock = new object();
        private CancellationTokenSource? _debounceCancellationTokenSource; // Manages cancellation for the debounce delay

        public event EventHandler<SystemInfoData>? SystemInfoUpdated;

        public SystemInfoService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
        }

        public void Start()
        {
            if (_isStarted) return;
            _journalWatcher.LocationChanged += OnLocationChanged;
            _journalWatcher.NextJumpSystemChanged += OnNextJumpSystemChanged;
            _isStarted = true;

            // On startup, proactively get the last known location from the journal watcher.
            // This populates the overlay immediately without waiting for a new jump event.
            var lastLocation = _journalWatcher.GetLastKnownLocation();
            if (lastLocation != null)
            {
                OnLocationChanged(this, lastLocation);
            }

        }

        public void Stop()
        {
            if (!_isStarted) return;
            // Cancel any pending operations when stopping the service.
            _debounceCancellationTokenSource?.Cancel();
            _fetchCancellationTokenSource?.Cancel();

            _journalWatcher.LocationChanged -= OnLocationChanged;
            _journalWatcher.NextJumpSystemChanged -= OnNextJumpSystemChanged;
            _isStarted = false;
        }

        public void RequestFetch(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName)) return;
            lock (_lock)
            {
                if (!_isStarted) return;
                _fetchCancellationTokenSource?.Cancel();
                _fetchCancellationTokenSource = new CancellationTokenSource();
                var fetchToken = _fetchCancellationTokenSource.Token;
                Task.Run(async () =>
                {
                    var systemInfo = await FetchSystemInfoAsync(systemName, fetchToken) ?? new SystemInfoData { SystemName = systemName };
                    _lastSystemInfo = systemInfo;
                    SystemInfoUpdated?.Invoke(this, systemInfo);
                });
            }
        }

        private void OnNextJumpSystemChanged(object? sender, NextJumpSystemChangedEventArgs e)
        {
            lock (_lock)
            {
                if (!_isStarted) return;

                // This is for the "Next Jump" overlay. We want this to be immediate.
                _fetchCancellationTokenSource?.Cancel();
                _fetchCancellationTokenSource = new CancellationTokenSource();
                var fetchToken = _fetchCancellationTokenSource.Token;

                Task.Run(async () =>
                {
                    var systemInfo = await FetchSystemInfoAsync(e.NextSystemName, fetchToken) ?? new SystemInfoData { SystemName = e.NextSystemName };
                    _lastSystemInfo = systemInfo;
                    SystemInfoUpdated?.Invoke(this, systemInfo);
                });
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            lock (_lock)
            {
                if (!_isStarted) return; // If we are stopping/stopped, do not process new events.

                if (e.IsNewSystem || _lastSystemInfo == null)
                {
                    // Always cancel any previous debounce operation
                    _debounceCancellationTokenSource?.Cancel(); // Cancel previous debounce
                    _debounceCancellationTokenSource = new CancellationTokenSource();
                    var currentDebounceToken = _debounceCancellationTokenSource.Token;

                    // Use Task.Run to offload the async work, allowing the lock to be released quickly.
                    Task.Run(async () =>
                    {
                        try
                        {
                    // Fetch immediately on system change to update the exploration overlay without delay.

                            // If we reach here, either it's the first fetch or the debounce completed.
                            // Cancel any *previous* actual fetch operation.
                            _fetchCancellationTokenSource?.Cancel(); // Cancel previous fetch
                            _fetchCancellationTokenSource = new CancellationTokenSource();
                            var currentFetchToken = _fetchCancellationTokenSource.Token;

                            var systemInfo = await FetchSystemInfoAsync(e.StarSystem, currentFetchToken) ?? new SystemInfoData { SystemName = e.StarSystem };
                            _lastSystemInfo = systemInfo;
                            SystemInfoUpdated?.Invoke(this, systemInfo);
                        }
                        catch (OperationCanceledException)
                        {
                            // This is expected when a new jump happens quickly.
                            System.Diagnostics.Debug.WriteLine($"[SystemInfoService] Debounced or cancelled fetch for '{e.StarSystem}'.");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SystemInfoService] Error fetching system info: {ex.Message}");
                        }
                    });
                }
            }
        }

        public SystemInfoData? GetLastSystemInfo()
        {
            return _lastSystemInfo;
        }

        private async Task<SystemInfoData?> FetchSystemInfoAsync(string systemName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return null;
            }

            // Check if cancellation has been requested before starting the operation.
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // EDSM API endpoint for system information
                var url = $"https://www.edsm.net/api-v1/system?systemName={Uri.EscapeDataString(systemName)}&showInformation=1";

                // Pass the cancellation token to the HTTP client.
                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                // EDSM returns an empty array `[]` if the system is not found, or an object `{...}` if it is.
                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var jsonDoc = await JsonDocument.ParseAsync(contentStream);

                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // System not found in EDSM, return default data
                    System.Diagnostics.Debug.WriteLine($"[SystemInfoService] System '{systemName}' not found on EDSM.");
                    return new SystemInfoData { SystemName = systemName };
                }

                var edsmSystem = jsonDoc.RootElement.Deserialize<EdsmSystem>(_jsonOptions);
                var info = edsmSystem?.Information;

                var result = new SystemInfoData
                {
                    SystemName = edsmSystem?.Name ?? systemName,
                    Allegiance = info?.Allegiance ?? "N/A",
                    Government = info?.Government ?? "N/A",
                    Economy = info?.Economy ?? "N/A",
                    Security = CleanEdsmSecurity(info?.Security) ?? "N/A",
                    Population = info?.Population ?? 0,
                    ControllingFaction = info?.Faction ?? "N/A",
                    FactionState = info?.FactionState ?? "N/A"
                };

                // Also fetch traffic stats (arrivals/departures) from EDSM
                try
                {
                    var traffic = await FetchTrafficAsync(systemName, cancellationToken);
                    if (traffic.HasValue)
                    {
                        var t = traffic.Value;
                        result.TrafficDay = t.Day;
                        result.TrafficWeek = t.Week;
                        result.TrafficTotal = t.Total;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemInfoService] Traffic fetch failed for '{systemName}': {ex.Message}");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemInfoService] EDSM API request failed for '{systemName}': {ex.Message}");
                return new SystemInfoData { SystemName = systemName, Allegiance = "Network Error" };
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemInfoService] Failed to parse EDSM response for '{systemName}': {ex.Message}");
                return new SystemInfoData { SystemName = systemName, Allegiance = "API Error" };
            }
        }

        private async Task<(int Day, int Week, int Total)?> FetchTrafficAsync(string systemName, CancellationToken cancellationToken)
        {
            var url = $"https://www.edsm.net/api-system-v1/traffic?systemName={Uri.EscapeDataString(systemName)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var jsonDoc = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = jsonDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (!root.TryGetProperty("traffic", out var traffic) || traffic.ValueKind != JsonValueKind.Object)
                return null;

            int day = traffic.TryGetProperty("day", out var d) && d.TryGetInt32(out var dv) ? dv : 0;
            int week = traffic.TryGetProperty("week", out var w) && w.TryGetInt32(out var wv) ? wv : 0;
            int total = traffic.TryGetProperty("total", out var t) && t.TryGetInt32(out var tv) ? tv : 0;

            return (day, week, total);
        }

        private string? CleanEdsmSecurity(string? edsmSecurity)
        {
            if (string.IsNullOrEmpty(edsmSecurity))
            {
                return null;
            }

            // EDSM returns values like "$SYSTEM_SECURITY_high;", we want "High".
            return edsmSecurity.Replace("$SYSTEM_SECURITY_", "").Replace(";", "") switch
            {
                "low" => "Low",
                "medium" => "Medium",
                "high" => "High",
                _ => edsmSecurity
            };
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
