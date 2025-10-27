using EliteDataRelay.Models;
using System;
using System.Linq;

namespace EliteDataRelay.Services
{
    public class StationInfoService : IStationInfoService
    {
        private readonly IJournalWatcherService _journalWatcher;
        private bool _isStarted;
        private StationInfoData? _lastStationInfo;

        public event EventHandler<StationInfoData>? StationInfoUpdated;

        public StationInfoService(IJournalWatcherService journalWatcher)
        {
            _journalWatcher = journalWatcher ?? throw new ArgumentNullException(nameof(journalWatcher));
        }

        public void Start()
        {
            if (_isStarted) return;
            _journalWatcher.Docked += OnDocked;
            _journalWatcher.Undocked += OnUndocked;
            _isStarted = true;

            // On startup, proactively get the last known docked state from the journal watcher.
            // This populates the overlay immediately if the player is already docked.
            var lastDockedState = _journalWatcher.GetLastKnownDockedState();
            if (lastDockedState != null)
            {
                OnDocked(this, lastDockedState);
            }
            else
            {
                // If we are not docked, explicitly send an "Undocked" event to ensure the overlay is hidden.
                OnUndocked(this, new UndockedEventArgs("Startup"));
            }
        }

        public void Stop()
        {
            if (!_isStarted) return;
            _journalWatcher.Docked -= OnDocked;
            _journalWatcher.Undocked -= OnUndocked;
            _isStarted = false;
        }

        private void OnDocked(object? sender, DockedEventArgs e)
        {
            var stationInfo = CreateStationInfoData(e.DockedEvent);
            _lastStationInfo = stationInfo;
            StationInfoUpdated?.Invoke(this, stationInfo);
        }

        private void OnUndocked(object? sender, UndockedEventArgs e)
        {
            // When undocked, send an empty data object to clear the overlay.
            var undockedInfo = new StationInfoData { StationName = "Undocked" };
            _lastStationInfo = undockedInfo;
            StationInfoUpdated?.Invoke(this, undockedInfo);
        }

        private StationInfoData CreateStationInfoData(DockedEvent dockedEvent)
        {
            var services = dockedEvent.StationServices.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // When docked at a fleet carrier, the StationName is the ID (e.g., "K2K-12K")
            // and the actual carrier name is in the StationFaction.Name property.
            // The custom name is in the top-level 'Name' property for carriers.
            string stationDisplayName = dockedEvent.StationName;
            if (dockedEvent.StationType == "FleetCarrier" && !string.IsNullOrEmpty(dockedEvent.Name))
            {
                // Format as "Carrier Name (CARRIER-ID)"
                stationDisplayName = $"{dockedEvent.Name} ({dockedEvent.StationName})";
            }

            return new StationInfoData
            {
                StationName = stationDisplayName,
                StationType = dockedEvent.StationType,
                Allegiance = dockedEvent.StationAllegiance ?? "N/A",
                Economy = dockedEvent.StationEconomyLocalised ?? dockedEvent.StationEconomy ?? "N/A",
                Government = dockedEvent.StationGovernmentLocalised ?? dockedEvent.StationGovernment ?? "N/A",
                ControllingFaction = dockedEvent.StationFaction?.Name ?? "N/A",
                HasRefuel = services.Contains("refuel"),
                HasRepair = services.Contains("repair"),
                // Journal uses 'Restock' for ammo/limpets; accept both for safety
                HasRearm = services.Contains("rearm") || services.Contains("restock"),
                HasOutfitting = services.Contains("outfitting"),
                HasShipyard = services.Contains("shipyard"),
                HasMarket = services.Contains("market")
            };
        }

        public void Dispose()
        {
            Stop();
        }

        public StationInfoData? GetLastStationInfo()
        {
            return _lastStationInfo;
        }
    }
}
