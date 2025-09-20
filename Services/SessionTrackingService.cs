using System;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public class SessionTrackingService : IDisposable
    {
        private readonly ICargoProcessorService _cargoProcessor;
        private readonly IStatusWatcherService _statusWatcher;
        private readonly System.Windows.Forms.Timer _timer;

        private long _startingBalance;
        private int _previousCargoCount;
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
            _previousCargoCount = -1;
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
            var currentCargoCount = e.Snapshot.Count;
            if (_previousCargoCount == -1)
            {
                _previousCargoCount = currentCargoCount;
            }

            if (currentCargoCount > _previousCargoCount)
            {
                TotalCargoCollected += (currentCargoCount - _previousCargoCount);
            }

            _previousCargoCount = currentCargoCount;
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            StopSession();
            _timer.Dispose();
        }
    }
}