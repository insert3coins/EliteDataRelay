﻿using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Services;
using EliteDataRelay.UI;
using EliteDataRelay.Models;

namespace EliteDataRelay
{
    public partial class CargoForm : Form
    {
        // Service dependencies
        private readonly IFileMonitoringService _fileMonitoringService;
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;
        private readonly ISoundService _soundService;
        private readonly ICargoFormUI _cargoFormUI;
        private readonly SessionTrackingService _sessionTrackingService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly IStationInfoService _stationInfoService;
        private readonly OverlayService _overlayService;
        private readonly ExplorationDataService _explorationDataService;
        private readonly ExplorationDatabaseService _explorationDatabaseService;
        private readonly ScreenshotRenamerService _screenshotRenamerService;
        private readonly WebOverlayServerService _webOverlayService;
        public CargoForm()
        {
            // Create all service instances. This form now owns its dependencies,
            // simplifying the application's object graph and ensuring only one
            // set of services exists.
            _journalWatcherService = new JournalWatcherService();
            _fileMonitoringService = new FileMonitoringService(_journalWatcherService);
            _cargoProcessorService = new CargoProcessorService();
            _soundService = new SoundService();
            _sessionTrackingService = new SessionTrackingService(_cargoProcessorService, _journalWatcherService);
            _systemInfoService = new SystemInfoService(_journalWatcherService);
            _stationInfoService = new StationInfoService(_journalWatcherService);
            _overlayService = new OverlayService();
            // Initialize the database service BEFORE creating any UI that might use it.
            _explorationDatabaseService = new ExplorationDatabaseService();
            _explorationDatabaseService.Initialize();
            _explorationDataService = new ExplorationDataService(_explorationDatabaseService);
            _cargoFormUI = new CargoFormUI(_overlayService, _sessionTrackingService, _explorationDataService);

            // Optional services
            _screenshotRenamerService = new ScreenshotRenamerService(_journalWatcherService);
            _webOverlayService = new WebOverlayServerService();

            InitializeComponent();

            SetupEventHandlers();
        }

        private int? _cargoCapacity;
        private bool _isInitializing;
        private SessionSummaryForm? _sessionSummaryForm;

        // Cache for last known values to re-populate the overlay when it's restarted.
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipType;
        private ShipLoadout? _lastLoadout;
        private string? _lastShipIdent;
        private string? _lastInternalShipName;
        private long? _lastBalance;
        private uint? _lastShipId;
        private string? _lastLocation;
        private long? _lastSystemAddress;
        private DateTime? _lastLocationTimestamp;
        private CargoSnapshot? _lastCargoSnapshot;
        private StationInfoData? _lastStationInfoData;
        private MaterialsEvent? _lastMaterials;
        private SystemInfoData? _lastSystemInfoData;
        private Status? _lastStatus;

        private System.Windows.Forms.Timer? _gameProcessCheckTimer;

