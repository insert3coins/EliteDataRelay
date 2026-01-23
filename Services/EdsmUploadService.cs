using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const string JournalEndpoint = "https://www.edsm.net/api-journal-v1";
        private const string DiscardEndpoint = "https://www.edsm.net/api-journal-v1/discard";
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly string[] DefaultDiscardList =
        {
            "ShutDown","EDDItemSet","EDDCommodityPrices","ModuleArrived","ShipArrived","Coriolis","EDShipyard","Market","Shipyard","Outfitting","ModuleInfo","Status",
            "SquadronCreated","SquadronStartup","DisbandedSquadron","InvitedToSquadron","AppliedToSquadron","JoinedSquadron","LeftSquadron","SharedBookmarkToSquadron",
            "CarrierStats","CarrierTradeOrder","CarrierFinance","CarrierBankTransfer","CarrierCrewServices","CarrierJumpRequest","CarrierJumpCancelled","CarrierDepositFuel",
            "CarrierDockingPermission","CarrierModulePack","CarrierBuy","CarrierNameChange","CarrierDecommission","ColonisationConstructionDepot","ColonisationContribution",
            "BookDropship","CancelDropship","DropshipDeploy","CollectItems","DropItems","Disembark","Embark","Fileheader","Commander","NewCommander","ClearSavedGame",
            "Music","Continued","Passengers","DockingCancelled","DockingDenied","DockingGranted","DockingRequested","DockingTimeout","StartJump","Touchdown","Liftoff",
            "NavBeaconScan","SupercruiseEntry","SupercruiseExit","NavRoute","NavRouteClear","PVPKill","CrimeVictim","UnderAttack","ShipTargeted","Scanned","DataScanned",
            "DatalinkScan","EngineerApply","EngineerLegacyConvert","FactionKillBond","Bounty","CapShipBond","DatalinkVoucher","SystemsShutdown","EscapeInterdiction",
            "HeatDamage","HeatWarning","HullDamage","ShieldState","FuelScoop","LaunchDrone","AfmuRepairs","CockpitBreached","ReservoirReplenished","CargoTransfer",
            "ApproachBody","LeaveBody","DiscoveryScan","MaterialDiscovered","Screenshot","CrewAssign","CrewFire","NpcCrewRank","ShipyardNew","StoredModules",
            "MassModuleStore","ModuleStore","ModuleSwap","SuitLoadout","SwitchSuitLoadout","CreateSuitLoadout","LoadoutEquipModule","PowerplayVote","PowerplayVoucher",
            "PowerplayMerits","ChangeCrewRole","CrewLaunchFighter","CrewMemberJoins","CrewMemberQuits","CrewMemberRoleChange","KickCrewMember","EndCrewSession",
            "LaunchFighter","DockFighter","FighterDestroyed","FighterRebuilt","VehicleSwitch","LaunchSRV","DockSRV","SRVDestroyed","JetConeBoost","JetConeDamage",
            "RebootRepair","RepairDrone","WingAdd","WingInvite","WingJoin","WingLeave","ReceiveText","SendText","Shutdown","SupercruiseDestinationDrop",
            "FSSSignalDiscovered","AsteroidCracked","ProspectedAsteroid","ScanBaryCentre","FSSBodySignals","SAASignalsFound","ScanOrganic"
        };
        private static readonly string[] ContextSeedEvents =
        {
            "LoadGame","FSDJump","CarrierJump","Location","Loadout","Statistics","Rank","Reputation","Progress","EngineerProgress","Balance"
        };

        private readonly HashSet<string> _discardedEvents = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _discardLock = new();

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
            LoadDefaultDiscardList();
            LoadState();
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _cts = new CancellationTokenSource();

            PrimeGameVersionFromHistory();
            _ = RefreshDiscardListAsync(_cts.Token);

            _journalWatcher.JournalEventReceived += OnJournalEventReceived;

            // When fast-start is enabled we skip historical lines. Replay a minimal set of
            // recent context events (last load/game/location/loadout) so EDSM has the
            // correct commander state before live streaming begins.
            if (AppConfiguration.FastStartSkipJournalHistory)
            {
                SeedContextFromHistory();
            }

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

        /// <summary>
        /// Enqueue an immediate Balance snapshot (used when Status.json balance changes but no Balance journal event fires).
        /// </summary>
        public void EnqueueBalanceSnapshot(long balance, long? loan = null)
        {
            if (!TryGetCredentials(out _, out _))
            {
                return;
            }

            var timestamp = DateTime.UtcNow;
            var json = BuildBalanceJson(balance, loan, timestamp);
            using var doc = JsonDocument.Parse(json);
            var clone = doc.RootElement.Clone();
            var hash = ComputeHash(json);

            // Remember latest balance for restart snapshot logic
            _lastBalanceRaw = json;
            _lastBalanceEventName = "Balance";

            if (ShouldSkipUpload(timestamp, hash, "Balance", forceSend: false))
            {
                return;
            }

            _queue.Enqueue(new QueuedEvent(clone, hash, timestamp, "Balance", json, false));
            EnsureWorker();
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
                ProcessParsedEvent(doc.RootElement, e.RawLine, e.EventName, forceSend: false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to parse journal line for EDSM: {ex.Message}");
            }
        }

        private void ProcessParsedEvent(JsonElement root, string rawLine, string? eventNameOverride, bool forceSend)
        {
            var evtName = eventNameOverride ?? GetEventName(root);
            if (string.IsNullOrWhiteSpace(evtName))
            {
                return;
            }

            if (evtName.Equals("Fileheader", StringComparison.OrdinalIgnoreCase) ||
                evtName.Equals("LoadGame", StringComparison.OrdinalIgnoreCase))
            {
                CacheGameVersions(root);
            }

            if (!IsAllowed(evtName, root))
            {
                return;
            }

            if (IsBalanceRelated(root))
            {
                _lastBalanceRaw = rawLine;
                _lastBalanceEventName = evtName;
            }

            var evtTimestamp = GetTimestampUtc(root);
            var hash = ComputeHash(rawLine);

            if (ShouldSkipUpload(evtTimestamp, hash, evtName, forceSend))
            {
                return;
            }

            _queue.Enqueue(new QueuedEvent(root.Clone(), hash, evtTimestamp, evtName, rawLine, forceSend));
            EnsureWorker();
        }

        private void SeedContextFromHistory()
        {
            if (!TryGetCredentials(out _, out _))
            {
                return;
            }

            try
            {
                foreach (var line in GetContextSeedLines())
                {
                    using var doc = JsonDocument.Parse(line);
                    ProcessParsedEvent(doc.RootElement, line, eventNameOverride: null, forceSend: false);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to seed context events: {ex.Message}");
            }
        }

        private IEnumerable<string> GetContextSeedLines()
        {
            var collected = new List<(DateTime ts, string raw)>();
            try
            {
                var journalDir = AppConfiguration.JournalPath;
                if (!Directory.Exists(journalDir)) return Enumerable.Empty<string>();

                var targets = new HashSet<string>(ContextSeedEvents, StringComparer.OrdinalIgnoreCase);
                var recentFiles = Directory.EnumerateFiles(journalDir, "Journal.*.log")
                    .OrderByDescending(f => f)
                    .Take(2);

                foreach (var file in recentFiles)
                {
                    foreach (var line in File.ReadLines(file).Reverse())
                    {
                        if (targets.Count == 0) break;
                        if (!TryExtractEvent(line, out var evtName, out var ts)) continue;
                        if (!targets.Contains(evtName!)) continue;

                        targets.Remove(evtName!);
                        collected.Add((ts, line));
                    }

                    if (targets.Count == 0) break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to read context lines: {ex.Message}");
            }

            return collected
                .OrderBy(c => c.ts)
                .Select(c => c.raw)
                .ToList();
        }

        private static bool TryExtractEvent(string line, out string? eventName, out DateTime ts)
        {
            eventName = null;
            ts = DateTime.UtcNow;
            try
            {
                using var doc = JsonDocument.Parse(line);
                eventName = GetEventName(doc.RootElement);
                ts = GetTimestampUtc(doc.RootElement);
                return !string.IsNullOrWhiteSpace(eventName);
            }
            catch
            {
                return false;
            }
        }

        private void PrimeGameVersionFromHistory()
        {
            if (!string.IsNullOrWhiteSpace(_gameVersion) &&
                !_gameVersion.Equals("Unknown", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(_gameBuild) &&
                !_gameBuild.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var journalDir = AppConfiguration.JournalPath;
                if (!Directory.Exists(journalDir)) return;

                var recentFiles = Directory.EnumerateFiles(journalDir, "Journal.*.log")
                    .OrderByDescending(f => f)
                    .Take(2);

                foreach (var file in recentFiles)
                {
                    foreach (var line in File.ReadLines(file).Reverse())
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        using var doc = JsonDocument.Parse(line);
                        if (CacheGameVersions(doc.RootElement))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to prime game version from history: {ex.Message}");
            }
        }

        private void LoadDefaultDiscardList()
        {
            lock (_discardLock)
            {
                _discardedEvents.Clear();
                foreach (var evt in DefaultDiscardList)
                {
                    _discardedEvents.Add(evt);
                }
            }
        }

        private async Task RefreshDiscardListAsync(CancellationToken token)
        {
            try
            {
                var response = await _httpClient.GetAsync(DiscardEndpoint, token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return;

                var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                var events = JsonSerializer.Deserialize<string[]>(body);
                if (events == null || events.Length == 0) return;

                lock (_discardLock)
                {
                    _discardedEvents.Clear();
                    foreach (var evt in events)
                    {
                        if (!string.IsNullOrWhiteSpace(evt))
                        {
                            _discardedEvents.Add(evt);
                        }
                    }
                }

                Trace.WriteLine($"[EdsmUpload] Refreshed discard list ({events.Length} events).");
            }
            catch (OperationCanceledException)
            {
                // shutting down, ignore
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[EdsmUpload] Failed to refresh discard list, using defaults. {ex.Message}");
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

            if (string.IsNullOrWhiteSpace(_gameVersion) || _gameVersion.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(_gameBuild) || _gameBuild.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                PrimeGameVersionFromHistory();
                if (string.IsNullOrWhiteSpace(_gameVersion) || _gameVersion.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(_gameBuild) || _gameBuild.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine("[EdsmUpload] Game version/build unknown; waiting for LoadGame/Fileheader before uploading.");
                    return;
                }
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
            var evtName = queuedEvent.EventName ?? GetEventName(queuedEvent.Payload) ?? "unknown";

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (token.IsCancellationRequested) return;

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                Trace.WriteLine($"[EdsmUpload] POST api-journal-v1 event='{evtName}' as '{commanderName}' (attempt {attempt}/{maxRetries}).");

                try
                {
                    var response = await _httpClient.PostAsync(JournalEndpoint, content, token).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                    LogResponse(response, body, evtName);

                    if (response.IsSuccessStatusCode)
                    {
                        RegisterSent(queuedEvent.EventHash, queuedEvent.TimestampUtc);
                        SaveState();
                        return; // Success, exit the retry loop
                    }

                    // If it's a client error (4xx), don't retry. It's a bad request.
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        Trace.WriteLine($"[EdsmUpload] Client error {response.StatusCode}. Will not retry.");
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    return; // Shutting down, don't retry
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[EdsmUpload] Error sending event '{evtName}' on attempt {attempt}: {ex.Message}");
                }

                // If we got here, it was a server error or a network error. Wait before retrying.
                if (attempt < maxRetries)
                {
                    await Task.Delay(2000 * attempt, token).ConfigureAwait(false); // Wait 2s, 4s
                }
            }

            Trace.WriteLine($"[EdsmUpload] Failed to send event '{evtName}' after {maxRetries} attempts.");
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

        private bool IsAllowed(string eventName, JsonElement root)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return false;
            }

            // Skip anything the server explicitly discards.
            lock (_discardLock)
            {
                if (_discardedEvents.Contains(eventName))
                {
                    return false;
                }
            }

            // Allow all other events (EDSM will ignore unknowns); keep balance-related as a safety net.
            return true;
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

        private bool CacheGameVersions(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;

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

            var updated = false;
            if (!string.IsNullOrWhiteSpace(gameVersion) && !_gameVersion.Equals(gameVersion, StringComparison.OrdinalIgnoreCase))
            {
                _gameVersion = gameVersion;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(gameBuild) && !_gameBuild.Equals(gameBuild, StringComparison.OrdinalIgnoreCase))
            {
                _gameBuild = gameBuild;
                updated = true;
            }

            return updated;
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

        private static string BuildBalanceJson(long balance, long? loan, DateTime timestampUtc)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("timestamp", timestampUtc.ToString("O"));
                writer.WriteString("event", "Balance");
                writer.WriteNumber("Balance", balance);
                if (loan.HasValue) writer.WriteNumber("Loan", loan.Value);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
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
                    if (!string.IsNullOrWhiteSpace(state.GameVersion)) _gameVersion = state.GameVersion;
                    if (!string.IsNullOrWhiteSpace(state.GameBuild)) _gameBuild = state.GameBuild;
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
                        EventHashes = new List<string>(_sentEventOrder),
                        GameVersion = _gameVersion,
                        GameBuild = _gameBuild
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
            public string? GameVersion { get; set; }
            public string? GameBuild { get; set; }
        }
    }

    public readonly record struct EdsmUploadStatus(bool IsActive, bool HasCredentials, DateTime? LastSuccessfulUploadUtc, string CommanderName);
}
