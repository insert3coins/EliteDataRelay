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
        private readonly IFileOutputService _fileOutputService;
        private readonly ICargoFormUI _cargoFormUI;
        private readonly IMaterialService _materialService;
        private readonly SessionTrackingService _sessionTrackingService;
        private readonly ISystemInfoService _systemInfoService;

        public CargoForm()
        {
            // Create all service instances. This form now owns its dependencies,
            // simplifying the application's object graph and ensuring only one
            // set of services exists.
            _fileMonitoringService = new FileMonitoringService();
            _cargoProcessorService = new CargoProcessorService();
            _journalWatcherService = new JournalWatcherService();
            _soundService = new SoundService();
            _fileOutputService = new FileOutputService();
            _cargoFormUI = new CargoFormUI();
            _materialService = new MaterialService(_journalWatcherService);
            _sessionTrackingService = new SessionTrackingService(_cargoProcessorService, _journalWatcherService);
            _systemInfoService = new SystemInfoService(_journalWatcherService);

            InitializeComponent();
            SetupEventHandlers();
        }

        private int? _cargoCapacity;
        private bool _isExiting;
        private SessionSummaryForm? _sessionSummaryForm;

        // Cache for last known values to re-populate the overlay when it's restarted.
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipType;
        private string? _lastShipIdent;
        private string? _lastInternalShipName;
        private long? _lastBalance;
        private string? _lastLocation;
        private CargoSnapshot? _lastCargoSnapshot;
        private IMaterialService? _lastMaterialServiceCache;
        private SystemInfoData? _lastSystemInfoData;

        private System.Windows.Forms.Timer? _gameProcessCheckTimer;

        private void InitializeComponent()
        {
            // Initialize the UI through the UI service
            _cargoFormUI.InitializeUI(this);
        }

        private void SetupEventHandlers()
        {
            // Wire up form events
            Load += CargoForm_Load;
            FormClosing += CargoForm_FormClosing;

            // Wire up UI events
            _cargoFormUI.StartClicked += OnStartClicked;
            _cargoFormUI.StopClicked += OnStopClicked;
            _cargoFormUI.ExitClicked += OnExitClicked;
            _cargoFormUI.AboutClicked += OnAboutClicked;
            _cargoFormUI.SettingsClicked += OnSettingsClicked;
            _cargoFormUI.SessionClicked += OnSessionClicked;

            // Timer to periodically check if the game process is still running
            _gameProcessCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Check every 5 seconds
            };
            _gameProcessCheckTimer.Tick += OnGameProcessCheck;

            // Wire up service events
            _fileMonitoringService.FileChanged += OnFileChanged;
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.CargoCapacityChanged += OnCargoCapacityChanged;
            _journalWatcherService.LocationChanged += OnLocationChanged;
            _journalWatcherService.BalanceChanged += OnBalanceChanged;

            _materialService.MaterialsUpdated += OnMaterialsUpdated;            
            _sessionTrackingService.SessionUpdated += OnSessionUpdated;
            // Assumes JournalWatcherService is updated to provide these events
            _journalWatcherService.CommanderNameChanged += OnCommanderNameChanged;
            _journalWatcherService.ShipInfoChanged += OnShipInfoChanged;
            _journalWatcherService.LoadoutChanged += OnLoadoutChanged;
            _journalWatcherService.StatusChanged += OnStatusChanged;
            _systemInfoService.SystemInfoUpdated += OnSystemInfoUpdated;
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
                (_materialService as IDisposable)?.Dispose();
                _gameProcessCheckTimer?.Dispose();
                _sessionSummaryForm?.Dispose();
                _sessionTrackingService.Dispose();
                (_systemInfoService as IDisposable)?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}