using System;
using System.Linq;
using System.Timers;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public class SessionTrackingService : IDisposable
    {
        private readonly System.Timers.Timer _sessionTimer;
        private DateTime? _sessionStartTime;
        private CargoSnapshot? _previousCargoSnapshot;

        public long TotalCargoCollected { get; private set; }
        public TimeSpan SessionDuration => _sessionStartTime.HasValue ? DateTime.UtcNow - _sessionStartTime.Value : TimeSpan.Zero;
        public double CargoPerHour
        {
            get
            {
                var durationHours = SessionDuration.TotalHours;
                if (durationHours > 0)
                {
                    return TotalCargoCollected / durationHours;
                }
                return 0;
            }
        }

        public event EventHandler? SessionUpdated;

        public SessionTrackingService()
        {
            _sessionTimer = new System.Timers.Timer(1000); // Update duration every second
            _sessionTimer.Elapsed += OnTimerElapsed;
        }

        public void StartSession()
        {
            Reset();
            _sessionStartTime = DateTime.UtcNow;
            _sessionTimer.Start();
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void StopSession()
        {
            _sessionTimer.Stop();
            SessionUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void OnCargoChanged(CargoSnapshot newSnapshot)
        {
            if (_sessionStartTime == null) return; // Session not started

            if (_previousCargoSnapshot != null)
            {
                long collectedInThisChange = 0;

                // Create a dictionary for quick lookups of old inventory
                var oldInventoryDict = _previousCargoSnapshot.Inventory
                    .ToDictionary(i => i.Name, i => i.Count);

                foreach (var newItem in newSnapshot.Inventory)
                {
                    oldInventoryDict.TryGetValue(newItem.Name, out int oldCount);
                    int diff = newItem.Count - oldCount;

                    // Only count items that are not limpets (internal name: "drones")
                    if (diff > 0 && !newItem.Name.Equals("drones", StringComparison.OrdinalIgnoreCase))
                    {
                        collectedInThisChange += diff;
                    }
                }

                if (collectedInThisChange > 0)
                {
                    TotalCargoCollected += collectedInThisChange;
                    SessionUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

            _previousCargoSnapshot = newSnapshot;
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => SessionUpdated?.Invoke(this, EventArgs.Empty);

        private void Reset()
        {
            _sessionStartTime = null;
            _previousCargoSnapshot = null;
            TotalCargoCollected = 0;
            _sessionTimer.Stop();
        }

        public void Dispose() => _sessionTimer?.Dispose();
    }
}