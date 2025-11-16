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
        private Icon? _appIcon;
        private LayoutManager? _layoutManager;
        private readonly OverlayService _overlayService;
        private readonly SessionTrackingService _sessionTrackingService;
        private readonly MiningTrackerService _miningTrackerService;
        private readonly ExplorationDataService _explorationDataService;
        private readonly FleetCarrierTrackerService _fleetCarrierTrackerService;
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

        public CargoFormUI(OverlayService overlayService, SessionTrackingService sessionTrackingService, ExplorationDataService explorationDataService, FleetCarrierTrackerService fleetCarrierTrackerService, MiningTrackerService miningTrackerService)
        {
            _overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));
            _sessionTrackingService = sessionTrackingService ?? throw new ArgumentNullException(nameof(sessionTrackingService));
            _explorationDataService = explorationDataService ?? throw new ArgumentNullException(nameof(explorationDataService));
            _fleetCarrierTrackerService = fleetCarrierTrackerService ?? throw new ArgumentNullException(nameof(fleetCarrierTrackerService));
            _miningTrackerService = miningTrackerService ?? throw new ArgumentNullException(nameof(miningTrackerService));
        }

        public void InitializeUI(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            InitializeIcon();
            _fontManager = new FontManager();
            _controlFactory = new ControlFactory(_fontManager, _sessionTrackingService, _fleetCarrierTrackerService, _miningTrackerService);

            if (_controlFactory.WatchingLabel != null)
            {
                _watchingAnimationManager = new WatchingAnimationManager(_controlFactory.WatchingLabel);
            }

            _layoutManager = new LayoutManager(_form, _controlFactory);

            _form.ResizeEnd += OnFormResizeEnd;
            _form.Load += OnFormLoad;
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
                // Update exploration overlay
                _overlayService.UpdateExplorationData(data);
            };

            _explorationDataService.SessionDataChanged += (sender, data) =>
            {
                explorationTab.UpdateSessionData(data);
                // Update exploration overlay with session data
                _overlayService.UpdateExplorationSessionData(data);
            };
        }

        private void OnFormResizeEnd(object? sender, EventArgs e)
        {
            UpdateCargoScrollBar();
        }

        private void InitializeIcon()
        {
            try
            {
                // Prefer freshly-rendered app icon reflecting current theme colors
                _appIcon = AppIconFactory.CreateAppIcon(32);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error generating app icon, falling back to resource: {ex}");
                try
                {
                    _iconStream = new MemoryStream(Properties.Resources.AppIcon);
                    _appIcon = new Icon(_iconStream);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Resource icon load failed: {ex2}");
                }
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
            _form.Size = new Size(1000, 800);

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
            _controlFactory.AboutBtn.Click += (s, e) => AboutClicked?.Invoke(s, e);

            // Tray icon event handlers
        }

        public void ShowForm()
        {
            if (_form == null) return;

            _form.Show();
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
        }

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

        public void ShowInfoNotification(string title, string message)
        {
            ShowInfoPopup(title, message);
        }

        public void ShowInfoPopup(string title, string message)
        {
            if (_form != null)
            {
                MessageBox.Show(_form, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void RefreshOverlay(Form owner)
        {
            _overlayService.Start(owner);
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

            _watchingAnimationManager?.SetMonitoringState(isMonitoring);
            _overlayService?.SetVisibility(isMonitoring);

            if (_controlFactory?.CargoWelcomePanel != null && _controlFactory.CargoGridView != null)
            {
                _controlFactory.CargoWelcomePanel.Visible = !isMonitoring;
                _controlFactory.CargoGridView.Visible = isMonitoring;
            }
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
                    _controlFactory?.Dispose();
                    _fontManager?.Dispose();
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

        public void UpdateExplorationCurrentSystem(SystemExplorationData data)
        {
            _controlFactory?.ExplorationTab?.UpdateSystemData(data);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
