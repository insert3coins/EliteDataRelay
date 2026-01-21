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
            "Docked", "Undocked", "SupercruiseEntry", "SupercruiseExit",

            // Game state
            "Commander", "LoadGame", "Statistics", "Fileheader", "NewCommander", "ClearSavedGame",
            "Rank", "Progress", "Reputation", "EngineerProgress",

            // Ship & Modules
            "Loadout", "ShipyardSwap", "ShipyardNew", "ShipyardTransfer", "ShipyardSell", "ShipyardBuy", "SetUserShipName",
            "ModuleBuy", "ModuleSell", "MassModuleStore", "ModuleStore", "ModuleRetrieve", "ModuleSellRemote", "FetchRemoteModule",
            "SellShipOnRebuy", "TechnologyBroker", "AfmuRepairs",

            // Station Services & Market
            "Market", "MarketBuy", "MarketSell", "Outfitting", "Shipyard", "CarrierTradeOrder", "SellDrones",
            "Repair", "RepairAll", "RefuelAll", "RefuelPartial", "RestockVehicle", "BuyAmmo",

            // Cargo & Materials
            "Cargo", "CollectCargo", "EjectCargo", "MiningRefined", "CargoDepot",
            "Materials", "MaterialCollected", "MaterialDiscarded", "MaterialTrade", "Synthesis",
            "EngineerCraft", "EngineerContribution",

            // Exploration
            "Touchdown", "Liftoff",
            "FSSDiscoveryScan", "FSSAllBodiesFound", "DiscoveryScan", "Scan", "SAAScanComplete", "NavBeaconScan",
            "SellExplorationData", "MultiSellExplorationData", "SellOrganicData", "BuyExplorationData",

            // Combat & Crime
            "Died", "PVPKill", "CommitCrime", "CrimeVictim",

            // Credits & Vouchers
            "MissionCompleted", "MissionFailed", "MissionAbandoned", "SearchAndRescue", "CommunityGoalReward",
            "Bounty", "FactionKillBond", "CapShipBond", "Resurrect",
            "PayFines", "PayBounties", "PayLegacyFines", "RedeemVoucher", "DatalinkVoucher",

            // Powerplay
            "PowerplaySalary", "PowerplayVoucher", "PowerplayDefect", "PowerplayFastTrack",

            // Crew & Wings
            "CrewHire", "WingAdd", "WingJoin", "WingLeave", "WingInvite",

            // Carrier Management
            "CarrierBankTransfer", "CarrierFinance", "CarrierCrewServices", "CarrierBuy", "CarrierSell", "CarrierStats",

            // On-Foot (Odyssey)
            "BuySuit", "SellSuit", "BuyWeapon", "SellWeapon", "UpgradeSuit", "UpgradeWeapon",
            "BuyMicroResources", "SellMicroResources", "BookTaxi"
        };

        private readonly IJournalWatcherService _journalWatcher;
        private readonly ConcurrentQueue<QueuedEvent> _queue = new();
        private readonly HashSet<string> _sentEventHashes = new(StringComparer.Ordinal);
        private readonly Queue<string> _sentEventOrder = new();
        private readonly object _stateLock = new();
        private readonly string _stateFilePath = Path.Combine(AppConfiguration.AppDataPath, "edsm-upload-state.json");
        private readonly string _softwareVersion;
        public event EventHandler<EdsmUploadStatus>? StatusChanged;
        private DateTime? _lastSentTimestampUtc;
        private CancellationTokenSource? _cts;
        private Task? _worker;
        private bool _started;
        private string _gameVersion = "Unknown";
        private string _gameBuild = "Unknown";
        private string? _lastBalanceRaw;
        private string? _lastBalanceEventName;

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
            SendCachedBalanceSnapshotIfAvailable();
            EnsureWorker();
            NotifyStatusChanged();

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
            NotifyStatusChanged();

            Trace.WriteLine("[EdsmUpload] Stopped.");
        }

        private void OnJournalEventReceived(object? sender, JournalEventArgs e)
        {
            // Settings-only: if commander/API not set, skip early.
            if (!TryGetCredentials(out _, out _))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(e.RawLine);
                var root = doc.RootElement;
                var evtTimestamp = GetTimestampUtc(root);
                var evtName = GetEventName(root) ?? e.EventName;
                if (!IsAllowed(evtName, root))
                {
                    return;
                }
                if (IsBalanceRelated(root))
                {
                    _lastBalanceRaw = e.RawLine;
                    _lastBalanceEventName = evtName;
                }
                var hash = ComputeHash(e.RawLine);

                if (evtName.Equals("Fileheader", StringComparison.OrdinalIgnoreCase))
                {
                    CacheGameVersions(root);
                }

                if (ShouldSkipUpload(evtTimestamp, hash, evtName, forceSend: false))
                {
                    return;
                }

                _queue.Enqueue(new QueuedEvent(root.Clone(), hash, evtTimestamp, evtName, e.RawLine, false));
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
                var token = _cts.Token;

                while (!token.IsCancellationRequested)
                {
                    if (!_queue.TryDequeue(out var item))
                    {
                        await Task.Delay(200, token).ConfigureAwait(false);
                        continue;
                    }

                    await SendEventAsync(item, token).ConfigureAwait(false);
                    await Task.Delay(500, token).ConfigureAwait(false); // small gap to avoid flooding
                }
            }, _cts.Token);
        }

        private async Task SendEventAsync(QueuedEvent queuedEvent, CancellationToken token)
        {
            if (!TryGetCredentials(out var commanderName, out var apiKey))
            {
                Trace.WriteLine("[EdsmUpload] Skipping: commander/API key not configured.");
                return;
            }

            var payload = new
            {
                commanderName,
                apiKey,
                fromSoftware = "EliteDataRelay",
                fromSoftwareVersion = _softwareVersion,
                fromGameVersion = _gameVersion,
                fromGameBuild = _gameBuild,
                message = new[] { queuedEvent.Payload }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var evtName = queuedEvent.EventName ?? GetEventName(queuedEvent.Payload) ?? "unknown";
            Trace.WriteLine($"[EdsmUpload] POST api-journal-v1 event='{evtName}' as '{commanderName}'.");

            try
            {
                var response = await _httpClient.PostAsync(Endpoint, content, token).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                LogResponse(response, body, evtName);
                if (response.IsSuccessStatusCode)
                {
                    RegisterSent(queuedEvent.EventHash, queuedEvent.TimestampUtc);
                    SaveState();
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Error sending event '{evtName}': {ex.Message}");
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

        private static bool IsAllowed(string eventName, JsonElement root)
        {
            if (_allowedEvents.Contains(eventName))
            {
                return true;
            }

            // Fallback: allow events that contain balance/credits data.
            return IsBalanceRelated(root);
        }

        private static bool IsBalanceRelated(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;

            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name;
                if (name.Equals("Balance", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Credits", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Loan", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Statistics event embeds balances in BankAccount
                if (name.Equals("BankAccount", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    return true;
                }
            }

            return false;
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

        private void CacheGameVersions(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return;

            string? gameVersion = null;
            if (root.TryGetProperty("gameversion", out var gv) && gv.ValueKind == JsonValueKind.String)
                gameVersion = gv.GetString();
            else if (root.TryGetProperty("gameVersion", out var gv2) && gv2.ValueKind == JsonValueKind.String)
                gameVersion = gv2.GetString();

            string? gameBuild = null;
            if (root.TryGetProperty("build", out var gb) && gb.ValueKind == JsonValueKind.String)
                gameBuild = gb.GetString();
            else if (root.TryGetProperty("gamebuild", out var gb2) && gb2.ValueKind == JsonValueKind.String)
                gameBuild = gb2.GetString();
            else if (root.TryGetProperty("gameBuild", out var gb3) && gb3.ValueKind == JsonValueKind.String)
                gameBuild = gb3.GetString();

            if (!string.IsNullOrWhiteSpace(gameVersion)) _gameVersion = gameVersion;
            if (!string.IsNullOrWhiteSpace(gameBuild)) _gameBuild = gameBuild;
        }

        private void SendCachedBalanceSnapshotIfAvailable()
        {
            if (string.IsNullOrWhiteSpace(_lastBalanceRaw)) return;

            try
            {
                using var doc = JsonDocument.Parse(_lastBalanceRaw);
                var updatedRaw = CloneWithTimestamp(doc.RootElement, DateTime.UtcNow);
                using var updatedDoc = JsonDocument.Parse(updatedRaw);
                var clone = updatedDoc.RootElement.Clone();
                var evtName = GetEventName(updatedDoc.RootElement) ?? _lastBalanceEventName ?? "Statistics";
                var hash = ComputeHash(updatedRaw + "|start-balance");
                _queue.Enqueue(new QueuedEvent(clone, hash, DateTime.UtcNow, evtName, updatedRaw, true));
                EnsureWorker();
                Trace.WriteLine("[EdsmUpload] Enqueued current balance snapshot on start.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to enqueue balance snapshot: {ex.Message}");
            }
        }

        private static string CloneWithTimestamp(JsonElement root, DateTime timestampUtc)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                if (root.ValueKind == JsonValueKind.Object)
                {
                    writer.WriteStartObject();
                    var hadTimestamp = false;
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("timestamp"))
                        {
                            hadTimestamp = true;
                            writer.WriteString(prop.Name, timestampUtc.ToString("O"));
                        }
                        else
                        {
                            prop.WriteTo(writer);
                        }
                    }
                    if (!hadTimestamp)
                    {
                        writer.WriteString("timestamp", timestampUtc.ToString("O"));
                    }
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("timestamp", timestampUtc.ToString("O"));
                    writer.WriteEndObject();
                }
            }

            return Encoding.UTF8.GetString(stream.ToArray());
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

        private bool ShouldSkipUpload(DateTime timestampUtc, string eventHash, string evtName, bool forceSend)
        {
            if (forceSend) return false;
            lock (_stateLock)
            {
                if (_sentEventHashes.Contains(eventHash))
                {
                    Trace.WriteLine($"[EdsmUpload] Skip '{evtName}': already sent.");
                    return true;
                }
            }

            return false;
        }

        private void RegisterSent(string eventHash, DateTime eventTimestampUtc)
        {
            var sentAtUtc = DateTime.UtcNow;
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

                var latest = sentAtUtc > eventTimestampUtc ? sentAtUtc : eventTimestampUtc;
                if (!_lastSentTimestampUtc.HasValue || latest > _lastSentTimestampUtc.Value)
                {
                    _lastSentTimestampUtc = latest;
                }
            }
            NotifyStatusChanged();
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

        public EdsmUploadStatus GetStatus()
        {
            var hasCredentials = TryGetCredentials(out var commanderName, out _);
            DateTime? lastSent;
            lock (_stateLock)
            {
                lastSent = _lastSentTimestampUtc;
            }

            return new EdsmUploadStatus(_started, hasCredentials, lastSent, commanderName);
        }

        public void RefreshStatus() => NotifyStatusChanged();

        private void NotifyStatusChanged()
        {
            var snapshot = GetStatus();
            try
            {
                StatusChanged?.Invoke(this, snapshot);
            }
            catch
            {
                // Ignore UI errors to keep upload pipeline resilient.
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }

        private readonly record struct QueuedEvent(JsonElement Payload, string EventHash, DateTime TimestampUtc, string EventName, string RawLine, bool ForceSend);

        private sealed class EdsmUploadState
        {
            public DateTime? LastSentTimestampUtc { get; set; }
            public List<string>? EventHashes { get; set; }
        }
    }

    public readonly record struct EdsmUploadStatus(bool IsActive, bool HasCredentials, DateTime? LastSuccessfulUploadUtc, string CommanderName);
}
