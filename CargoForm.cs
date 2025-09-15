﻿﻿﻿﻿﻿using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Services;
using EliteCargoMonitor.UI;

namespace EliteCargoMonitor
{
    /// <summary>
    /// Main form that coordinates between UI and services
    /// </summary>
    public partial class CargoForm : Form
    {
        // Service dependencies
        private readonly IFileMonitoringService _fileMonitoringService;
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;
        private readonly ISoundService _soundService;
        private readonly IFileOutputService _fileOutputService;
        private readonly ICargoFormUI _cargoFormUI;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public CargoForm(
            IFileMonitoringService fileMonitoringService,
            ICargoProcessorService cargoProcessorService,
            IJournalWatcherService journalWatcherService,
            ISoundService soundService,
            IFileOutputService fileOutputService,
            ICargoFormUI cargoFormUI)
        {
            _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));
            _cargoProcessorService = cargoProcessorService ?? throw new ArgumentNullException(nameof(cargoProcessorService));
            _journalWatcherService = journalWatcherService ?? throw new ArgumentNullException(nameof(journalWatcherService));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _fileOutputService = fileOutputService ?? throw new ArgumentNullException(nameof(fileOutputService));
            _cargoFormUI = cargoFormUI ?? throw new ArgumentNullException(nameof(cargoFormUI));

            InitializeComponent();
            SetupEventHandlers();
        }

        /// <summary>
        /// Parameterless constructor for design-time support and simple instantiation
        /// </summary>
        public CargoForm()
        {
            // Create default service instances for design-time and simple usage
            _fileMonitoringService = new FileMonitoringService();
            _cargoProcessorService = new CargoProcessorService();
            _journalWatcherService = new JournalWatcherService();
            _soundService = new SoundService();
            _fileOutputService = new FileOutputService();
            _cargoFormUI = new CargoFormUI();

            InitializeComponent();
            SetupEventHandlers();
        }

        private int? _cargoCapacity;
        private bool _isExiting;

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

            // Wire up service events
            _fileMonitoringService.FileChanged += OnFileChanged;
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.CargoCapacityChanged += OnCargoCapacityChanged;
        }

        #region Form Events

        private void CargoForm_Load(object? sender, EventArgs e)
        {
            // Check if cargo file exists on startup
            if (!File.Exists(AppConfiguration.CargoPath))
            {
                MessageBox.Show(
                    $"Cargo.json not found.\nMake sure Elite Dangerous is running\nand the file is at:\n{AppConfiguration.CargoPath}",
                    "File not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Check if journal directory exists
            if (string.IsNullOrEmpty(_journalWatcherService.JournalDirectoryPath) || !Directory.Exists(_journalWatcherService.JournalDirectoryPath))
            {
                MessageBox.Show(
                    $"Journal directory not found.\nJournal watcher will be disabled.\nPath: {_journalWatcherService.JournalDirectoryPath}",
                    "Directory not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // If user closes window, hide to tray instead of exiting
            if (e.CloseReason == CloseReason.UserClosing && !_isExiting)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized; // This will trigger the hide logic in CargoFormUI
            }
            else
            {
                // Stop monitoring and dispose services on actual exit
                StopMonitoringInternal();
                (_journalWatcherService as IDisposable)?.Dispose();
                (_fileMonitoringService as IDisposable)?.Dispose();
                (_soundService as IDisposable)?.Dispose();
                _cargoFormUI?.Dispose();
            }
        }

        #endregion

        #region UI Event Handlers

        private void OnStartClicked(object? sender, EventArgs e)
        {
            StartMonitoring();
        }

        private void OnStopClicked(object? sender, EventArgs e)
        {
            StopMonitoringInternal();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            string fullAboutText = $"{AppConfiguration.AboutInfo}{Environment.NewLine}Project Page: {AppConfiguration.AboutUrl}";
            //_cargoFormUI.AppendText(fullAboutText + Environment.NewLine);

            string message = $"{fullAboutText}{Environment.NewLine}{Environment.NewLine}Would you like to open the project page in your browser?";

            var result = MessageBox.Show(
                this,
                message,
                "About Elite Cargo Monitor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(AppConfiguration.AboutUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Service Event Handlers

        private void OnFileChanged(object? sender, EventArgs e)
        {
            // Delegate file processing to the cargo processor service
            _cargoProcessorService.ProcessCargoFile();
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            // Update UI with new cargo data
            _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);
            
            // Write to output file
            _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            _cargoCapacity = e.CargoCapacity;
        }

        #endregion

        #region Monitoring Control

        private void StartMonitoring()
        {
            // Play start sound
            _soundService.PlayStartSound();
            
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateTitle($"Cargo Monitor – Watching: {AppConfiguration.CargoPath}");

            // Start journal monitoring to find capacity before the first cargo read
            _journalWatcherService.StartMonitoring();

            // Process initial file snapshot
            _cargoProcessorService.ProcessCargoFile();
            
            // Start file monitoring
            _fileMonitoringService.StartMonitoring();
        }

        private void StopMonitoringInternal()
        {
            // Play stop sound
            _soundService.PlayStopSound();
            
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: true, stopEnabled: false);
            _cargoFormUI.UpdateTitle($"Cargo Monitor – Stopped: {AppConfiguration.CargoPath}");

            // Stop file monitoring
            _fileMonitoringService.StopMonitoring();

            // Stop journal monitoring
            _journalWatcherService.StopMonitoring();
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
                _cargoFormUI?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}