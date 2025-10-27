using EliteDataRelay.Models;
using System;
using System.Linq;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Provides mining convenience helpers, like restock limpet reminders upon docking.
    /// </summary>
    public class MiningCompanionService : IDisposable
    {
        private readonly IJournalWatcherService _journal;
        private readonly SessionTrackingService _session;
        private bool _started;

        public event EventHandler<string>? RestockReminder;

        public MiningCompanionService(IJournalWatcherService journal, SessionTrackingService session)
        {
            _journal = journal;
            _session = session;
        }

        public void Start()
        {
            if (_started) return;
            _journal.Docked += OnDocked;
            _started = true;
        }

        public void Stop()
        {
            if (!_started) return;
            _journal.Docked -= OnDocked;
            _started = false;
        }

        private void OnDocked(object? sender, DockedEventArgs e)
        {
            // If player used limpets this session and station can rearm or outfitting, remind to restock
            var services = e.DockedEvent.StationServices.Select(s => s.ToLowerInvariant()).ToHashSet();
            bool canRestock = services.Contains("rearm") || services.Contains("outfitting") || services.Contains("contacts");
            if (canRestock && _session.IsMiningSessionActive && _session.LimpetsUsed > 0)
            {
                RestockReminder?.Invoke(this, $"Docked at {e.DockedEvent.StationName}: consider restocking limpets.");
            }
        }

        public void Dispose() => Stop();
    }
}

