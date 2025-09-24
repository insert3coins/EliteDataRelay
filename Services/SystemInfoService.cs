using EliteDataRelay.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        public event EventHandler<SystemInfoData>? SystemInfoUpdated;

        public SystemInfoService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
        }

        public void Start()
        {
            if (_isStarted) return;
            _journalWatcher.LocationChanged += OnLocationChanged;
            _isStarted = true;

        }

        public void Stop()
        {
            if (!_isStarted) return;
            _journalWatcher.LocationChanged -= OnLocationChanged;
            _isStarted = false;
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            // We only want to fetch data for a new system. The initial poll from JournalWatcher will handle the first load.
            if (e.IsNewSystem)
            {
                try
                {
                    var systemInfo = await FetchSystemInfoAsync(e.StarSystem) ?? new SystemInfoData { SystemName = e.StarSystem };
                    SystemInfoUpdated?.Invoke(this, systemInfo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SystemInfoService] Error fetching system info: {ex.Message}");
                }
            }
        }

        private async Task<SystemInfoData?> FetchSystemInfoAsync(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
            {
                return null;
            }

            try
            {
                // EDSM API endpoint for system information
                var url = $"https://www.edsm.net/api-v1/system?systemName={Uri.EscapeDataString(systemName)}&showInformation=1";
                
                // EDSM returns an empty array `[]` if the system is not found,
                // or an object `{...}` if it is. We need to handle both cases.
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var contentStream = await response.Content.ReadAsStreamAsync();
                using var jsonDoc = await JsonDocument.ParseAsync(contentStream);

                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // System not found in EDSM, return default data
                    System.Diagnostics.Debug.WriteLine($"[SystemInfoService] System '{systemName}' not found on EDSM.");
                    return new SystemInfoData { SystemName = systemName };
                }

                var edsmSystem = jsonDoc.RootElement.Deserialize<EdsmSystem>(_jsonOptions);
                var info = edsmSystem?.Information;

                return new SystemInfoData
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
                "anarchy" => "Anarchy",
                var s => s
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }
}