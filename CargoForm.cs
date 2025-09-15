using System;
using System.IO;
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
        private readonly ISoundService _soundService;
        private readonly IFileOutputService _fileOutputService;
        private readonly ICargoFormUI _cargoFormUI;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public CargoForm(
            IFileMonitoringService fileMonitoringService,
            ICargoProcessorService cargoProcessorService,
            ISoundService soundService,
            IFileOutputService fileOutputService,
            ICargoFormUI cargoFormUI)
        {
            _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));
            _cargoProcessorService = cargoProcessorService ?? throw new ArgumentNullException(nameof(cargoProcessorService));
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
            _soundService = new SoundService();
            _fileOutputService = new FileOutputService();
            _cargoFormUI = new CargoFormUI();

            InitializeComponent();
            SetupEventHandlers();
        }

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
        }

        private void CargoForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Stop monitoring and dispose services
            StopMonitoringInternal();
            
            // Dispose services
            (_fileMonitoringService as IDisposable)?.Dispose();
            (_soundService as IDisposable)?.Dispose();
            _cargoFormUI?.Dispose();
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
            Close();
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            _cargoFormUI.AppendText($"{AppConfiguration.AboutText}{Environment.NewLine}");
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
            _cargoFormUI.UpdateCargoDisplay(e.Snapshot);
            
            // Write to output file
            _fileOutputService.WriteCargoSnapshot(e.Snapshot);
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
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose services
                (_fileMonitoringService as IDisposable)?.Dispose();
                (_soundService as IDisposable)?.Dispose();
                _cargoFormUI?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}