﻿using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
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
        private readonly IStatusWatcherService _statusWatcherService;
        private readonly ICargoFormUI _cargoFormUI;
        private readonly SessionTrackingService _sessionTrackingService;

        public CargoForm(
            IFileMonitoringService fileMonitoringService,
            ICargoProcessorService cargoProcessorService,
            IJournalWatcherService journalWatcherService,
            ISoundService soundService,
            IFileOutputService fileOutputService,
            ICargoFormUI cargoFormUI,
            IStatusWatcherService statusWatcherService,
            SessionTrackingService sessionTrackingService)
        {
            _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));
            _cargoProcessorService = cargoProcessorService ?? throw new ArgumentNullException(nameof(cargoProcessorService));
            _journalWatcherService = journalWatcherService ?? throw new ArgumentNullException(nameof(journalWatcherService));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _fileOutputService = fileOutputService ?? throw new ArgumentNullException(nameof(fileOutputService));
            _statusWatcherService = statusWatcherService ?? throw new ArgumentNullException(nameof(statusWatcherService));
            _cargoFormUI = cargoFormUI ?? throw new ArgumentNullException(nameof(cargoFormUI));
            _sessionTrackingService = sessionTrackingService ?? throw new ArgumentNullException(nameof(sessionTrackingService));

            InitializeComponent();
            SetupEventHandlers();
        }

        public CargoForm()
        {
            // Create default service instances for design-time and simple usage
            _fileMonitoringService = new FileMonitoringService();
            _cargoProcessorService = new CargoProcessorService();
            _journalWatcherService = new JournalWatcherService();
            _soundService = new SoundService();
            _fileOutputService = new FileOutputService();
            _statusWatcherService = new StatusWatcherService();
            _cargoFormUI = new CargoFormUI();
            _sessionTrackingService = new SessionTrackingService();

            InitializeComponent();
            SetupEventHandlers();
        }

        private int? _cargoCapacity;
        private bool _isExiting;
        private SessionSummaryForm? _sessionSummaryForm;

        // Cache for last known values to re-populate the overlay when it's restarted.
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipIdent;
        private long? _lastBalance;
        private string? _lastLocation;
        private CargoSnapshot? _lastCargoSnapshot;

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
            _statusWatcherService.BalanceChanged += OnBalanceChanged;

            _sessionTrackingService.SessionUpdated += OnSessionUpdated;
            // Assumes JournalWatcherService is updated to provide these events
            _journalWatcherService.CommanderNameChanged += OnCommanderNameChanged;
            _journalWatcherService.ShipInfoChanged += OnShipInfoChanged;
        }

        #region Monitoring Control

        private void RepopulateOverlay()
        {
            // Re-populate the UI (and the new overlay) with the last known data.
            if (_lastCommanderName != null) _cargoFormUI.UpdateCommanderName(_lastCommanderName);
            if (_lastShipName != null && _lastShipIdent != null) _cargoFormUI.UpdateShipInfo(_lastShipName, _lastShipIdent);
            if (_lastBalance.HasValue) _cargoFormUI.UpdateBalance(_lastBalance.Value);
            if (_lastLocation != null) _cargoFormUI.UpdateLocation(_lastLocation);
            if (_lastCargoSnapshot != null)
            {
                _cargoFormUI.UpdateCargoList(_lastCargoSnapshot);
                _cargoFormUI.UpdateCargoHeader(_lastCargoSnapshot.Count, _cargoCapacity);
                _cargoFormUI.UpdateCargoDisplay(_lastCargoSnapshot, _cargoCapacity);
            }
        }

        private void StartMonitoring()
        {
            // Play start sound
            _soundService.PlayStartSound();
            
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateTitle("Elite Data Relay – Watching");

            // Re-populate the UI (and the new overlay) with the last known data.
            RepopulateOverlay();

            // Start journal monitoring to find capacity before the first cargo read
            _journalWatcherService.StartMonitoring();

            // Start status monitoring for balance
            _statusWatcherService.StartMonitoring();

            // Process initial file snapshot
            _cargoProcessorService.ProcessCargoFile();
            
            // Start file monitoring
            _fileMonitoringService.StartMonitoring();

            // Start the game process checker
            _gameProcessCheckTimer?.Start();

            // Start the session tracker
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StartSession();
            }
        }

        private void StopMonitoringInternal()
        {
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: true, stopEnabled: false);
            _cargoFormUI.UpdateTitle("Elite Data Relay – Stopped");

            // Stop file monitoring
            _fileMonitoringService.StopMonitoring();

            // Stop journal monitoring
            _journalWatcherService.StopMonitoring();

            // Stop status monitoring
            _statusWatcherService.StopMonitoring();

            // Stop the game process checker
            _gameProcessCheckTimer?.Stop();

            // Stop the session tracker
            if (AppConfiguration.EnableSessionTracking)
            {
                _sessionTrackingService.StopSession();
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose services
                (_journalWatcherService as IDisposable)?.Dispose();
                (_fileMonitoringService as IDisposable)?.Dispose();
                (_soundService as IDisposable)?.Dispose();
                (_statusWatcherService as IDisposable)?.Dispose();
                _cargoFormUI?.Dispose();
                _gameProcessCheckTimer?.Dispose();
                _sessionSummaryForm?.Dispose();
                _sessionTrackingService.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}