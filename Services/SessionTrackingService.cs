using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Tracks statistics for a single play session, such as credits earned and cargo collected.
    /// </summary>
    public class SessionTrackingService : IDisposable
    {
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;

        private long _startingBalance;
        private long _currentBalance;
        private int _startingCargoCount;
        private DateTime? _sessionStartTime;

        // Mining-specific tracking
        private readonly Dictionary<string, int> _refinedCommodities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _pendingRefined = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private long _miningProfit;
        private int _limpetsUsed;
        private int _lastLimpetCount = -1; // Use -1 to indicate it's not yet initialized for the session.
        private DateTime? _miningStartTime;
        private DateTime? _miningStopTime;

        public bool IsSessionActive { get; private set; }
        public bool IsMiningSessionActive { get; private set; }

        /// <summary>
        /// The net credits earned or lost during the current session.
        /// </summary>
        public long CreditsEarned => IsSessionActive ? _currentBalance - _startingBalance : 0;

        /// <summary>
        /// The total amount of cargo units collected during the session.
        /// </summary>
        public int TotalCargoCollected { get; private set; }

        /// <summary>
        /// The duration of the current active session.
        /// </summary>
        public TimeSpan SessionDuration => IsSessionActive && _sessionStartTime.HasValue ? DateTime.UtcNow - _sessionStartTime.Value : TimeSpan.Zero;

        #region Mining Properties
        /// <summary>
        /// The total profit earned from selling commodities refined during this session.
        /// </summary>
        public long MiningProfit => _miningProfit;

        /// <summary>
        /// The number of collector and prospector limpets used during the session.
        /// </summary>
        public int LimpetsUsed => _limpetsUsed;

        /// <summary>
        /// The duration of active mining (from first refinement to last).
        /// </summary>
        public TimeSpan MiningDuration
        {
            get
            {
                if (!_miningStartTime.HasValue) return TimeSpan.Zero;
                var endTime = IsMiningSessionActive ? DateTime.UtcNow : (_miningStopTime ?? _miningStartTime.Value);
                return endTime - _miningStartTime.Value;
            }
        }

        /// <summary>
        /// A dictionary of commodities refined during this session and their quantities.
        /// </summary>
        public IReadOnlyDictionary<string, int> RefinedCommodities => _refinedCommodities;

        /// <summary>
        /// The total number of tons of all commodities refined during this session.
        /// </summary>
        public int TotalRefinedCount => _refinedCommodities.Values.Sum();
        #endregion

        /// <summary>
        /// Fires whenever session data (like credits or cargo) is updated.
        /// </summary>
        public event EventHandler? SessionUpdated;

        public SessionTrackingService(ICargoProcessorService cargoProcessorService, IJournalWatcherService journalWatcherService)
        {
            _cargoProcessorService = cargoProcessorService;
            _journalWatcherService = journalWatcherService;
        }

        public void StartSession(long initialBalance, int initialCargoCount)
        {
            if (IsSessionActive) return;

            _startingBalance = initialBalance;
            _currentBalance = initialBalance;
            _startingCargoCount = initialCargoCount;
            TotalCargoCollected = 0;
            _sessionStartTime = DateTime.UtcNow;

            IsSessionActive = true;

            // Subscribe to events now that the session is active
            _journalWatcherService.CargoCollected += OnCargoCollected;

            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopSession()
        {
            if (!IsSessionActive) return;

            IsSessionActive = false;
            _sessionStartTime = null;

            // Unsubscribe from events
            _journalWatcherService.CargoCollected -= OnCargoCollected;

            // Also ensure the mining session is stopped.
            if (IsMiningSessionActive)
            {
                StopMiningSession();
            }
        }

        public void StartMiningSession()
        {
            if (!IsSessionActive || IsMiningSessionActive) return;

            // Reset mining stats
            _miningProfit = 0;
            _limpetsUsed = 0;
            _miningStartTime = DateTime.UtcNow;
            _refinedCommodities.Clear();
            _miningStopTime = null;
            _lastLimpetCount = -1;
            _pendingRefined.Clear();

            IsMiningSessionActive = true;

            _journalWatcherService.MiningRefined += OnMiningRefined;
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.MarketSell += OnMarketSell;
            _journalWatcherService.BuyDrones += OnBuyDrones;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopMiningSession()
        {
            IsMiningSessionActive = false;
            _miningStopTime = DateTime.UtcNow;
            _journalWatcherService.MiningRefined -= OnMiningRefined;
            _cargoProcessorService.CargoProcessed -= OnCargoProcessed;
            _journalWatcherService.MarketSell -= OnMarketSell;
            _journalWatcherService.BuyDrones -= OnBuyDrones;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the session's current credit balance.
        /// </summary>
        /// <param name="newBalance">The new total credit balance.</param>
        public void UpdateBalance(long newBalance)
        {
            if (!IsSessionActive) return;

            _currentBalance = newBalance;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnCargoCollected(object? sender, CargoCollectedEventArgs e)
        {
            TotalCargoCollected += e.Quantity;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMiningRefined(object? sender, MiningRefinedEventArgs e)
        {
            if (!IsMiningSessionActive) return;

            var commodity = e.CommodityType.ToLowerInvariant();
            // Add to a pending list. It will be confirmed once it appears in Cargo.json
            if (_pendingRefined.ContainsKey(commodity))
            {
                _pendingRefined[commodity]++;
            }
            else
            {
                _pendingRefined[commodity] = 1;
            }
            // We don't fire SessionUpdated here, we wait for confirmation from the cargo hold.
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            if (!IsMiningSessionActive) return;

            bool sessionWasUpdated = false;
            
            // --- New Limpet Tracking Logic ---
            // Find the current number of limpets (drones) in the cargo hold.
            var currentLimpetCount = e.Snapshot.Inventory.FirstOrDefault(i => i.Name.Equals("drones", StringComparison.OrdinalIgnoreCase))?.Count ?? 0;

            if (_lastLimpetCount == -1)
            {
                // This is the first cargo update for this session, establish the baseline.
                _lastLimpetCount = currentLimpetCount;
            }
            else if (currentLimpetCount < _lastLimpetCount)
            {
                // The number of limpets has decreased, so we must have used some.
                _limpetsUsed += _lastLimpetCount - currentLimpetCount;
                sessionWasUpdated = true;
            }
            // Update the last known count for the next comparison.
            _lastLimpetCount = currentLimpetCount;
            // --- End of New Limpet Tracking Logic ---

            // --- Pending Refined Commodity Logic ---
            if (_pendingRefined.Any())
            {
                foreach (var item in e.Snapshot.Inventory)
                {
                    var commodityName = item.Name.ToLowerInvariant();
                    if (_pendingRefined.TryGetValue(commodityName, out int pendingCount) && pendingCount > 0)
                    {
                        // This commodity was pending refinement and is now in cargo.
                        // Move it from pending to confirmed.
                        _pendingRefined[commodityName]--;
                        if (_pendingRefined[commodityName] == 0)
                        {
                            _pendingRefined.Remove(commodityName);
                        }

                        _refinedCommodities[commodityName] = _refinedCommodities.GetValueOrDefault(commodityName) + 1;
                        sessionWasUpdated = true;
                    }
                }
            }

            if (sessionWasUpdated) SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnMarketSell(object? sender, MarketSellEventArgs e)
        {
            if (!IsMiningSessionActive) return;

            var commodity = e.Commodity.ToLowerInvariant();
            // Check if the commodity sold was one we refined in this session
            if (_refinedCommodities.TryGetValue(commodity, out int refinedCount))
            {
                // Determine how many of the sold items were actually from this session.
                int soldFromSession = Math.Min(e.Count, refinedCount);

                // Calculate the profit for only the items sold from this session.
                long pricePerUnit = e.TotalSale / e.Count;
                _miningProfit += pricePerUnit * soldFromSession;
                
                // Decrement the count of refined commodities. If we've sold them all, remove the key.
                if (refinedCount <= soldFromSession)
                {
                    _refinedCommodities.Remove(commodity);
                }
                else
                {
                    _refinedCommodities[commodity] = refinedCount - soldFromSession;
                }
                SessionUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnBuyDrones(object? sender, BuyDronesEventArgs e)
        {
            if (!IsMiningSessionActive) return;
        }

        public void Dispose() => StopSession();
    }
}