        private async void RunHistoricalExplorationImportIfNeeded()
        {
            try
            {
                var importer = new ExplorationHistoryImportService(_explorationDataService);
                var imported = await importer.ImportIfNeededAsync();
                if (imported)
                {
                    // Refresh the exploration log UI if the import added data.
                    _cargoFormUI.RefreshExplorationLog();
                    SafeInvoke(() => _cargoFormUI.ShowInfoPopup("Exploration History Import", "Historical exploration data imported."));

                    // Ensure a current system is selected in exploration after import.
                    // Prefer current in-game location if available; fall back to most recent DB system
                    SystemExplorationData? resolved = null;
                    if (!string.IsNullOrWhiteSpace(_lastLocation))
                    {
                        if (_lastSystemAddress.HasValue)
                        {
                            _explorationDataService.HandleSystemChange(_lastLocation, _lastSystemAddress, _lastLocationTimestamp ?? DateTime.UtcNow);
                            resolved = _explorationDataService.GetCurrentSystemData();
                        }
                        else
                        {
                            // Try resolve by name from database
                            var byName = _explorationDatabaseService.LoadSystemByName(_lastLocation);
                            if (byName?.SystemAddress.HasValue == true)
                            {
                                _explorationDataService.HandleSystemChange(byName.SystemName, byName.SystemAddress, byName.LastVisited);
                                resolved = _explorationDataService.GetCurrentSystemData();
                            }
                        }
                    }

                    if (resolved == null)
                    {
                        var lastVisited = _explorationDatabaseService.GetVisitedSystems(1).FirstOrDefault();
                        if (lastVisited != null && lastVisited.SystemAddress.HasValue)
                        {
                            _explorationDataService.HandleSystemChange(lastVisited.SystemName, lastVisited.SystemAddress, lastVisited.LastVisited);
                            resolved = _explorationDataService.GetCurrentSystemData();
                        }
                    }

                    if (resolved != null)
                    {
                        _cargoFormUI.UpdateExplorationCurrentSystem(resolved);
                    }

                    // If monitoring is already active, ensure exploration visuals are active like Start did.
                    if (_fileMonitoringService.IsMonitoring)
                    {
                        var currentSystem = _explorationDataService.GetCurrentSystemData();
                        var session = _explorationDataService.GetSessionData();

                        // Update desktop overlay
                        _overlayService.UpdateExplorationData(currentSystem);
                        _overlayService.UpdateExplorationSessionData(session);

                        // Update web overlay
                        _webOverlayService.UpdateExploration(currentSystem);
                        _webOverlayService.UpdateExplorationSession(session);
                    }
                }
            }
            catch
            {
                // Swallow exceptions to avoid impacting app startup.
            }
        }

        private void InitializeComponent()
        {
            // Initialize the UI through the UI service
            _cargoFormUI.InitializeUI(this);
        }

        private void SetupEventHandlers()
        {
            // Wire up form events
            this.Load += CargoForm_Load;
            FormClosing += CargoForm_FormClosing;

            // Wire up UI events
            _cargoFormUI.StartClicked += OnStartClicked;
            _cargoFormUI.StopClicked += OnStopClicked;
            _cargoFormUI.ExitClicked += OnExitClicked;
            _cargoFormUI.AboutClicked += OnAboutClicked;
            _cargoFormUI.SettingsClicked += OnSettingsClicked;

            // The session button is now handled inside the settings form logic,
            // but we can still handle it here if needed for other purposes.
            // For now, we'll use the settings form to show it.
            // If you want a dedicated button, you can re-add this.
            _cargoFormUI.SessionClicked += OnSessionClicked;

            _cargoFormUI.MiningStartClicked += OnMiningStartClicked;
            _cargoFormUI.MiningStopClicked += OnMiningStopClicked;

            // Timer to periodically check if the game process is still running
            _gameProcessCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Check every 5 seconds
            };
            _gameProcessCheckTimer.Tick += OnGameProcessCheck;

            // Wire up service events
            // Use a lambda to subscribe ProcessCargoFile (which returns bool) to the FileChanged event (which expects void).
            // We discard the boolean result as it's not needed for file change notifications.
            _fileMonitoringService.FileChanged += (fileName) => OnGameFileChanged(fileName);

            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.MaterialsChanged += OnMaterialsChanged;
            _journalWatcherService.CargoCapacityChanged += OnCargoCapacityChanged;
            _journalWatcherService.LocationChanged += OnLocationChanged;
            _journalWatcherService.BalanceChanged += OnBalanceChanged;

            _sessionTrackingService.MiningNotificationRaised += OnMiningNotificationRaised;
            _sessionTrackingService.SessionUpdated += OnSessionUpdated;
            _journalWatcherService.CommanderNameChanged += OnCommanderNameChanged;
            _journalWatcherService.ShipInfoChanged += OnShipInfoChanged;
            _journalWatcherService.LoadoutChanged += OnLoadoutChanged;
            _journalWatcherService.StatusChanged += OnStatusChanged;
            _journalWatcherService.InitialScanComplete += OnInitialScanComplete;
            _stationInfoService.StationInfoUpdated += OnStationInfoUpdated; // This line was missing
            _systemInfoService.SystemInfoUpdated += OnSystemInfoUpdated;

