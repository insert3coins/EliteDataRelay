using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Captures recent journal events and exposes them for the History tab.
    /// </summary>
    public sealed class JournalHistoryService : IDisposable
    {
        private const int MaxEntries = 1000;
        private static readonly TimeSpan SaveThrottle = TimeSpan.FromSeconds(5);
        private const int MaxHashes = 5000;

        private readonly IJournalWatcherService _journalWatcherService;
        private readonly List<JournalHistoryEntry> _entries = new();
        private readonly object _lock = new();
        private readonly string _statePath = Path.Combine(AppConfiguration.AppDataPath, "journal-history.json");
        private readonly HashSet<string> _seenHashes = new(StringComparer.Ordinal);
        private readonly Queue<string> _hashOrder = new();
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private int _disposed;
        private Task? _loadTask;
        private volatile bool _stateLoaded;
        private int _notifyScheduled;
        private static readonly TimeSpan NotifyDebounce = TimeSpan.FromMilliseconds(250);

        public JournalHistoryService(IJournalWatcherService journalWatcherService)
        {
            _journalWatcherService = journalWatcherService ?? throw new ArgumentNullException(nameof(journalWatcherService));
        }

        public event EventHandler? HistoryUpdated;

        public void Start()
        {
            EnsureLoadedAsync();
            _journalWatcherService.JournalEventReceived += OnJournalEventReceived;
        }

        public void Stop()
        {
            _journalWatcherService.JournalEventReceived -= OnJournalEventReceived;
        }

        public IReadOnlyList<JournalHistoryEntry> GetEntries(int maxCount = 500)
        {
            lock (_lock)
            {
                return _entries.Take(maxCount).Select(Clone).ToList();
            }
        }

        private void EnsureLoadedAsync()
        {
            if (_stateLoaded || _loadTask != null) return;
            _loadTask = Task.Run(() =>
            {
                LoadState();
                _stateLoaded = true;
                try { HistoryUpdated?.Invoke(this, EventArgs.Empty); } catch { }
            });
        }

        private void OnJournalEventReceived(object? sender, JournalEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.RawLine);
                var root = doc.RootElement;
                var entry = CreateEntry(root, e.EventName, e.RawLine);
                if (entry == null) return;
                var hash = ComputeHash(e.RawLine);

                lock (_lock)
                {
                    if (_seenHashes.Contains(hash))
                    {
                        return;
                    }

                    _entries.Insert(0, entry);
                    RegisterHash(hash);
                    if (_entries.Count > MaxEntries)
                    {
                        _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
                    }
                }

                ThrottledSave();
                ScheduleNotify();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JournalHistory] Failed to process journal event: {ex.Message}");
            }
        }

        private static JournalHistoryEntry? CreateEntry(JsonElement root, string eventName, string rawLine)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;

            var ts = ReadTimestamp(root);
            var starSystem = TryGetString(root, "StarSystem");
            var body = TryGetString(root, "Body");
            var station = TryGetString(root, "StationName");
            var category = GetCategory(eventName);
            var summary = BuildSummaryDetailed(eventName, root, starSystem, station, body);

            return new JournalHistoryEntry
            {
                TimestampUtc = ts,
                EventName = eventName,
                Category = category,
                StarSystem = starSystem,
                Body = body,
                Station = station,
                Summary = summary,
                RawJson = rawLine
            };
        }

        private static string BuildSummaryLegacy(string eventName, JsonElement root, string? starSystem, string? station, string? body)
        {
            switch (eventName)
            {
                case "FSDJump":
                case "CarrierJump":
                    var dist = TryGetDouble(root, "JumpDist");
                    var fuel = TryGetDouble(root, "FuelUsed");
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(starSystem)) parts.Add(starSystem);
                    if (dist.HasValue) parts.Add($"{dist.Value:F1} ly");
                    if (fuel.HasValue) parts.Add($"{fuel.Value:F1} t fuel");
                    return parts.Count > 0 ? string.Join(" · ", parts) : eventName;

                case "Location":
                    bool docked = TryGetBool(root, "Docked") ?? false;
                    if (docked && !string.IsNullOrWhiteSpace(station))
                    {
                        return $"Docked at {station}" + (!string.IsNullOrWhiteSpace(starSystem) ? $" ({starSystem})" : string.Empty);
                    }
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        return $"{starSystem ?? "Unknown"} @ {body}";
                    }
                    return starSystem ?? eventName;

                case "Docked":
                    var faction = TryGetString(root, "StationFaction");
                    return string.IsNullOrWhiteSpace(station)
                        ? "Docked"
                        : $"Docked at {station}" + (string.IsNullOrWhiteSpace(starSystem) ? string.Empty : $" ({starSystem})");

                case "Undocked":
                    return $"Undocked{(string.IsNullOrWhiteSpace(station) ? string.Empty : $" from {station}")}";

                case "Loadout":
                case "ShipyardSwap":
                case "ShipyardBuy":
                case "ShipyardNew":
                    var ship = TryGetString(root, "Ship_Localised") ?? TryGetString(root, "Ship") ?? "Ship";
                    var name = TryGetString(root, "ShipName");
                    return string.IsNullOrWhiteSpace(name) ? ship : $"{ship} \"{name}\"";

                case "Market":
                case "Outfitting":
                case "Shipyard":
                    return string.IsNullOrWhiteSpace(station) ? eventName : $"{station} market";

                case "Scan":
                case "SAAScanComplete":
                case "NavBeaconScan":
                    var bodyName = TryGetString(root, "BodyName") ?? body ?? "Body";
                    return $"{eventName} · {bodyName}";

                case "FSSDiscoveryScan":
                    var bodies = TryGetInt(root, "Bodies");
                    return bodies.HasValue ? $"Discovered {bodies.Value} bodies" : "Discovery scan";

                case "SellExplorationData":
                case "MultiSellExplorationData":
                    var count = TryGetArrayLength(root, "Systems");
                    return count.HasValue ? $"Sold data for {count.Value} systems" : "Sold exploration data";

                default:
                    if (!string.IsNullOrWhiteSpace(starSystem))
                    {
                        return $"{eventName} @ {starSystem}";
                    }
                    return eventName;
            }
        }

        private static string BuildSummaryDetailed(string eventName, JsonElement root, string? starSystem, string? station, string? body)
        {
            string summary = eventName;
            bool includeLocationSuffix = true;
            var usedProps = new List<string> { "event", "timestamp" };

            switch (eventName)
            {
                case "FSDJump":
                case "CarrierJump":
                    var dist = TryGetDouble(root, "JumpDist");
                    var fuel = TryGetDouble(root, "FuelUsed");
                    var fsdParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(starSystem)) fsdParts.Add(starSystem);
                    if (dist.HasValue) fsdParts.Add($"{dist.Value:F1} ly");
                    if (fuel.HasValue) fsdParts.Add($"{fuel.Value:F1} t fuel");
                    summary = fsdParts.Count > 0 ? string.Join(" | ", fsdParts) : eventName;
                    usedProps.AddRange(new[] { "JumpDist", "FuelUsed" });
                    break;

                case "Location":
                    bool docked = TryGetBool(root, "Docked") ?? false;
                    summary = docked && !string.IsNullOrWhiteSpace(station)
                        ? $"Docked at {station}"
                        : (!string.IsNullOrWhiteSpace(body) ? $"{body}" : (starSystem ?? eventName));
                    usedProps.Add("Docked");
                    break;

                case "Docked":
                    summary = string.IsNullOrWhiteSpace(station) ? "Docked" : $"Docked at {station}";
                    break;

                case "Undocked":
                    summary = $"Undocked{(string.IsNullOrWhiteSpace(station) ? string.Empty : $" from {station}")}";
                    break;

                case "Loadout":
                case "ShipyardSwap":
                case "ShipyardBuy":
                case "ShipyardNew":
                    var ship = TryGetString(root, "Ship_Localised") ?? TryGetString(root, "Ship") ?? "Ship";
                    var name = TryGetString(root, "ShipName");
                    summary = string.IsNullOrWhiteSpace(name) ? ship : $"{ship} \"{name}\"";
                    usedProps.AddRange(new[] { "Ship_Localised", "Ship", "ShipName" });
                    break;

                case "Market":
                case "Outfitting":
                case "Shipyard":
                    summary = string.IsNullOrWhiteSpace(station) ? eventName : $"{station} market";
                    break;

                case "Scan":
                case "SAAScanComplete":
                case "NavBeaconScan":
                    var bodyName = TryGetString(root, "BodyName") ?? body ?? "Body";
                    var wasMapped = TryGetBool(root, "WasMapped");
                    var wasDiscovered = TryGetBool(root, "WasDiscovered");
                    var scanParts = new List<string> { bodyName };
                    if (wasDiscovered.HasValue) scanParts.Add(wasDiscovered.Value ? "first discover" : "discovered");
                    if (wasMapped.HasValue) scanParts.Add(wasMapped.Value ? "mapped" : "unmapped");
                    summary = $"{eventName} | {string.Join(" | ", scanParts)}";
                    usedProps.AddRange(new[] { "BodyName", "WasMapped", "WasDiscovered" });
                    break;

                case "FSSDiscoveryScan":
                    var bodies = TryGetInt(root, "Bodies");
                    summary = bodies.HasValue ? $"Discovered {bodies.Value} bodies" : "Discovery scan";
                    usedProps.Add("Bodies");
                    break;

                case "SellExplorationData":
                case "MultiSellExplorationData":
                    var count = TryGetArrayLength(root, "Systems");
                    var earnings = TryGetLong(root, "TotalEarnings");
                    var pieces = new List<string>();
                    if (count.HasValue) pieces.Add($"{count.Value} systems");
                    if (earnings.HasValue) pieces.Add($"{earnings.Value:N0} cr");
                    summary = pieces.Count > 0 ? $"Sold exploration data | {string.Join(" | ", pieces)}" : "Sold exploration data";
                    usedProps.AddRange(new[] { "Systems", "TotalEarnings" });
                    break;

                case "SendText":
                    var sendMsg = TryGetString(root, "Message");
                    var sendTo = TryGetString(root, "To") ?? TryGetString(root, "Channel");
                    summary = $"Sent{(string.IsNullOrWhiteSpace(sendTo) ? string.Empty : $" ({sendTo})")}: {TruncateMessage(sendMsg)}";
                    usedProps.AddRange(new[] { "Message", "To", "Channel" });
                    includeLocationSuffix = false;
                    break;

                case "ReceiveText":
                    var recvMsg = TryGetString(root, "Message_Localised") ?? TryGetString(root, "Message");
                    summary = string.IsNullOrWhiteSpace(recvMsg) ? "Received message" : TruncateMessage(recvMsg);
                    usedProps.AddRange(new[] { "Message_Localised", "Message" });
                    includeLocationSuffix = false;
                    break;

                case "FuelScoop":
                    var scooped = TryGetDouble(root, "Scooped");
                    var total = TryGetDouble(root, "Total");
                    var scoopParts = new List<string> { "Fuel scoop" };
                    if (scooped.HasValue) scoopParts.Add($"{scooped.Value:F2} t");
                    if (total.HasValue) scoopParts.Add($"Total {total.Value:F2} t");
                    summary = string.Join(" | ", scoopParts);
                    usedProps.AddRange(new[] { "Scooped", "Total" });
                    break;

                case "FSDTarget":
                    var targetName = TryGetString(root, "Name") ?? "FSD Target";
                    var remaining = TryGetInt(root, "RemainingJumpsInRoute");
                    var starClass = TryGetString(root, "StarClass");
                    var targetParts = new List<string> { targetName };
                    if (!string.IsNullOrWhiteSpace(starClass)) targetParts.Add($"Class {starClass}");
                    if (remaining.HasValue) targetParts.Add($"{remaining.Value} jumps left");
                    summary = string.Join(" | ", targetParts);
                    usedProps.AddRange(new[] { "Name", "StarClass", "RemainingJumpsInRoute" });
                    includeLocationSuffix = false;
                    break;

                case "StartJump":
                    var jumpType = TryGetString(root, "JumpType");
                    var targetSystem = TryGetString(root, "StarSystem");
                    var startStarClass = TryGetString(root, "StarClass");
                    var startParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(jumpType)) startParts.Add(jumpType);
                    if (!string.IsNullOrWhiteSpace(targetSystem)) startParts.Add($"to {targetSystem}");
                    if (!string.IsNullOrWhiteSpace(startStarClass)) startParts.Add($"({startStarClass})");
                    summary = startParts.Count > 0 ? $"Jump {string.Join(" ", startParts)}" : eventName;
                    usedProps.AddRange(new[] { "JumpType", "StarSystem", "StarClass" });
                    includeLocationSuffix = false;
                    break;

                case "JetConeBoost":
                    var boostVal = TryGetDouble(root, "BoostValue");
                    summary = boostVal.HasValue ? $"Jet cone boost x{boostVal.Value:F1}" : "Jet cone boost";
                    usedProps.Add("BoostValue");
                    break;

                case "ReservoirReplenished":
                    var fuelMain = TryGetDouble(root, "FuelMain");
                    var fuelRes = TryGetDouble(root, "FuelReservoir");
                    var resParts = new List<string> { "Reservoir replenished" };
                    if (fuelMain.HasValue) resParts.Add($"Main {fuelMain.Value:F2} t");
                    if (fuelRes.HasValue) resParts.Add($"Reserve {fuelRes.Value:F2} t");
                    summary = string.Join(" | ", resParts);
                    usedProps.AddRange(new[] { "FuelMain", "FuelReservoir" });
                    break;

                case "FSSAllBodiesFound":
                    var countBodies = TryGetInt(root, "Count");
                    summary = countBodies.HasValue ? $"All bodies found ({countBodies.Value})" : "All bodies found";
                    usedProps.Add("Count");
                    break;

                case "FSSSignalDiscovered":
                    var signalName = TryGetString(root, "SignalName_Localised") ?? TryGetString(root, "SignalName") ?? "Signal discovered";
                    var signalStrength = TryGetDouble(root, "Strength");
                    summary = signalStrength.HasValue
                        ? $"{signalName} | Strength {signalStrength.Value:F1}"
                        : signalName;
                    usedProps.AddRange(new[] { "SignalName_Localised", "SignalName", "Strength" });
                    break;

                case "ShipLocker":
                    summary = SummarizeShipLocker(root);
                    usedProps.AddRange(new[] { "Items", "Components", "Consumables", "Data" });
                    includeLocationSuffix = false;
                    break;

                default:
                    summary = eventName;
                    break;
            }

            if (includeLocationSuffix)
            {
                var location = BuildLocationSuffix(starSystem, station, body);
                if (!string.IsNullOrWhiteSpace(location))
                {
                    summary = $"{summary} | {location}";
                }
            }

            var extras = BuildPropertySummary(root, usedProps);
            if (!string.IsNullOrWhiteSpace(extras))
            {
                summary = $"{summary} | {extras}";
            }

            return summary;
        }

        private static string BuildLocationSuffix(string? starSystem, string? station, string? body)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(starSystem)) parts.Add(starSystem);
            if (!string.IsNullOrWhiteSpace(station)) parts.Add(station);
            if (!string.IsNullOrWhiteSpace(body)) parts.Add(body);
            return parts.Count > 0 ? string.Join(" / ", parts) : string.Empty;
        }

        private static string GetCategory(string eventName)
        {
            return eventName switch
            {
                "FSDJump" => "travel",
                "CarrierJump" => "travel",
                "StartJump" => "travel",
                "Location" => "travel",
                "SupercruiseEntry" => "travel",
                "SupercruiseExit" => "travel",
                "Docked" => "station",
                "Undocked" => "station",
                "Market" => "market",
                "Outfitting" => "market",
                "Shipyard" => "market",
                "ShipyardBuy" => "ship",
                "ShipyardSell" => "ship",
                "ShipyardSwap" => "ship",
                "Loadout" => "ship",
                "SetUserShipName" => "ship",
                "FSSDiscoveryScan" => "exploration",
                "FSSAllBodiesFound" => "exploration",
                "DiscoveryScan" => "exploration",
                "Scan" => "exploration",
                "SAAScanComplete" => "exploration",
                "NavBeaconScan" => "exploration",
                "SellExplorationData" => "exploration",
                "MultiSellExplorationData" => "exploration",
                "Bounty" => "combat",
                "Died" => "combat",
                "Resurrect" => "combat",
                "FuelScoop" => "travel",
                "FSDTarget" => "travel",
                "JetConeBoost" => "travel",
                "ReservoirReplenished" => "travel",
                _ => "other"
            };
        }

        private static string BuildPropertySummary(JsonElement root, IEnumerable<string> alreadyUsed)
        {
            var used = new HashSet<string>(alreadyUsed, StringComparer.OrdinalIgnoreCase)
            {
                "timestamp",
                "event",
                "StarSystem",
                "SystemAddress",
                "Body",
                "BodyID",
                "BodyName",
                "StationName",
                "StarPos",
                "StarPosFromCmdr",
                "Taxi",
                "Multicrew",
                "OnFoot"
            };

            var parts = new List<string>();
            foreach (var prop in root.EnumerateObject())
            {
                if (used.Contains(prop.Name)) continue;
                if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array) continue;

                string? value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => FormatNumber(prop.Value),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(value)) continue;
                if (value.Length > 64) value = value.Substring(0, 64) + "...";
                parts.Add($"{prop.Name}: {value}");
                if (parts.Count >= 6) break;
            }

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        private static string SummarizeShipLocker(JsonElement root)
        {
            var parts = new List<string> { "Ship locker" };
            AddShipLockerPart(root, "Items", parts);
            AddShipLockerPart(root, "Components", parts);
            AddShipLockerPart(root, "Consumables", parts);
            AddShipLockerPart(root, "Data", parts);

            return string.Join(" | ", parts);
        }

        private static void AddShipLockerPart(JsonElement root, string propertyName, List<string> parts)
        {
            if (!root.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            {
                return;
            }

            int totalCount = 0;
            var topItems = new List<(string Name, int Count)>();

            foreach (var item in arr.EnumerateArray())
            {
                var count = item.TryGetProperty("Count", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var cv) ? cv : 0;
                var name = TryGetString(item, "Name_Localised") ?? TryGetString(item, "Name") ?? "Unknown";
                totalCount += count;
                topItems.Add((name, count));
            }

            topItems = topItems
                .OrderByDescending(t => t.Count)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            var topLabel = topItems.Count > 0
                ? $" top: {string.Join(", ", topItems.Select(t => $"{t.Name} x{t.Count}"))}"
                : string.Empty;

            parts.Add($"{propertyName}: {arr.GetArrayLength()} types (total {totalCount}){topLabel}");
        }

        private static string FormatNumber(JsonElement value)
        {
            if (value.TryGetInt64(out var l)) return l.ToString("N0", CultureInfo.InvariantCulture);
            if (value.TryGetDouble(out var d)) return d.ToString("0.###", CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static string? TryGetString(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }

        private static bool? TryGetBool(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) &&
                (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
            {
                return prop.GetBoolean();
            }
            return null;
        }

        private static double? TryGetDouble(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var value))
            {
                return value;
            }
            return null;
        }

        private static int? TryGetInt(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
            {
                return value;
            }
            return null;
        }

        private static long? TryGetLong(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var value))
            {
                return value;
            }
            return null;
        }

        private static string TruncateMessage(string? message)
        {
            if (string.IsNullOrEmpty(message)) return "(blank)";
            const int max = 140;
            return message.Length <= max ? message : message.Substring(0, max) + "…";
        }

        private static int? TryGetArrayLength(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                return prop.GetArrayLength();
            }
            return null;
        }

        private static DateTime ReadTimestamp(JsonElement root)
        {
            if (root.TryGetProperty("timestamp", out var tsProp) &&
                tsProp.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(tsProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            return DateTime.UtcNow;
        }

        private void ThrottledSave()
        {
            var now = DateTime.UtcNow;
            if (now - _lastSaveUtc < SaveThrottle)
            {
                return;
            }

            _lastSaveUtc = now;
            SaveState();
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(_statePath))
                {
                    return;
                }

                var json = File.ReadAllText(_statePath);
                List<JournalHistoryEntry>? loadedEntries = null;
                List<string>? loadedHashes = null;

                try
                {
                    // New format: object with entries and hashes
                    var state = JsonSerializer.Deserialize<HistoryState>(json);
                    if (state?.Entries != null)
                    {
                        loadedEntries = state.Entries;
                        loadedHashes = state.Hashes;
                    }
                }
                catch
                {
                    // fallback to legacy list format
                    loadedEntries = JsonSerializer.Deserialize<List<JournalHistoryEntry>>(json);
                }

                if (loadedEntries == null) return;

                lock (_lock)
                {
                    var merged = new List<JournalHistoryEntry>(_entries.Count + loadedEntries.Count);
                    var seen = new HashSet<string>(_seenHashes, StringComparer.Ordinal);

                    // prefer already-captured live entries first
                    foreach (var existing in _entries)
                    {
                        var h = ComputeHash(existing.RawJson);
                        if (seen.Add(h))
                        {
                            merged.Add(existing);
                            RegisterHashInternal(h);
                        }
                    }

                    foreach (var loaded in loadedEntries)
                    {
                        var h = ComputeHash(loaded.RawJson);
                        if (seen.Add(h))
                        {
                            merged.Add(loaded);
                            RegisterHashInternal(h);
                        }
                    }

                    _entries.Clear();
                    _entries.AddRange(merged.Take(MaxEntries));

                    if (loadedHashes != null)
                    {
                        foreach (var h in loadedHashes.Take(MaxHashes))
                        {
                            if (string.IsNullOrWhiteSpace(h)) continue;
                            RegisterHashInternal(h);
                        }
                    }
                }
                ScheduleNotify();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JournalHistory] Failed to load state: {ex.Message}");
            }
        }

        private void SaveState()
        {
            try
            {
                Directory.CreateDirectory(AppConfiguration.AppDataPath);
                List<JournalHistoryEntry> snapshot;
                List<string> hashSnapshot;
                lock (_lock)
                {
                    snapshot = _entries.Take(MaxEntries).Select(Clone).ToList();
                    hashSnapshot = _hashOrder.ToList();
                }

                var options = new JsonSerializerOptions { WriteIndented = false };
                var state = new HistoryState
                {
                    Entries = snapshot,
                    Hashes = hashSnapshot
                };
                var json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(_statePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[JournalHistory] Failed to save state: {ex.Message}");
            }
        }

        private void ScheduleNotify()
        {
            if (Interlocked.Exchange(ref _notifyScheduled, 1) == 1) return;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(NotifyDebounce).ConfigureAwait(false);
                    HistoryUpdated?.Invoke(this, EventArgs.Empty);
                }
                catch { /* ignore */ }
                finally
                {
                    Interlocked.Exchange(ref _notifyScheduled, 0);
                }
            });
        }

        private string ComputeHash(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        private void RegisterHash(string hash)
        {
            RegisterHashInternal(hash);
            while (_hashOrder.Count > MaxHashes)
            {
                var removed = _hashOrder.Dequeue();
                _seenHashes.Remove(removed);
            }
        }

        private void RegisterHashInternal(string hash)
        {
            if (_seenHashes.Add(hash))
            {
                _hashOrder.Enqueue(hash);
            }
        }

        private sealed class HistoryState
        {
            public List<JournalHistoryEntry>? Entries { get; set; }
            public List<string>? Hashes { get; set; }
        }

        private static JournalHistoryEntry Clone(JournalHistoryEntry entry) => new()
        {
            TimestampUtc = entry.TimestampUtc,
            EventName = entry.EventName,
            Summary = entry.Summary,
            Category = entry.Category,
            StarSystem = entry.StarSystem,
            Body = entry.Body,
            Station = entry.Station,
            RawJson = entry.RawJson
        };

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            Stop();
            SaveState();
            _loadTask?.Dispose();
        }
    }
}
