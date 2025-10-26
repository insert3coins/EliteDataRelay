using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI : ICargoFormUI
    {
        private FontManager? _fontManager;
        private ControlFactory? _controlFactory;
        private Form? _form;
        private TrayIconManager? _trayIconManager;
        private Icon? _appIcon;
        private LayoutManager? _layoutManager;
        private readonly OverlayService _overlayService;
        private readonly SessionTrackingService _sessionTrackingService;
        private readonly ExplorationDataService _explorationDataService;
        private MemoryStream? _iconStream;
        private WatchingAnimationManager? _watchingAnimationManager;
        private string _currentLocation = "Unknown";
        private bool _isMonitoring;
        private bool _disposedValue;

        private string _baseTitle = "";

        public event EventHandler? StartClicked;

        public event EventHandler? StopClicked;

        public event EventHandler? ExitClicked;

        public event EventHandler? AboutClicked;

        public event EventHandler? SettingsClicked;

        public event EventHandler? SessionClicked;

        public event EventHandler? MiningStartClicked;

        public event EventHandler? MiningStopClicked;

        public CargoFormUI(OverlayService overlayService, SessionTrackingService sessionTrackingService, ExplorationDataService explorationDataService)
        {
            _overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));
            _sessionTrackingService = sessionTrackingService ?? throw new ArgumentNullException(nameof(sessionTrackingService));
            _explorationDataService = explorationDataService ?? throw new ArgumentNullException(nameof(explorationDataService));
        }

        public void InitializeUI(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            InitializeIcon();
            _fontManager = new FontManager();
            _controlFactory = new ControlFactory(_fontManager, _sessionTrackingService);

            if (_controlFactory.WatchingLabel != null)
            {
                _watchingAnimationManager = new WatchingAnimationManager(_controlFactory.WatchingLabel);
            }

            _layoutManager = new LayoutManager(_form, _controlFactory);

            _form.Resize += OnFormResize;
            _form.ResizeEnd += OnFormResizeEnd;
            _form.Load += OnFormLoad;
            _trayIconManager = new TrayIconManager(_appIcon);
            SetupFormProperties();
            _layoutManager.ApplyLayout();
            SetupEventHandlers();
            InitializeShipTab(); // This was the missing call
        }

        public void OnFormLoad(object? sender, EventArgs e)
        {
            // The form is now loaded and has its final initial size.
            // We can now correctly adjust the column widths for the welcome message.
            if (_controlFactory?.TabControl != null)
            {
                // Force the Ship tab to be created and have a handle by briefly selecting it.
                // This ensures that controls on it (like the PictureBox) can be invalidated and painted
                // even before the user clicks the tab for the first time.
                var originalIndex = _controlFactory.TabControl.SelectedIndex;
                _controlFactory.TabControl.SelectedIndex = 1; // Index of Ship tab
                _controlFactory.TabControl.SelectedIndex = originalIndex;
            }
            InitializeMaterialsTab();
            InitializeExplorationTab();
        }

        private void InitializeMaterialsTab()
        {
            if (_controlFactory == null || _controlFactory.TabControl.TabPages.ContainsKey("Materials"))
                return;

            var materialsTab = new MaterialsTab
            {
                Name = "Materials",
                Text = "Materials"
            };
            _controlFactory.TabControl.TabPages.Add(materialsTab);
            _controlFactory.MaterialsTab = materialsTab;
        }

        private void InitializeExplorationTab()
        {
            if (_controlFactory == null || _controlFactory.TabControl.TabPages.ContainsKey("Exploration"))
                return;

            var explorationTab = new ExplorationTab(_explorationDataService.Database);
            _controlFactory.TabControl.TabPages.Add(explorationTab);
            _controlFactory.ExplorationTab = explorationTab;

            // Wire up exploration service events to update the tab
            _explorationDataService.SystemDataChanged += (sender, data) =>
            {
                explorationTab.UpdateSystemData(data);
                // When data for the current system changes (e.g., new scan),
                // refresh the historical log to reflect the latest saved state. This was missing.
                explorationTab.RefreshLog();
            };

            _explorationDataService.SessionDataChanged += (sender, data) =>
            {
                explorationTab.UpdateSessionData(data);
            };
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            if (_form?.WindowState == FormWindowState.Minimized)
            {
                _form.Hide();
                _trayIconManager?.ShowBalloonTip(1000, "Elite Data Relay", "Minimized to tray.", ToolTipIcon.Info);
            }
            else if (_form?.WindowState == FormWindowState.Normal || _form?.WindowState == FormWindowState.Maximized)
            {
            }
        }

        private void OnFormResizeEnd(object? sender, EventArgs e)
        {
            UpdateCargoScrollBar();
        }

        private void InitializeIcon()
        {
            try
            {
                // Create a MemoryStream from the icon resource. This stream must be kept open
                // for the lifetime of the Icon object. We store it in a field and dispose of
                // it when the UI is disposed. This prevents heap corruption (0xc0000374) that
                // can occur if the stream is garbage collected while the Icon is still in use.
                _iconStream = new MemoryStream(Properties.Resources.AppIcon);
                _appIcon = new Icon(_iconStream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error initializing application icon: {ex}");
                // If icon fails to load, _appIcon will remain null, and the form/tray will use defaults.
            }
        }

        private void SetupFormProperties()
        {
            if (_form == null) return;

            // Basic form properties
            _form.Text = "Elite Data Relay";
            _form.Padding = Padding.Empty;
            _baseTitle = _form.Text;
            UpdateFullTitleText();

            // Make the window a fixed size and not resizable.
            _form.FormBorderStyle = FormBorderStyle.FixedSingle;
            _form.Size = new Size(800, 540);

            // Set application icon from pre-loaded resource
            if (_appIcon != null) _form.Icon = _appIcon;
        }

        private void SetupEventHandlers()
        {
            if (_controlFactory == null) return;

            _controlFactory.StartBtn.Click += (s, e) => StartClicked?.Invoke(s, e);
            _controlFactory.StopBtn.Click += (s, e) => StopClicked?.Invoke(s, e);
            _controlFactory.ExitBtn.Click += (s, e) => ExitClicked?.Invoke(s, e);
            _controlFactory.SettingsBtn.Click += (s, e) => SettingsClicked?.Invoke(s, e);
            _controlFactory.SessionBtn.Click += (s, e) => SessionClicked?.Invoke(s, e);
            _controlFactory.AboutBtn.Click += (s, e) => AboutClicked?.Invoke(s, e);
            _controlFactory.MiningSessionPanel.StartMiningClicked += OnMiningStartClicked;
            _controlFactory.MiningSessionPanel.StopMiningClicked += OnMiningStopClicked;

            // Tray icon event handlers
            if (_trayIconManager != null)
            {
                _trayIconManager.ShowApplicationClicked += OnShowApplication;
                _trayIconManager.StartClicked += (s, e) => StartClicked?.Invoke(s, e);
                _trayIconManager.StopClicked += (s, e) => StopClicked?.Invoke(s, e);
                _trayIconManager.ExitClicked += (s, e) => ExitClicked?.Invoke(s, e);
            }
        }

        public void OnShowApplication(object? sender, EventArgs e)
        {
            ShowForm();
        }

        public void ShowForm()
        {
            if (_form == null) return;

            _form.Show();
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
        }

        public void OnMiningStartClicked(object? sender, EventArgs e) => MiningStartClicked?.Invoke(sender, e);

        public void OnMiningStopClicked(object? sender, EventArgs e) => MiningStopClicked?.Invoke(sender, e);

        public void UpdateSystemInfo(SystemInfoData data)
        {
            _overlayService?.UpdateSystemInfo(data);
        }

        public void UpdateStationInfo(StationInfoData data)
        {
            _overlayService?.UpdateStationInfo(data);
        }

        public void UpdateMaterials(MaterialsEvent materials)
        {
            _controlFactory?.MaterialsTab?.UpdateAllMaterials(materials.Raw, materials.Manufactured, materials.Encoded);
        }

        public void AppendMiningAnnouncement(MiningNotificationEventArgs notification)
        {
            // Only show announcements if the user has enabled them in settings.
            if (AppConfiguration.EnableMiningAnnouncements)
            {
                _controlFactory?.MiningSessionPanel?.AddAnnouncement(notification);
            }
        }

        public void ShowMiningNotification(MiningNotificationEventArgs notification)
        {
            if (_trayIconManager == null) return;

            // Only show a "Cargo Full" notification if the user has enabled it.
            if (notification.Type == MiningNotificationType.CargoFull && !AppConfiguration.NotifyOnCargoFull)
            {
                return;
            }

            // Only show tray notifications for specific, user-facing events.
            // The 'Info' type is for UI announcements only.
            if (notification.Type == MiningNotificationType.Info)
            {
                return;
            }


            var icon = notification.Type switch // This switch is now exhaustive
            {
                MiningNotificationType.CargoFull => ToolTipIcon.Warning,
                MiningNotificationType.BackupCreated => ToolTipIcon.Info,
                MiningNotificationType.BackupRestored => ToolTipIcon.Info,
                MiningNotificationType.AutoStart => ToolTipIcon.Info,
                MiningNotificationType.ReportGenerated => ToolTipIcon.Info,
                MiningNotificationType.Reminder => ToolTipIcon.Warning,
                MiningNotificationType.ValuableCommodityRefined => ToolTipIcon.Info,
                _ => ToolTipIcon.None // Default case for Info and any future types
            };

            _trayIconManager.ShowBalloonTip(3000, "Elite Data Relay", notification.Message, icon);
        }

        public void UpdateSessionOverlay(int cargoCollected, long creditsEarned)
        {
            _overlayService?.UpdateSessionOverlay(cargoCollected, creditsEarned);
        }

        public void ShowOverlays()
        {
            _overlayService?.Show();
        }

        public void HideOverlays()
        {
            _overlayService?.Hide();
        }

        public void UpdateMonitoringVisuals(bool isMonitoring)
        {
            _isMonitoring = isMonitoring;
            UpdateFullTitleText();

            _trayIconManager?.SetMonitoringState(startEnabled: !isMonitoring, stopEnabled: isMonitoring);
            _watchingAnimationManager?.SetMonitoringState(isMonitoring);
            _overlayService?.SetVisibility(isMonitoring);

            if (_controlFactory?.CargoWelcomePanel != null && _controlFactory.CargoGridView != null)
            {
                _controlFactory.CargoWelcomePanel.Visible = !isMonitoring;
                _controlFactory.CargoGridView.Visible = isMonitoring;
            }
        }

        public void RefreshMiningStats()
        {
            _controlFactory?.MiningSessionPanel?.UpdateStats();
        }

        public void UpdateMiningPreferences(MiningSessionPreferences preferences)
        {
            // This method is no longer used since settings were moved,
            // but is required by the interface.
        }

        public void UpdateCargoScrollBar()
        {
            _controlFactory?.UpdateCargoScrollBar();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _fontManager?.Dispose();
                    _controlFactory?.Dispose();
                    _trayIconManager?.Dispose();
                    _iconStream?.Dispose();
                    _appIcon?.Dispose();
                    _layoutManager?.Dispose();
                    _watchingAnimationManager?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void RefreshExplorationLog()
        {
            _controlFactory?.ExplorationTab?.RefreshLog();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}