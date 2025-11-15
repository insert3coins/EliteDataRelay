using EliteDataRelay.Models;
using EliteDataRelay.Models.FleetCarrier;
using EliteDataRelay.Models.Journal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Tracks Fleet Carrier activity from journal events and exposes snapshots for the UI.
    /// </summary>
    public sealed class FleetCarrierTrackerService : IDisposable
    {
        private readonly IJournalWatcherService _journalWatcher;
        private readonly object _sync = new();
        private FleetCarrierState? _personalCarrier;
        private FleetCarrierState? _squadronCarrier;
        private ulong? _dockedCarrierMarketId;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private FileSystemWatcher? _marketWatcher;

        public event EventHandler<FleetCarrierState>? PersonalCarrierUpdated;
        public event EventHandler<FleetCarrierState>? SquadronCarrierUpdated;

        public FleetCarrierTrackerService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));

            _journalWatcher.CarrierStats += OnCarrierStats;
            _journalWatcher.CarrierLocation += OnCarrierLocation;
            _journalWatcher.CarrierJumpRequested += OnCarrierJumpRequested;
            _journalWatcher.CarrierJumpCancelled += OnCarrierJumpCancelled;
            _journalWatcher.CarrierTradeOrder += OnCarrierTradeOrder;
            _journalWatcher.CarrierCrewServices += OnCarrierCrewServices;
            _journalWatcher.CarrierBankTransfer += OnCarrierBankTransfer;
            _journalWatcher.CarrierFuelDeposited += OnCarrierFuelDeposited;
            _journalWatcher.CargoTransfer += OnCargoTransfer;
            _journalWatcher.MarketSell += OnMarketSell;
            _journalWatcher.Docked += OnDocked;
            _journalWatcher.Undocked += OnUndocked;

            Task.Run(InitializeFromLatestJournalSafe);
            SetupMarketWatcher();
        }

        public FleetCarrierState? GetPersonalCarrierSnapshot()
        {
            lock (_sync)
            {
                return _personalCarrier?.Clone();
            }
        }

        public FleetCarrierState? GetSquadronCarrierSnapshot()
        {
            lock (_sync)
            {
                return _squadronCarrier?.Clone();
            }
        }

        private void OnCarrierStats(object? sender, CarrierStatsEvent.CarrierStatsEventArgs e) => ApplyCarrierStats(e);

        private void OnCarrierLocation(object? sender, CarrierLocationEvent.CarrierLocationEventArgs e) => ApplyCarrierLocation(e);

        private void OnCarrierJumpRequested(object? sender, CarrierJumpRequestEvent.CarrierJumpRequestEventArgs e)
        {
            lock (_sync)
            {
                var state = EnsureCarrierState(e.CarrierType, e.CarrierID);
                var address = e.SystemAddress ?? e.SystemID ?? 0;
                var departure = e.DepartureTime ?? DateTime.UtcNow;

                state.Destination.Set(e.SystemName ?? "Unknown", e.Body ?? string.Empty, address, departure);
                state.JumpDepartureUtc = departure;
                state.CooldownCompleteUtc = departure.AddMinutes(5);
                state.LastUpdatedUtc = DateTime.UtcNow;

                RaiseSnapshot(state);
            }
        }

        private void OnCarrierJumpCancelled(object? sender, CarrierJumpCancelledEvent.CarrierJumpCancelledEventArgs e)
        {
            lock (_sync)
            {
                var state = EnsureCarrierState(e.CarrierType, e.CarrierID);
                state.Destination.Reset();
                state.JumpDepartureUtc = null;
                state.CooldownCompleteUtc = null;
                state.LastUpdatedUtc = DateTime.UtcNow;
                RaiseSnapshot(state);
            }
        }

        private void OnCarrierTradeOrder(object? sender, CarrierTradeOrderEvent.CarrierTradeOrderEventArgs e)
        {
            lock (_sync)
            {
                if (_personalCarrier == null || _personalCarrier.CarrierId == 0 || e.CarrierID != _personalCarrier.CarrierId || e.BlackMarket)
                {
                    return;
                }

                var localized = string.IsNullOrWhiteSpace(e.Commodity_Localised) ? e.Commodity : e.Commodity_Localised;
                var commodity = _personalCarrier.GetOrAddCommodity(e.Commodity, localized, stolen: false);

                if (e.CancelTrade)
                {
                    commodity.OutstandingPurchaseOrders = 0;
                    commodity.SalePrice = 0;
                    commodity.StockCount = 0;
                    _personalCarrier.RemoveIfEmpty(commodity);
                }
                else
                {
                    if (e.SaleOrder > 0)
                    {
                        commodity.SalePrice = e.Price;
                        commodity.StockCount = e.SaleOrder;
                    }
                    else if (e.SaleOrder == 0 && commodity.StockCount <= 0)
                    {
                        commodity.StockCount = 0;
                    }

                    commodity.OutstandingPurchaseOrders = e.PurchaseOrder;

                    if (commodity.StockCount <= 0 && commodity.OutstandingPurchaseOrders <= 0 && commodity.SalePrice <= 0)
                    {
                        _personalCarrier.RemoveIfEmpty(commodity);
                    }
                }

                _personalCarrier.LastUpdatedUtc = DateTime.UtcNow;
                RaiseSnapshot(_personalCarrier);
            }
        }

        private void OnCarrierCrewServices(object? sender, CarrierCrewServicesEvent.CarrierCrewServicesEventArgs e)
        {
            lock (_sync)
            {
                if (!EventMatchesPersonalCarrier(e.CarrierID))
                {
                    return;
                }

                _personalCarrier!.UpdateCrewStatus(e.CrewRole, e.Operation);
                RaiseSnapshot(_personalCarrier);
            }
        }

        private void OnCarrierBankTransfer(object? sender, CarrierBankTransferEvent.CarrierBankTransferEventArgs e)
        {
            lock (_sync)
            {
                if (!EventMatchesPersonalCarrier(e.CarrierID))
                {
                    return;
                }

                _personalCarrier!.Balance = e.CarrierBalance;
                _personalCarrier.LastUpdatedUtc = DateTime.UtcNow;
                RaiseSnapshot(_personalCarrier);
            }
        }

        private void OnCarrierFuelDeposited(object? sender, CarrierDepositFuelEvent.CarrierDepositFuelEventArgs e)
        {
            lock (_sync)
            {
                if (!EventMatchesPersonalCarrier(e.CarrierID))
                {
                    return;
                }

                _personalCarrier!.FuelLevel = e.Total;
                _personalCarrier.LastUpdatedUtc = DateTime.UtcNow;
                RaiseSnapshot(_personalCarrier);
            }
        }

        private void OnCargoTransfer(object? sender, CargoTransferEvent.CargoTransferEventArgs e)
        {
            lock (_sync)
            {
                if (_personalCarrier == null || _personalCarrier.CarrierId == 0 || _dockedCarrierMarketId != _personalCarrier.CarrierId || e.Transfers == null)
                {
                    return;
                }

                var updated = false;
                foreach (var transfer in e.Transfers)
                {
                    var delta = transfer.Direction?.Equals("ToCarrier", StringComparison.OrdinalIgnoreCase) == true
                        ? transfer.Count
                        : transfer.Direction?.Equals("ToShip", StringComparison.OrdinalIgnoreCase) == true
                            ? -transfer.Count
                            : 0;

                    if (delta == 0)
                    {
                        continue;
                    }

                    var localized = string.IsNullOrWhiteSpace(transfer.Type_Localised) ? transfer.Type : transfer.Type_Localised;
                    var commodity = _personalCarrier.GetOrAddCommodity(transfer.Type, localized, transfer.Stolen ?? false);
                    commodity.StockCount = Math.Max(0, commodity.StockCount + delta);
                    _personalCarrier.RemoveIfEmpty(commodity);
                    updated = true;
                }

                if (updated)
                {
                    _personalCarrier.LastUpdatedUtc = DateTime.UtcNow;
                    RaiseSnapshot(_personalCarrier);
                }
            }
        }

        private void OnMarketSell(object? sender, MarketSellEvent.MarketSellEventArgs e)
        {
            lock (_sync)
            {
                if (_personalCarrier == null || _personalCarrier.CarrierId == 0 || (ulong)e.MarketID != _personalCarrier.CarrierId || e.BlackMarket)
                {
                    return;
                }

                var localized = string.IsNullOrWhiteSpace(e.Type_Localised) ? e.Type : e.Type_Localised;
                var commodity = _personalCarrier.GetOrAddCommodity(e.Type, localized, stolen: e.StolenGoods);
                commodity.StockCount = Math.Max(0, commodity.StockCount + e.Count);
                if (commodity.OutstandingPurchaseOrders > 0)
                {
                    commodity.OutstandingPurchaseOrders = Math.Max(0, commodity.OutstandingPurchaseOrders - e.Count);
                }
                _personalCarrier.LastUpdatedUtc = DateTime.UtcNow;
                RaiseSnapshot(_personalCarrier);
            }
        }

        private void OnDocked(object? sender, DockedEventArgs e)
        {
            if (e.DockedEvent.StationType.Equals("FleetCarrier", StringComparison.OrdinalIgnoreCase) &&
                e.DockedEvent.MarketId.HasValue)
            {
                _dockedCarrierMarketId = e.DockedEvent.MarketId.Value;
                TriggerMarketRefresh();
            }
            else
            {
                _dockedCarrierMarketId = null;
            }
        }

        private void OnUndocked(object? sender, UndockedEventArgs e)
        {
            _dockedCarrierMarketId = null;
        }

        private FleetCarrierState EnsureCarrierState(string? carrierTypeHint, ulong carrierId)
        {
            var match = ResolveCarrierById(carrierId);
            if (match != null)
            {
                if (!string.IsNullOrWhiteSpace(carrierTypeHint))
                {
                    match.CarrierType = carrierTypeHint!;
                }
                return match;
            }

            var isSquadron = string.Equals(carrierTypeHint, "SquadronCarrier", StringComparison.OrdinalIgnoreCase);

            if (isSquadron)
            {
                _squadronCarrier ??= new FleetCarrierState { CarrierType = "SquadronCarrier" };
                match = _squadronCarrier;
            }
            else
            {
                _personalCarrier ??= new FleetCarrierState();
                match = _personalCarrier;
            }

            if (carrierId != 0)
            {
                match.CarrierId = carrierId;
            }

            if (!string.IsNullOrWhiteSpace(carrierTypeHint))
            {
                match.CarrierType = carrierTypeHint!;
            }

            return match;
        }

        private FleetCarrierState? ResolveCarrierById(ulong carrierId)
        {
            if (carrierId == 0)
            {
                return null;
            }

            if (_personalCarrier != null && _personalCarrier.CarrierId == carrierId)
            {
                return _personalCarrier;
            }

            if (_squadronCarrier != null && _squadronCarrier.CarrierId == carrierId)
            {
                return _squadronCarrier;
            }

            return null;
        }

        private bool EventMatchesPersonalCarrier(ulong carrierId) =>
            _personalCarrier != null && _personalCarrier.CarrierId == carrierId && carrierId != 0;

        private static bool IsSquadron(string? carrierType) =>
            string.Equals(carrierType, "SquadronCarrier", StringComparison.OrdinalIgnoreCase);

        private void RaiseSnapshot(FleetCarrierState? state)
        {
            if (state == null)
            {
                return;
            }

            var snapshot = state.Clone();
            if (ReferenceEquals(state, _personalCarrier))
            {
                PersonalCarrierUpdated?.Invoke(this, snapshot);
            }
            else if (ReferenceEquals(state, _squadronCarrier))
            {
                SquadronCarrierUpdated?.Invoke(this, snapshot);
            }
        }

        private void ApplyCarrierStats(CarrierStatsEvent.CarrierStatsEventArgs stats)
        {
            lock (_sync)
            {
                var state = EnsureCarrierState(stats.CarrierType, stats.CarrierID);
                state.ApplyStats(stats);
                RaiseSnapshot(state);
            }
        }

        private void ApplyCarrierLocation(CarrierLocationEvent.CarrierLocationEventArgs location)
        {
            lock (_sync)
            {
                var state = EnsureCarrierState(location.CarrierType, location.CarrierID);
                state.ApplyLocation(location);
                RaiseSnapshot(state);
            }
        }

        private void InitializeFromLatestJournalSafe()
        {
            try
            {
                InitializeFromLatestJournal();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[FleetCarrierTrackerService] Failed to initialize from journal: {ex.Message}");
            }
        }

        private void InitializeFromLatestJournal()
        {
            var directory = _journalWatcher.JournalDirectoryPath;
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var journalFiles = Directory.EnumerateFiles(directory, "Journal*.log", SearchOption.TopDirectoryOnly)
                                        .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                                        .ToList();

            if (journalFiles.Count == 0)
            {
                return;
            }

            bool needPersonalStats = _personalCarrier?.CarrierId > 0 ? false : true;
            bool needPersonalLocation = string.IsNullOrWhiteSpace(_personalCarrier?.StarSystem);
            bool needSquadStats = _squadronCarrier?.CarrierId > 0 ? false : true;
            bool needSquadLocation = string.IsNullOrWhiteSpace(_squadronCarrier?.StarSystem);

            foreach (var file in journalFiles)
            {
                LoadCarrierEventsFromFile(file,
                                          ref needPersonalStats,
                                          ref needPersonalLocation,
                                          ref needSquadStats,
                                          ref needSquadLocation);

                if (!needPersonalStats && !needPersonalLocation && !needSquadStats && !needSquadLocation)
                {
                    break;
                }
            }
        }

        private void SetupMarketWatcher()
        {
            var directory = _journalWatcher.JournalDirectoryPath;
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            _marketWatcher = new FileSystemWatcher(directory, "Market.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _marketWatcher.Changed += OnMarketFileChanged;
            _marketWatcher.Created += OnMarketFileChanged;
            _marketWatcher.Renamed += OnMarketFileChanged;
        }

        private void OnMarketFileChanged(object sender, FileSystemEventArgs e)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100); // allow file write to finish
                TryReadMarketFile(e.FullPath);
            });
        }

        private void TryReadMarketFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var market = JsonSerializer.Deserialize<MarketSnapshot.Market>(json, _jsonOptions);
                        if (market != null)
                        {
                            ApplyMarketSnapshot(market);
                        }
                        return;
                    }
                    catch (IOException)
                    {
                        Task.Delay(50).Wait();
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void ApplyMarketSnapshot(MarketSnapshot.Market market)
        {
            if (!string.Equals(market.StationType, "FleetCarrier", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lock (_sync)
            {
                FleetCarrierState? target = null;

                if (_personalCarrier != null && _personalCarrier.CarrierId == market.MarketID)
                {
                    target = _personalCarrier;
                }
                else if (_squadronCarrier != null && _squadronCarrier.CarrierId == market.MarketID)
                {
                    target = _squadronCarrier;
                }

                if (target == null)
                {
                    return;
                }

                var stock = target.Stock;

                var commodityMap = stock.ToDictionary(c => c.CommodityName, StringComparer.OrdinalIgnoreCase);

                foreach (var item in market.Items)
                {
                    var commodity = target.GetOrAddCommodity(item.Name, item.NameLocalised, stolen: false);
                    commodity.StockCount = item.Stock;
                    commodity.SalePrice = item.BuyPrice;
                    commodity.OutstandingPurchaseOrders = item.Demand;
                    commodity.Rare = item.Rare;
                    commodity.BlackMarket = false;
                }

                target.LastUpdatedUtc = market.Timestamp;
                RaiseSnapshot(target);
            }
        }

        private void TriggerMarketRefresh()
        {
            var directory = _journalWatcher.JournalDirectoryPath;
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var path = Path.Combine(directory, "Market.json");
            if (!File.Exists(path))
            {
                return;
            }

            Task.Run(() => TryReadMarketFile(path));
        }

        private void LoadCarrierEventsFromFile(string filePath,
                                               ref bool needPersonalStats,
                                               ref bool needPersonalLocation,
                                               ref bool needSquadStats,
                                               ref bool needSquadLocation)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch
            {
                return;
            }

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (!needPersonalStats && !needPersonalLocation && !needSquadStats && !needSquadLocation)
                {
                    break;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(line);
                    if (!document.RootElement.TryGetProperty("event", out var evtProperty))
                    {
                        continue;
                    }

                    var evtType = evtProperty.GetString();
                    switch (evtType)
                    {
                        case "CarrierStats":
                            {
                                var stats = JsonSerializer.Deserialize<CarrierStatsEvent.CarrierStatsEventArgs>(line, _jsonOptions);
                                if (stats == null) break;
                                ApplyCarrierStats(stats);
                                if (IsSquadron(stats.CarrierType))
                                {
                                    needSquadStats = false;
                                }
                                else
                                {
                                    needPersonalStats = false;
                                }
                                break;
                            }
                        case "CarrierLocation":
                            {
                                var location = JsonSerializer.Deserialize<CarrierLocationEvent.CarrierLocationEventArgs>(line, _jsonOptions);
                                if (location == null) break;
                                ApplyCarrierLocation(location);
                                if (IsSquadron(location.CarrierType))
                                {
                                    needSquadLocation = false;
                                }
                                else
                                {
                                    needPersonalLocation = false;
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }
                catch
                {
                    // Ignore malformed lines
                }
            }
        }

        public void Dispose()
        {
            _journalWatcher.CarrierStats -= OnCarrierStats;
            _journalWatcher.CarrierLocation -= OnCarrierLocation;
            _journalWatcher.CarrierJumpRequested -= OnCarrierJumpRequested;
            _journalWatcher.CarrierJumpCancelled -= OnCarrierJumpCancelled;
            _journalWatcher.CarrierTradeOrder -= OnCarrierTradeOrder;
            _journalWatcher.CarrierCrewServices -= OnCarrierCrewServices;
            _journalWatcher.CarrierBankTransfer -= OnCarrierBankTransfer;
            _journalWatcher.CarrierFuelDeposited -= OnCarrierFuelDeposited;
            _journalWatcher.CargoTransfer -= OnCargoTransfer;
            _journalWatcher.MarketSell -= OnMarketSell;
            _journalWatcher.Docked -= OnDocked;
            _journalWatcher.Undocked -= OnUndocked;
            _marketWatcher?.Dispose();
        }
    }
}
