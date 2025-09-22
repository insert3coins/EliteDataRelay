using System;

namespace EliteDataRelay.Services
{
    public interface IJournalWatcherService : IDisposable
    {
        event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;
        event EventHandler<CargoInventoryEventArgs>? CargoInventoryChanged;
        event EventHandler<LocationChangedEventArgs>? LocationChanged;
        event EventHandler<CommanderNameChangedEventArgs>? CommanderNameChanged;
        event EventHandler<LoadoutChangedEventArgs>? LoadoutChanged;
        event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;

        // Material Events
        event EventHandler<MaterialsEventArgs>? MaterialsEvent;
        event EventHandler<MaterialCollectedEventArgs>? MaterialCollectedEvent;
        event EventHandler<MaterialCollectedEventArgs>? MaterialDiscardedEvent;
        event EventHandler<MaterialTradeEventArgs>? MaterialTradeEvent;
        event EventHandler<EngineerCraftEventArgs>? EngineerCraftEvent;
        event EventHandler<ScanEventArgs>? ScanEvent;
        event EventHandler<DockableBodyEventArgs>? DockableBodyFound;

        bool IsMonitoring { get; }
        string JournalDirectoryPath { get; }

        void StartMonitoring();
        void StopMonitoring();
    }
}