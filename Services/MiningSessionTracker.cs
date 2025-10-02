using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EliteDataRelay.Services

{
    /// <summary>
    /// Tracks statistics for a single mining session, such as refined commodities and limpets used.
    /// </summary>
    public class MiningSessionTracker
    {
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;

        // Mining-specific tracking state
        private readonly Dictionary<string, int> _refinedCommodities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _pendingRefined = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private long _miningProfit;
        private int _limpetsUsed;
        private int _lastLimpetCount = -1; // -1 indicates not yet initialized for the session.
        private DateTime? _miningStartTime;
        private DateTime? _miningStopTime;

        public bool IsActive { get; private set; }
        public long MiningProfit => _miningProfit;
        public int LimpetsUsed => _limpetsUsed;
        public IReadOnlyDictionary<string, int> RefinedCommodities => _refinedCommodities;

        public TimeSpan MiningDuration
        {
            get
            {
                if (!_miningStartTime.HasValue) return TimeSpan.Zero;
                var endTime = IsActive ? DateTime.UtcNow : (_miningStopTime ?? _miningStartTime.Value);
                return endTime - _miningStartTime.Value;
            }
        }

        public event EventHandler? Updated;

        public MiningSessionTracker(ICargoProcessorService cargoProcessorService, IJournalWatcherService journalWatcherService)
        {
            _cargoProcessorService = cargoProcessorService;
            _journalWatcherService = journalWatcherService;
        }

        public void Start()
        {
            if (IsActive) return;

            // Reset mining stats
            _miningProfit = 0;
            _limpetsUsed = 0;
            _miningStartTime = DateTime.UtcNow;
            _refinedCommodities.Clear();
            _miningStopTime = null;
            _lastLimpetCount = -1;
            _pendingRefined.Clear();

            IsActive = true;

            _journalWatcherService.MiningRefined += OnMiningRefined;
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.MarketSell += OnMarketSell;
            _journalWatcherService.BuyDrones += OnBuyDrones;
            Updated?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (!IsActive) return;

            IsActive = false;
            _miningStopTime = DateTime.UtcNow;
            _journalWatcherService.MiningRefined -= OnMiningRefined;
            _cargoProcessorService.CargoProcessed -= OnCargoProcessed;
            _journalWatcherService.MarketSell -= OnMarketSell;
            _journalWatcherService.BuyDrones -= OnBuyDrones;
            Updated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMiningRefined(object? sender, MiningRefinedEventArgs e)
        {
            var commodity = e.CommodityType.ToLowerInvariant();
            _pendingRefined[commodity] = _pendingRefined.GetValueOrDefault(commodity) + 1;
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            bool wasUpdated = false;

            var currentLimpetCount = e.Snapshot.Inventory.FirstOrDefault(i => i.Name.Equals("drones", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
            if (_lastLimpetCount != -1 && currentLimpetCount < _lastLimpetCount)
            {
                _limpetsUsed += _lastLimpetCount - currentLimpetCount;
                wasUpdated = true;
            }
            _lastLimpetCount = currentLimpetCount;

            if (_pendingRefined.Any())
            {
                foreach (var item in e.Snapshot.Inventory)
                {
                    var commodityName = item.Name.ToLowerInvariant();
                    if (_pendingRefined.TryGetValue(commodityName, out int pendingCount) && pendingCount > 0)
                    {
                        _pendingRefined[commodityName]--;
                        if (_pendingRefined[commodityName] == 0)
                        {
                            _pendingRefined.Remove(commodityName);
                        }
                        _refinedCommodities[commodityName] = _refinedCommodities.GetValueOrDefault(commodityName) + 1;
                        wasUpdated = true;
                    }
                }
            }

            if (wasUpdated) Updated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMarketSell(object? sender, MarketSellEventArgs e)
        {
            var commodity = e.Commodity.ToLowerInvariant();
            if (_refinedCommodities.TryGetValue(commodity, out int refinedCount))
            {
                int soldFromSession = Math.Min(e.Count, refinedCount);
                long pricePerUnit = e.TotalSale / e.Count;
                _miningProfit += pricePerUnit * soldFromSession;

                _refinedCommodities[commodity] -= soldFromSession;
                if (_refinedCommodities[commodity] <= 0)
                {
                    _refinedCommodities.Remove(commodity);
                }
                Updated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnBuyDrones(object? sender, BuyDronesEventArgs e) { }
    }
}