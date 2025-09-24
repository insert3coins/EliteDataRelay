using System;
using EliteDataRelay.Models;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Defines the contract for a service that monitors the Elite Dangerous journal files.
    /// </summary>
    public interface IJournalWatcherService : IDisposable
    {
        event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;
        event EventHandler<CargoInventoryEventArgs>? CargoInventoryChanged;
        event EventHandler<BalanceChangedEventArgs>? BalanceChanged;
        event EventHandler<LocationChangedEventArgs>? LocationChanged;
        event EventHandler<CommanderNameChangedEventArgs>? CommanderNameChanged;
        event EventHandler<LoadoutChangedEventArgs>? LoadoutChanged;
        event EventHandler<StatusChangedEventArgs>? StatusChanged;
        event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;
        event EventHandler<MaterialsEventArgs>? MaterialsEvent;
        event EventHandler<MaterialCollectedEventArgs>? MaterialCollectedEvent;
        event EventHandler<MaterialCollectedEventArgs>? MaterialDiscardedEvent;
        event EventHandler<MaterialTradeEventArgs>? MaterialTradeEvent;
        event EventHandler<EngineerCraftEventArgs>? EngineerCraftEvent;

        bool IsMonitoring { get; }
        string JournalDirectoryPath { get; }

        void StartMonitoring();
        void StopMonitoring();
        void Reset();
        LocationChangedEventArgs? GetLastKnownLocation();
    }
}