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

        public bool IsSessionActive { get; private set; }

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

        public void Dispose() => StopSession();
    }
}