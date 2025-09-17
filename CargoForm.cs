﻿using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Services;
using EliteCargoMonitor.UI;

namespace EliteCargoMonitor
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
        private SettingsForm? _settingsForm;

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

            // Wire up service events
            _fileMonitoringService.FileChanged += OnFileChanged;
            _cargoProcessorService.CargoProcessed += OnCargoProcessed;
            _journalWatcherService.CargoCapacityChanged += OnCargoCapacityChanged;
            _journalWatcherService.LocationChanged += OnLocationChanged;
        }

        #region Form Events

        private async void CargoForm_Load(object? sender, EventArgs e)
        {
            // Asynchronously check if cargo file exists on startup to avoid blocking UI on slow I/O
            bool cargoFileExists = await Task.Run(() => File.Exists(AppConfiguration.CargoPath));
            if (!cargoFileExists)
            {
                MessageBox.Show(
                    this,
                    $"Cargo.json not found.\nPlease make sure Elite Dangerous is running and the file exists at:\n{AppConfiguration.CargoPath}",
                    "File not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Asynchronously check if journal directory exists
            string? journalPath = _journalWatcherService.JournalDirectoryPath;
            bool journalDirExists = await Task.Run(() => !string.IsNullOrEmpty(journalPath) && Directory.Exists(journalPath));
            if (!journalDirExists)
            {
                MessageBox.Show(
                    this,
                    $"Journal directory not found.\nJournal watcher will be disabled.\nExpected path: {journalPath}",
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
            }
        }

        #endregion

        #region UI Event Handlers

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            await StartMonitoring();
        }

        private async void OnStopClicked(object? sender, EventArgs e)
        {
            _soundService.PlayStopSound();
            await StopMonitoringInternal();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private async void OnAboutClicked(object? sender, EventArgs e)
        {
            string fullAboutText = $"{AppConfiguration.AboutInfo}{Environment.NewLine}Project Page: {AppConfiguration.AboutUrl}";

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
                    await Task.Run(() => Process.Start(new ProcessStartInfo(AppConfiguration.AboutUrl) { UseShellExecute = true }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            // If the settings form is already open, bring it to the front.
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Activate();
                return;
            }

            // Otherwise, create and show a new instance.
            _settingsForm = new SettingsForm();
            // Nullify the reference when the form is closed so a new one can be created next time.
            _settingsForm.FormClosed += (s, args) => _settingsForm = null;
            _settingsForm.Show(this);
        }

        #endregion

        #region Service Event Handlers

        private async void OnFileChanged(object? sender, EventArgs e)
        {
            // Delegate file processing to the cargo processor service
            await _cargoProcessorService.ProcessCargoFileAsync();
        }

        private void OnCargoProcessed(object? sender, CargoProcessedEventArgs e)
        {
            // --- File Output ---
            // If enabled in settings, write the snapshot to the output text file.
            if (AppConfiguration.EnableFileOutput)
            {
                _fileOutputService.WriteCargoSnapshot(e.Snapshot, _cargoCapacity);
            }

            int totalCount = e.Snapshot.Inventory.Sum(item => item.Count);

            // Update the header label in the button panel
            _cargoFormUI.UpdateCargoHeader(totalCount, _cargoCapacity);

            // Update the main window display with the new list view
            _cargoFormUI.UpdateCargoList(e.Snapshot);

            // Update the visual cargo size indicator
            _cargoFormUI.UpdateCargoDisplay(e.Snapshot, _cargoCapacity);
        }

        private void OnCargoCapacityChanged(object? sender, CargoCapacityEventArgs e)
        {
            _cargoCapacity = e.CargoCapacity;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _cargoFormUI.UpdateLocation(e.StarSystem);
        }

        #endregion

        #region Monitoring Control

        private Task StartMonitoring()
        {
            // Play start sound
            _soundService.PlayStartSound();
            
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: false, stopEnabled: true);
            _cargoFormUI.UpdateTitle("Cargo Monitor – Watching");

            // Run the rest of the start-up logic on a background thread
            // to keep the UI responsive, especially during the initial file read.
            return Task.Run(async () =>
            {
                // Start journal monitoring to find capacity before the first cargo read
                _journalWatcherService.StartMonitoring();

                // Process initial file snapshot
                await _cargoProcessorService.ProcessCargoFileAsync();
                
                // Start file monitoring
                _fileMonitoringService.StartMonitoring();
            });
        }

        private Task StopMonitoringInternal()
        {
            // Update UI state
            _cargoFormUI.SetButtonStates(startEnabled: true, stopEnabled: false);
            _cargoFormUI.UpdateTitle("Cargo Monitor – Stopped");

            // Run shutdown on a background thread to keep UI responsive.
            return Task.Run(() =>
            {
                // Stop file monitoring
                _fileMonitoringService.StopMonitoring();

                // Stop journal monitoring
                _journalWatcherService.StopMonitoring();
            });
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