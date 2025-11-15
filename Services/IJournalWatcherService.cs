using EliteDataRelay.Models;
using System;
using JournalEvents = EliteDataRelay.Models.Journal;

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
        event EventHandler<FSSSignalDiscoveredEvent>? FSSSignalDiscovered;
        event EventHandler<FSSAllBodiesFoundEvent>? FSSAllBodiesFound;
        event EventHandler<DiscoveryScanEvent>? DiscoveryScan;
        event EventHandler<NavBeaconScanEvent>? NavBeaconScan;
        event EventHandler<FirstFootfallEvent>? FirstFootfall;
        event EventHandler<ScanOrganicEvent>? ScanOrganic;
        event EventHandler<SellOrganicDataEvent>? SellOrganicData;
        event EventHandler<CodexEntryEvent>? CodexEntry;
        event EventHandler<SellExplorationDataEvent>? SellExplorationData;
        event EventHandler<MultiSellExplorationDataEvent>? MultiSellExplorationData;
        event EventHandler<TouchdownEvent>? Touchdown;
        event EventHandler<ScreenshotEventArgs>? ScreenshotTaken;
        event EventHandler<NextJumpSystemChangedEventArgs>? NextJumpSystemChanged;
        event EventHandler<JumpInitiatedEventArgs>? JumpInitiated;
        event EventHandler<JumpCompletedEventArgs>? JumpCompleted;
        event EventHandler<JournalEvents.CarrierStatsEvent.CarrierStatsEventArgs>? CarrierStats;
        event EventHandler<JournalEvents.CarrierLocationEvent.CarrierLocationEventArgs>? CarrierLocation;
        event EventHandler<JournalEvents.CarrierJumpRequestEvent.CarrierJumpRequestEventArgs>? CarrierJumpRequested;
        event EventHandler<JournalEvents.CarrierJumpCancelledEvent.CarrierJumpCancelledEventArgs>? CarrierJumpCancelled;
        event EventHandler<JournalEvents.CarrierTradeOrderEvent.CarrierTradeOrderEventArgs>? CarrierTradeOrder;
        event EventHandler<JournalEvents.CarrierCrewServicesEvent.CarrierCrewServicesEventArgs>? CarrierCrewServices;
        event EventHandler<JournalEvents.CarrierBankTransferEvent.CarrierBankTransferEventArgs>? CarrierBankTransfer;
        event EventHandler<JournalEvents.CarrierDepositFuelEvent.CarrierDepositFuelEventArgs>? CarrierFuelDeposited;
        event EventHandler<JournalEvents.CargoTransferEvent.CargoTransferEventArgs>? CargoTransfer;
        event EventHandler<JournalEvents.MarketSellEvent.MarketSellEventArgs>? MarketSell;

        void StartMonitoring();
        void StopMonitoring();
        void Reset();

        LocationChangedEventArgs? GetLastKnownLocation();
        DockedEventArgs? GetLastKnownDockedState();
    }
}
