using EliteDataRelay.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        private readonly ConcurrentDictionary<string, int> _refinedCommodities = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, int> _collectedCommodities = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public long TotalCargoCollected { get; private set; }
        public long CreditsEarned { get; private set; }
        public bool IsMiningSessionActive { get; private set; }
        public long MiningProfit { get; private set; } // Placeholder for now
        public int LimpetsUsed { get; private set; }
        public IReadOnlyDictionary<string, int> CollectedCommodities => _collectedCommodities;
        public TimeSpan MiningDuration => _miningStartTime.HasValue ? DateTime.UtcNow - _miningStartTime.Value : TimeSpan.Zero;
        public IReadOnlyDictionary<string, int> RefinedCommodities => _refinedCommodities;
        public int TotalRefinedCount => _refinedCommodities.Values.Sum();
        public TimeSpan SessionDuration => _sessionStartTime.HasValue ? DateTime.UtcNow - _sessionStartTime.Value : TimeSpan.Zero;

        // Compatibility property for older controls if needed.
        public bool IsMining
        {
            get => IsMiningSessionActive;
        }
        public event EventHandler? SessionUpdated;

        public SessionTrackingService(ICargoProcessorService cargoProcessorService, IJournalWatcherService journalWatcherService)
        {
            _cargoProcessorService = cargoProcessorService;
            _journalWatcherService = journalWatcherService;

            // Subscribe to the cargo processor to get updates based on Cargo.json
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.LaunchDrone += OnLaunchDrone;
            _journalWatcherService.MiningRefined += OnMiningRefined;
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
            
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopSession()
        {
            _initialCargoSnapshot = null;
            _previousCargoSnapshot = null;
            _sessionStartTime = null;
            StopMiningSession(); // Ensure mining session also stops
            SessionUpdated?.Invoke(this, EventArgs.Empty);
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
            // We only care about this if a session is active.
            if (_previousCargoSnapshot == null) return;

            var currentSnapshot = e.Snapshot;
            var previousSnapshot = _previousCargoSnapshot;

            // Create a dictionary of the previous cargo state for quick lookups.
            var previousCargoDict = previousSnapshot.Items.ToDictionary(c => c.Name, c => c.Count);

            // Iterate through the current cargo and find any items that have increased in quantity.
            foreach (var currentItem in currentSnapshot.Items)
            {
                previousCargoDict.TryGetValue(currentItem.Name, out var previousCount);

                // Calculate the difference since the last snapshot.
                var diff = currentItem.Count - previousCount;

                // If the difference is positive, it means we collected new items.
                if (diff > 0)
                {
                    // Drones (limpets) are consumables, not collected cargo for profit. Exclude them.
                    // The internal name for limpets in Cargo.json is "drones".
                    if (string.Equals(currentItem.Name, "drones", StringComparison.OrdinalIgnoreCase)) continue;

                    // Add the newly collected amount to our running totals.
                    TotalCargoCollected += diff;
                    _collectedCommodities.AddOrUpdate(currentItem.Name, diff, (key, existingCount) => existingCount + diff);
                }
            }

            // Update the previous snapshot to the current one for the next comparison.
            _previousCargoSnapshot = currentSnapshot;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StartMiningSession()
        {
            if (IsMiningSessionActive) return;
            IsMiningSessionActive = true;
            _miningStartTime = DateTime.UtcNow;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopMiningSession()
        {
            if (!IsMiningSessionActive) return;
            IsMiningSessionActive = false;
            _miningStartTime = null;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnLaunchDrone(object? sender, LaunchDroneEventArgs e)
        {
            // Only track limpets used during an active mining session
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


        public void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            _cargoProcessorService.CargoProcessed -= OnCargoProcessed;
            _journalWatcherService.LaunchDrone -= OnLaunchDrone;
            _journalWatcherService.MiningRefined -= OnMiningRefined;
            GC.SuppressFinalize(this);
        }


    }
}