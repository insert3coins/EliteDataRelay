using EliteDataRelay.Models;
using System;

namespace EliteDataRelay.Services
{
    public interface IJournalWatcherService
    {
        string? JournalDirectoryPath { get; }
        event EventHandler<CargoCapacityEventArgs>? CargoCapacityChanged;
        event EventHandler<LocationChangedEventArgs>? LocationChanged;
        event EventHandler<BalanceChangedEventArgs>? BalanceChanged;
        event EventHandler<CommanderNameChangedEventArgs>? CommanderNameChanged;
        event EventHandler<ShipInfoChangedEventArgs>? ShipInfoChanged;
        event EventHandler<LoadoutChangedEventArgs>? LoadoutChanged;
        event EventHandler<StatusChangedEventArgs>? StatusChanged;
        event EventHandler InitialScanComplete;
        event EventHandler<CargoCollectedEventArgs>? CargoCollected;
        event EventHandler<DockedEventArgs>? Docked;
        event EventHandler<UndockedEventArgs>? Undocked;
        event EventHandler<MiningRefinedEventArgs>? MiningRefined;
        event EventHandler<LaunchDroneEventArgs>? LaunchDrone;
        event EventHandler<MarketSellEventArgs>? MarketSell;
        event EventHandler<BuyDronesEventArgs>? BuyDrones;

        void StartMonitoring();
        void StopMonitoring();
        void Reset();

        LocationChangedEventArgs? GetLastKnownLocation();
        DockedEventArgs? GetLastKnownDockedState();
    }
}