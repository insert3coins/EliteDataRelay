using System;
using System.Windows.Forms;
using System.Linq;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public class SessionTrackingService : IDisposable
    {
        private readonly ICargoProcessorService _cargoProcessor;
        private readonly IStatusWatcherService _statusWatcher;
        private readonly System.Windows.Forms.Timer _timer;

        private long _startingBalance;
        private CargoSnapshot? _previousSnapshot;
        private DateTime _sessionStartTime;
        private bool _sessionActive;

        public long CreditsEarned { get; private set; }
        public long TotalCargoCollected { get; private set; }
        public TimeSpan SessionDuration { get; private set; }

        public event EventHandler? SessionUpdated;

        public SessionTrackingService(ICargoProcessorService cargoProcessor, IStatusWatcherService statusWatcher)
        {
            _cargoProcessor = cargoProcessor ?? throw new ArgumentNullException(nameof(cargoProcessor));
            _statusWatcher = statusWatcher ?? throw new ArgumentNullException(nameof(statusWatcher));
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += OnTimerTick;
        }

        public void StartSession()
        {
            if (_sessionActive) return;

            // Reset stats
            CreditsEarned = 0;
            TotalCargoCollected = 0;
            SessionDuration = TimeSpan.Zero;
            _startingBalance = -1;
            _previousSnapshot = null;
            _sessionStartTime = DateTime.UtcNow;
            _sessionActive = true;

            // Subscribe to events
            _cargoProcessor.CargoProcessed += OnCargoProcessed;
            _statusWatcher.BalanceChanged += OnBalanceChanged;
            _timer.Start();

            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopSession()
        {
            if (!_sessionActive) return;

            // Unsubscribe to prevent memory leaks
            _cargoProcessor.CargoProcessed -= OnCargoProcessed;
            _statusWatcher.BalanceChanged -= OnBalanceChanged;
            _timer.Stop();
            _sessionActive = false;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            SessionDuration = DateTime.UtcNow - _sessionStartTime;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            if (_startingBalance == -1)
            {
                _startingBalance = e.Balance;
            }

            CreditsEarned = e.Balance - _startingBalance;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            var newSnapshot = e.Snapshot;

            if (_previousSnapshot != null)
            {
                // Create dictionaries for quick lookups. Using OrdinalIgnoreCase for safety.
                var newInventory = newSnapshot.Inventory.ToDictionary(i => i.Name, i => i.Count, StringComparer.OrdinalIgnoreCase);
                var oldInventory = _previousSnapshot.Inventory.ToDictionary(i => i.Name, i => i.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var item in newInventory)
                {
                    // The internal name for limpets is 'drones'.
                    if (item.Key.Equals("drones", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip limpets, do not count them in session cargo.
                    }

                    oldInventory.TryGetValue(item.Key, out int oldCount);
                    long newCount = item.Value;

                    if (newCount > oldCount)
                    {
                        TotalCargoCollected += (newCount - oldCount);
                    }
                }
            }

            _previousSnapshot = newSnapshot;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            StopSession();
            _timer.Dispose();
        }
    }
}