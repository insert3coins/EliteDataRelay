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
        event EventHandler<BalanceChangedEventArgs>? BalanceChanged;
        event EventHandler<LocationChangedEventArgs>? LocationChanged;
        event EventHandler<CommanderNameChangedEventArgs>? CommanderNameChanged;
        event EventHandler<LoadoutChangedEventArgs>? LoadoutChanged;
        event EventHandler<StatusChangedEventArgs>? StatusChanged;
        event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;
        event EventHandler<DockedEventArgs>? Docked;
        event EventHandler<UndockedEventArgs>? Undocked;
        event EventHandler? InitialScanComplete;

        bool IsMonitoring { get; }
        string JournalDirectoryPath { get; }

        void StartMonitoring();
        void StopMonitoring();
        void Reset();
        LocationChangedEventArgs? GetLastKnownLocation();
        DockedEventArgs? GetLastKnownDockedState();
    }
}