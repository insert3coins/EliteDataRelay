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
        event EventHandler<JournalEvents.MaterialCollectedEventArgs>? MaterialCollected;
        event EventHandler<JournalEvents.AsteroidCrackedEventArgs>? AsteroidCracked;
        event EventHandler<JournalEvents.ProspectedAsteroidEventArgs>? ProspectedAsteroid;
        event EventHandler<JournalEvents.SupercruiseExitEventArgs>? SupercruiseExit;
        event EventHandler<JournalEvents.SupercruiseEntryEventArgs>? SupercruiseEntry;
        event EventHandler<JournalEvents.MusicTrackEventArgs>? MusicTrackChanged;
        event EventHandler<JournalEvents.ShutdownEventArgs>? Shutdown;
        event EventHandler<JournalEvents.FileheaderEventArgs>? FileheaderRead;
        event EventHandler<FSSDiscoveryScanEvent>? FSSDiscoveryScan;
        event EventHandler<ScanEvent>? BodyScanned;
        event EventHandler<SAAScanCompleteEvent>? SAAScanComplete;
        event EventHandler<FSSBodySignalsEvent>? FSSBodySignals;
        event EventHandler<SAASignalsFoundEvent>? SAASignalsFound;
        event EventHandler<FSSSignalDiscoveredEvent>? FSSSignalDiscovered;
        event EventHandler<FSSAllBodiesFoundEvent>? FSSAllBodiesFound;
        event EventHandler<JournalEventArgs>? JournalEventReceived;
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

        void StartMonitoring();
        void StopMonitoring();
        void Reset();

        LocationChangedEventArgs? GetLastKnownLocation();
        DockedEventArgs? GetLastKnownDockedState();
    }
}
