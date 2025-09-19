﻿using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        #region Hotkey P/Invoke

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_START = 1;
        private const int HOTKEY_ID_STOP = 2;
        private const int HOTKEY_ID_SHOW = 3;
        private const int HOTKEY_ID_HIDE = 4;

        #endregion

        // Service dependencies
        private readonly IFileMonitoringService _fileMonitoringService;
        private readonly ICargoProcessorService _cargoProcessorService;
        private readonly IJournalWatcherService _journalWatcherService;
        private readonly ISoundService _soundService;
        private readonly IFileOutputService _fileOutputService;
        private readonly IStatusWatcherService _statusWatcherService;
        private readonly ICargoFormUI _cargoFormUI;

        public CargoForm(
            IFileMonitoringService fileMonitoringService,
            ICargoProcessorService cargoProcessorService,
            IJournalWatcherService journalWatcherService,
            ISoundService soundService,
            IFileOutputService fileOutputService,
            ICargoFormUI cargoFormUI,
            IStatusWatcherService statusWatcherService)
        {
            _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));
            _cargoProcessorService = cargoProcessorService ?? throw new ArgumentNullException(nameof(cargoProcessorService));
            _journalWatcherService = journalWatcherService ?? throw new ArgumentNullException(nameof(journalWatcherService));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _fileOutputService = fileOutputService ?? throw new ArgumentNullException(nameof(fileOutputService));
            _statusWatcherService = statusWatcherService ?? throw new ArgumentNullException(nameof(statusWatcherService));
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
            _statusWatcherService = new StatusWatcherService();
            _cargoFormUI = new CargoFormUI();

            InitializeComponent();
            SetupEventHandlers();
        }

        private int? _cargoCapacity;
        private bool _isExiting;

        // Cache for last known values to re-populate the overlay when it's restarted.
        private string? _lastCommanderName;
        private string? _lastShipName;
        private string? _lastShipIdent;
        private long? _lastBalance;
        private string? _lastLocation;
        private CargoSnapshot? _lastCargoSnapshot;

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
            _statusWatcherService.BalanceChanged += OnBalanceChanged;

            // Assumes JournalWatcherService is updated to provide these events
            _journalWatcherService.CommanderNameChanged += OnCommanderNameChanged;
            _journalWatcherService.ShipInfoChanged += OnShipInfoChanged;
        }

        #region Form Events

        private void CargoForm_Load(object? sender, EventArgs e)
        {
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // Restore window size and location from settings
            if (AppConfiguration.WindowSize.Width > 0 && AppConfiguration.WindowSize.Height > 0)
            {
                this.Size = AppConfiguration.WindowSize;
            }

            // Ensure the form is not loaded off-screen
            if (AppConfiguration.WindowLocation != Point.Empty)
            {
                bool isVisible = Screen.AllScreens.Any(s => s.WorkingArea.Contains(AppConfiguration.WindowLocation));

                if (isVisible)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = AppConfiguration.WindowLocation;
                }
            }

            // Restore window state, but don't start minimized.
            if (AppConfiguration.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }

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
            if (AppConfiguration.EnableHotkeys)
            {
                UnregisterHotkeys();
            }

            // Save window state before closing.
            // Use RestoreBounds if the window is minimized or maximized.
            switch (this.WindowState)
            {
                case FormWindowState.Maximized:
                    AppConfiguration.WindowState = FormWindowState.Maximized;
                    AppConfiguration.WindowLocation = this.RestoreBounds.Location;
                    AppConfiguration.WindowSize = this.RestoreBounds.Size;
                    break;
                case FormWindowState.Normal:
                    AppConfiguration.WindowState = FormWindowState.Normal;
                    AppConfiguration.WindowLocation = this.Location;
                    AppConfiguration.WindowSize = this.Size;
                    break;
                default: // Minimized
                    AppConfiguration.WindowState = FormWindowState.Normal; // Don't save as minimized
                    AppConfiguration.WindowLocation = this.RestoreBounds.Location;
                    AppConfiguration.WindowSize = this.RestoreBounds.Size;
                    break;
            }
            AppConfiguration.Save();

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

        private void OnStartClicked(object? sender, EventArgs e)
        {
            StartMonitoring();
        }

        private void OnStopClicked(object? sender, EventArgs e)
        {
            _soundService.PlayStopSound();
            StopMonitoringInternal();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _isExiting = true;
            Close();
        }

        private void OnAboutClicked(object? sender, EventArgs e)
        {
            // Open the dedicated About form instead of a simple MessageBox.
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void OnSettingsClicked(object? sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    ApplyLiveSettingsChanges();
                }
            }
        }

        /// <summary>
        /// Applies settings changes that need to take effect immediately,
        /// such as hotkeys and overlay visibility.
        /// </summary>
        private void ApplyLiveSettingsChanges()
        {
            // Unregister any existing hotkeys before re-registering, to handle changes.
            UnregisterHotkeys();
            if (AppConfiguration.EnableHotkeys)
            {
                RegisterHotkeys();
            }

            // If monitoring is active, refresh the overlay to apply visibility changes.
            if (_fileMonitoringService.IsMonitoring)
            {
                _cargoFormUI.RefreshOverlay();
                RepopulateOverlay();
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
            _lastCargoSnapshot = e.Snapshot;
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
            _lastLocation = e.StarSystem;
            _cargoFormUI.UpdateLocation(e.StarSystem);
        }

        private void OnBalanceChanged(object? sender, BalanceChangedEventArgs e)
        {
            _lastBalance = e.Balance;
            _cargoFormUI.UpdateBalance(e.Balance);
        }

        private void OnCommanderNameChanged(object? sender, CommanderNameChangedEventArgs e)
        {
            _lastCommanderName = e.CommanderName;
            _cargoFormUI.UpdateCommanderName(e.CommanderName);
        }

        private void OnShipInfoChanged(object? sender, ShipInfoChangedEventArgs e)
        {
            _lastShipName = e.ShipName;
            _lastShipIdent = e.ShipIdent;
            _cargoFormUI.UpdateShipInfo(e.ShipName, e.ShipIdent);
        }

        #endregion

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
        }

        #endregion

        #region Hotkey Handling

        private void RegisterHotkeys()
        {
            RegisterHotkey(HOTKEY_ID_START, AppConfiguration.StartMonitoringHotkey);
            RegisterHotkey(HOTKEY_ID_STOP, AppConfiguration.StopMonitoringHotkey);
            RegisterHotkey(HOTKEY_ID_SHOW, AppConfiguration.ShowOverlayHotkey);
            RegisterHotkey(HOTKEY_ID_HIDE, AppConfiguration.HideOverlayHotkey);
        }

        private void UnregisterHotkeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_START);
            UnregisterHotKey(this.Handle, HOTKEY_ID_STOP);
            UnregisterHotKey(this.Handle, HOTKEY_ID_SHOW);
            UnregisterHotKey(this.Handle, HOTKEY_ID_HIDE);
        }

        private void RegisterHotkey(int id, Keys key)
        {
            if (key == Keys.None) return;

            uint modifiers = 0;
            if ((key & Keys.Alt) == Keys.Alt) modifiers |= 1;
            if ((key & Keys.Control) == Keys.Control) modifiers |= 2;
            if ((key & Keys.Shift) == Keys.Shift) modifiers |= 4;

            Keys keyCode = key & ~Keys.Modifiers;

            RegisterHotKey(this.Handle, id, modifiers, (uint)keyCode);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_ID_START:
                        if (!_fileMonitoringService.IsMonitoring) StartMonitoring();
                        break;
                    case HOTKEY_ID_STOP:
                        if (_fileMonitoringService.IsMonitoring) OnStopClicked(null, EventArgs.Empty);
                        break;
                    case HOTKEY_ID_SHOW:
                        _cargoFormUI.ShowOverlays();
                        break;
                    case HOTKEY_ID_HIDE:
                        _cargoFormUI.HideOverlays();
                        break;
                }
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
            }
            base.Dispose(disposing);
        }
    }
}