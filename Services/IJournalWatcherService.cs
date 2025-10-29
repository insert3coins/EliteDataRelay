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
        event EventHandler<MaterialsEvent>? MaterialsChanged;
        event EventHandler InitialScanComplete;
        event EventHandler<CargoCollectedEventArgs>? CargoCollected;
        event EventHandler<DockedEventArgs>? Docked;
        event EventHandler<UndockedEventArgs>? Undocked;
        event EventHandler<MiningRefinedEventArgs>? MiningRefined;
        event EventHandler<LaunchDroneEventArgs>? LaunchDrone;
        event EventHandler<BuyDronesEventArgs>? BuyDrones;
        event EventHandler<MarketBuyEventArgs>? MarketBuy;
        event EventHandler<FSSDiscoveryScanEvent>? FSSDiscoveryScan;
        event EventHandler<ScanEvent>? BodyScanned;
        event EventHandler<SAAScanCompleteEvent>? SAAScanComplete;
        event EventHandler<FSSBodySignalsEvent>? FSSBodySignals;
        event EventHandler<SAASignalsFoundEvent>? SAASignalsFound;
        event EventHandler<SellExplorationDataEvent>? SellExplorationData;
        event EventHandler<MultiSellExplorationDataEvent>? MultiSellExplorationData;
        event EventHandler<TouchdownEvent>? Touchdown;
        event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;

        void StartMonitoring();
        void StopMonitoring();
        void Reset();

        LocationChangedEventArgs? GetLastKnownLocation();
        DockedEventArgs? GetLastKnownDockedState();
    }
}

