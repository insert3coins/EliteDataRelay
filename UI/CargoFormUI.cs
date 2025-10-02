using System;
using System.Drawing;
using System.IO;
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
        private MemoryStream? _iconStream;
        private WatchingAnimationManager? _watchingAnimationManager;
        private string _currentLocation = "Unknown";        

        private string _baseTitle = "";

        public event EventHandler? StartClicked;

        public event EventHandler? StopClicked;

        public event EventHandler? ExitClicked;

        public event EventHandler? AboutClicked;

        public event EventHandler? SettingsClicked;

        public event EventHandler? SessionClicked;

        public CargoFormUI(OverlayService overlayService, SessionTrackingService sessionTrackingService)
        {
            _overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));
            _sessionTrackingService = sessionTrackingService ?? throw new ArgumentNullException(nameof(sessionTrackingService));
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

            // The layout manager now adds the TabControl instead of the ListView directly.
            // Assuming LayoutManager is adapted to add _controlFactory.TabControl to the form's main panel.
            _layoutManager = new LayoutManager(_form, _controlFactory);

            _form.Resize += OnFormResize;
            _form.Load += OnFormLoad;
            _trayIconManager = new TrayIconManager(_appIcon);
            SetupFormProperties();
            _layoutManager.ApplyLayout();
            SetupEventHandlers();
            DisplayWelcomeMessage();
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // The form is now loaded and has its final initial size.
            // We can now correctly adjust the column widths for the welcome message.
            AdjustMessageColumnLayout();
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
            _form.Text = "Elite Data Relay – Stopped";
            _form.Padding = Padding.Empty;
            _baseTitle = _form.Text;
            UpdateFullTitleText();

            // Make the window a fixed size and not resizable.
            _form.FormBorderStyle = FormBorderStyle.FixedSingle;
            _form.Size = new Size(800, 450);

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

            // Tray icon event handlers
            if (_trayIconManager != null)
            {
                _trayIconManager.ShowApplicationClicked += OnShowApplication;
                _trayIconManager.StartClicked += (s, e) => StartClicked?.Invoke(s, e);
                _trayIconManager.StopClicked += (s, e) => StopClicked?.Invoke(s, e);
                _trayIconManager.ExitClicked += (s, e) => ExitClicked?.Invoke(s, e);
            }
        }

        private void OnShowApplication(object? sender, EventArgs e)
        {
            ShowForm();
        }

        private void ShowForm()
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

        public void UpdateMiningStats()
        {
            _controlFactory?.MiningStatsControl?.UpdateLabels();
        }

        public void Dispose()
        {
            _controlFactory?.Dispose();
            _layoutManager?.Dispose();
            _trayIconManager?.Dispose();
            _watchingAnimationManager?.Dispose();
            _fontManager?.Dispose(); // Dispose fonts after the controls that use them.
            _appIcon?.Dispose();
            _iconStream?.Dispose();
        }
    }
}