            // Mining companion removed

            // Wire up exploration events
            _journalWatcherService.FSSDiscoveryScan += (sender, e) => _explorationDataService.HandleFSSDiscoveryScan(e);
            _journalWatcherService.FSSAllBodiesFound += (sender, e) => _explorationDataService.HandleFSSAllBodiesFound(e);
            _journalWatcherService.NavBeaconScan += (sender, e) => _explorationDataService.HandleNavBeaconScan(e);
            _journalWatcherService.FSSSignalDiscovered += (sender, e) => _explorationDataService.HandleFSSSignalDiscovered(e);
            _journalWatcherService.DiscoveryScan += (sender, e) => _explorationDataService.HandleDiscoveryScan(e);
            _journalWatcherService.BodyScanned += (sender, e) => _explorationDataService.HandleScan(e);
            _journalWatcherService.SAAScanComplete += (sender, e) => _explorationDataService.HandleSAAScanComplete(e);
            _journalWatcherService.FSSBodySignals += (sender, e) => _explorationDataService.HandleFSSBodySignals(e);
            _journalWatcherService.SAASignalsFound += (sender, e) => _explorationDataService.HandleSAASignalsFound(e);
            _journalWatcherService.FirstFootfall += (sender, e) => _explorationDataService.HandleFirstFootfall(e);
            _journalWatcherService.ScanOrganic += (sender, e) => _explorationDataService.HandleScanOrganic(e);
            _journalWatcherService.SellOrganicData += (sender, e) => _explorationDataService.HandleSellOrganicData(e);
            _journalWatcherService.CodexEntry += (sender, e) => _explorationDataService.HandleCodexEntry(e);
            _journalWatcherService.Touchdown += (sender, e) => _explorationDataService.HandleTouchdown(e);
            _journalWatcherService.SellExplorationData += (sender, e) => {
                _explorationDataService.HandleSellExplorationData(e);
                // After selling data, refresh the log to show updated discovery/mapping statuses.
                _cargoFormUI.RefreshExplorationLog();
            };

            // Push exploration updates to web overlay
            _explorationDataService.SystemDataChanged += (s, data) => _webOverlayService.UpdateExploration(data);
            _explorationDataService.SessionDataChanged += (s, data) => _webOverlayService.UpdateExplorationSession(data);
        }

        private void OnMiningNotificationRaised(object? sender, MiningNotificationEventArgs e)
        {
            SafeInvoke(() =>
            {
                _cargoFormUI.AppendMiningAnnouncement(e); // Add announcement to the UI list
                _cargoFormUI.ShowMiningNotification(e);   // Show the tray notification

                // Reminder sound removed with Mining companion
            });
        }

        private async void OnGameFileChanged(string fileName)
        {
            // This service now only cares about Cargo.json
            if (fileName.Equals("Cargo.json", StringComparison.OrdinalIgnoreCase)) await _cargoProcessorService.ProcessCargoFileAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose services
                (_journalWatcherService as IDisposable)?.Dispose();
                (_fileMonitoringService as IDisposable)?.Dispose();
                (_soundService as IDisposable)?.Dispose();
                _cargoFormUI?.Dispose();
                _gameProcessCheckTimer?.Dispose();
                _sessionSummaryForm?.Dispose();
                _sessionTrackingService.Dispose();
                (_stationInfoService as IDisposable)?.Dispose();
                (_systemInfoService as IDisposable)?.Dispose();
                _overlayService.Dispose();
                _explorationDataService.Dispose();
                _explorationDatabaseService.Dispose();
                _screenshotRenamerService?.Dispose();
                _webOverlayService?.Dispose();
                
            }
            base.Dispose(disposing);
        }
    }
}
