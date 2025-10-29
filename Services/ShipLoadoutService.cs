using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    public class ShipLoadoutService : IShipLoadoutService
    {
        private readonly IJournalWatcherService _journalWatcher;
        private bool _isStarted;

        public event EventHandler? ShipLoadoutUpdated;
        public ShipLoadout? CurrentLoadout { get; private set; }

        public ShipLoadoutService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
        }

        public void Start()
        {
            if (_isStarted) return;
            _journalWatcher.LoadoutChanged += OnLoadoutChanged;
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _journalWatcher.LoadoutChanged -= OnLoadoutChanged;
            _isStarted = false;
        }

        private void OnLoadoutChanged(object? sender, LoadoutChangedEventArgs e)
        {
            CurrentLoadout = e.Loadout;
            ShipLoadoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
