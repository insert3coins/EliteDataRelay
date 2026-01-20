using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Uploads journal events to EDSM via the api-journal-v1 endpoint.
    /// Uses commander name/API key from settings only.
    /// </summary>
    public sealed class EdsmUploadService : IDisposable
    {
        private const int MaxRememberedEvents = 5000;
        private const string Endpoint = "https://www.edsm.net/api-journal-v1";
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly HashSet<string> _allowedEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            "FSDJump", "CarrierJump", "Location",
            "Docked", "Undocked",
            "Loadout", "ShipyardSwap", "ShipyardNew", "ShipyardTransfer", "ShipyardSell", "ShipyardBuy", "SetUserShipName",
            "Market", "Outfitting", "Shipyard", "CarrierTradeOrder",
            "FSSDiscoveryScan", "FSSAllBodiesFound", "DiscoveryScan", "Scan", "SAAScanComplete", "NavBeaconScan",
            "SellExplorationData", "MultiSellExplorationData"
        };

        private readonly IJournalWatcherService _journalWatcher;
        private readonly ConcurrentQueue<QueuedEvent> _queue = new();
        private readonly HashSet<string> _sentEventHashes = new(StringComparer.Ordinal);
        private readonly Queue<string> _sentEventOrder = new();
        private readonly object _stateLock = new();
        private readonly string _stateFilePath = Path.Combine(AppConfiguration.AppDataPath, "edsm-upload-state.json");
        private readonly string _softwareVersion;
        private DateTime? _lastSentTimestampUtc;
        private CancellationTokenSource? _cts;
        private Task? _worker;
        private bool _started;

        public EdsmUploadService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
            _softwareVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
            LoadState();
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _cts = new CancellationTokenSource();

            _journalWatcher.JournalEventReceived += OnJournalEventReceived;
            EnsureWorker();

            Trace.WriteLine("[EdsmUpload] Started.");
        }

        public void Stop()
        {
            if (!_started) return;
            _started = false;

            _journalWatcher.JournalEventReceived -= OnJournalEventReceived;
            _cts?.Cancel();
            _cts = null;
            _worker = null;

            Trace.WriteLine("[EdsmUpload] Stopped.");
        }

        private void OnJournalEventReceived(object? sender, JournalEventArgs e)
        {
            // Settings-only: if commander/API not set, skip early.
            if (!TryGetCredentials(out _, out _))
            {
                return;
            }

            if (!_allowedEvents.Contains(e.EventName))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(e.RawLine);
                var root = doc.RootElement;
                var evtTimestamp = GetTimestampUtc(root);
                var evtName = GetEventName(root) ?? e.EventName;
                var hash = ComputeHash(e.RawLine);

                if (ShouldSkipUpload(evtTimestamp, hash, evtName))
                {
                    return;
                }

                _queue.Enqueue(new QueuedEvent(root.Clone(), hash, evtTimestamp, evtName));
                EnsureWorker();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to parse journal line for EDSM: {ex.Message}");
            }
        }

        private void EnsureWorker()
        {
            if (_worker != null && !_worker.IsCompleted) return;
            if (_cts == null) return;

            _worker = Task.Run(async () =>
            {
                const int batchSize = 10;
                var token = _cts.Token;

                while (!token.IsCancellationRequested)
                {
                    if (_queue.IsEmpty)
                    {
                        await Task.Delay(200, token).ConfigureAwait(false);
                        continue;
                    }

                    var batch = new List<QueuedEvent>(batchSize);
                    while (batch.Count < batchSize && _queue.TryDequeue(out var item))
                    {
                        batch.Add(item);
                    }

                    await SendBatchAsync(batch, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false); // small gap to avoid flooding
                }
            }, _cts.Token);
        }

        private async Task SendBatchAsync(List<QueuedEvent> eventsBatch, CancellationToken token)
        {
            if (eventsBatch.Count == 0) return;
            if (!TryGetCredentials(out var commanderName, out var apiKey))
            {
                Trace.WriteLine("[EdsmUpload] Skipping batch: commander/API key not configured.");
                return;
            }

            // Send events one-by-one (no batching)
            var stateChanged = false;
            foreach (var single in eventsBatch)
            {
                if (token.IsCancellationRequested) break;

                var payload = new
                {
                    commanderName,
                    apiKey,
                    fromSoftware = "EliteDataRelay",
                    fromSoftwareVersion = _softwareVersion,
                    fromGameVersion = "Unknown",
                    fromGameBuild = "Unknown",
                    message = new[] { single.Payload }
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var evtName = single.EventName ?? GetEventName(single.Payload) ?? "unknown";
                Trace.WriteLine($"[EdsmUpload] POST api-journal-v1 event='{evtName}' as '{commanderName}'.");

                try
                {
                    var response = await _httpClient.PostAsync(Endpoint, content, token).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    LogResponse(response, body, evtName);
                    if (response.IsSuccessStatusCode)
                    {
                        RegisterSent(single.EventHash, single.TimestampUtc);
                        stateChanged = true;
                    }
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[EdsmUpload] Error sending event '{evtName}': {ex.Message}");
                }
            }

            if (stateChanged)
            {
                SaveState();
            }
        }

        private static void LogResponse(HttpResponseMessage response, string body, string context)
        {
            int? msgnum = null;
            string? msg = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("msgnum", out var mn) && mn.TryGetInt32(out var mv))
                    msgnum = mv;
                if (root.TryGetProperty("msg", out var m) && m.ValueKind == JsonValueKind.String)
                    msg = m.GetString();
            }
            catch { /* ignore parse errors */ }

            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            var msgInfo = msgnum.HasValue || !string.IsNullOrWhiteSpace(msg)
                ? $" msgnum={msgnum?.ToString() ?? "?"} msg='{msg ?? ""}'"
                : $" body='{Truncate(body)}'";

            var prefix = response.IsSuccessStatusCode ? "[EdsmUpload] OK" : "[EdsmUpload] FAIL";
            Trace.WriteLine($"{prefix} {context}: {status}{msgInfo}");
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            const int max = 200;
            return value.Length <= max ? value : value[..max] + "...";
        }

        private static string ComputeHash(string rawLine)
        {
            var bytes = Encoding.UTF8.GetBytes(rawLine ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        private static DateTime GetTimestampUtc(JsonElement evt)
        {
            if (evt.ValueKind == JsonValueKind.Object &&
                evt.TryGetProperty("timestamp", out var ts) &&
                ts.ValueKind == JsonValueKind.String)
            {
                var value = ts.GetString();
                if (!string.IsNullOrEmpty(value) &&
                    DateTime.TryParse(value, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var parsed))
                {
                    return parsed;
                }
            }

            return DateTime.UtcNow;
        }

        private bool TryGetCredentials(out string commanderName, out string apiKey)
        {
            commanderName = (AppConfiguration.EdsmCommanderName ?? string.Empty).Trim();
            apiKey = (AppConfiguration.EdsmApiKey ?? string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(commanderName) && !string.IsNullOrWhiteSpace(apiKey);
        }

        private static string? GetEventName(JsonElement evt)
        {
            if (evt.ValueKind == JsonValueKind.Object && evt.TryGetProperty("event", out var e) && e.ValueKind == JsonValueKind.String)
            {
                return e.GetString();
            }
            return null;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EliteDataRelay", version));
            }
            catch { /* ignore */ }
            return client;
        }

        private bool ShouldSkipUpload(DateTime timestampUtc, string eventHash, string evtName)
        {
            lock (_stateLock)
            {
                if (_lastSentTimestampUtc.HasValue && timestampUtc <= _lastSentTimestampUtc.Value)
                {
                    Trace.WriteLine($"[EdsmUpload] Skip '{evtName}': timestamp {timestampUtc:O} <= last sent {_lastSentTimestampUtc:O}");
                    return true;
                }

                if (_sentEventHashes.Contains(eventHash))
                {
                    Trace.WriteLine($"[EdsmUpload] Skip '{evtName}': already sent.");
                    return true;
                }
            }

            return false;
        }

        private void RegisterSent(string eventHash, DateTime timestampUtc)
        {
            lock (_stateLock)
            {
                if (_sentEventHashes.Add(eventHash))
                {
                    _sentEventOrder.Enqueue(eventHash);
                    while (_sentEventOrder.Count > MaxRememberedEvents)
                    {
                        var removed = _sentEventOrder.Dequeue();
                        _sentEventHashes.Remove(removed);
                    }
                }

                if (!_lastSentTimestampUtc.HasValue || timestampUtc > _lastSentTimestampUtc.Value)
                {
                    _lastSentTimestampUtc = timestampUtc;
                }
            }
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(_stateFilePath))
                {
                    return;
                }

                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<EdsmUploadState>(json);
                if (state == null) return;

                lock (_stateLock)
                {
                    _lastSentTimestampUtc = state.LastSentTimestampUtc;
                    if (state.EventHashes != null)
                    {
                        foreach (var hash in state.EventHashes)
                        {
                            if (string.IsNullOrWhiteSpace(hash)) continue;
                            if (_sentEventHashes.Add(hash))
                            {
                                _sentEventOrder.Enqueue(hash);
                            }
                        }

                        while (_sentEventOrder.Count > MaxRememberedEvents)
                        {
                            var removed = _sentEventOrder.Dequeue();
                            _sentEventHashes.Remove(removed);
                        }
                    }
                }

                Trace.WriteLine($"[EdsmUpload] Loaded state. Stored={_sentEventOrder.Count}, lastTs={_lastSentTimestampUtc:O}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to load upload state: {ex.Message}");
            }
        }

        private void SaveState()
        {
            try
            {
                Directory.CreateDirectory(AppConfiguration.AppDataPath);
                EdsmUploadState snapshot;
                lock (_stateLock)
                {
                    snapshot = new EdsmUploadState
                    {
                        LastSentTimestampUtc = _lastSentTimestampUtc,
                        EventHashes = new List<string>(_sentEventOrder)
                    };
                }

                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to save upload state: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }

        private readonly record struct QueuedEvent(JsonElement Payload, string EventHash, DateTime TimestampUtc, string EventName);

        private sealed class EdsmUploadState
        {
            public DateTime? LastSentTimestampUtc { get; set; }
            public List<string>? EventHashes { get; set; }
        }
    }
}
