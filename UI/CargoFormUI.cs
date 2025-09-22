using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
        private OverlayService? _overlayService;
        private MemoryStream? _iconStream;
        private WatchingAnimationManager? _watchingAnimationManager;
        private string _currentLocation = "Unknown";
        private IMaterialService? _materialServiceCache;

        private string _baseTitle = "";

        public event EventHandler? StartClicked;

        public event EventHandler? StopClicked;

        public event EventHandler? ExitClicked;

        public event EventHandler? AboutClicked;

        public event EventHandler? SettingsClicked;

        public event EventHandler? SessionClicked;

        public void InitializeUI(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _form.Resize += OnFormResize;
            _form.Load += OnFormLoad;

            InitializeIcon();
            _trayIconManager = new TrayIconManager(_appIcon);
            _fontManager = new FontManager();
            _controlFactory = new ControlFactory(_fontManager);
            _overlayService = new OverlayService();

            if (_controlFactory.WatchingLabel != null)
            {
                _watchingAnimationManager = new WatchingAnimationManager(_controlFactory.WatchingLabel);
            }

            // The layout manager now adds the TabControl instead of the ListView directly.
            // Assuming LayoutManager is adapted to add _controlFactory.TabControl to the form's main panel.
            _layoutManager = new LayoutManager(_form, _controlFactory); 

            SetupFormProperties();
            _layoutManager.ApplyLayout();
            SetupEventHandlers();
            DisplayWelcomeMessage();
            UpdateMaterialSearchAutocomplete();
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

            // Set a minimum size to prevent the window from becoming too small to be useful.
            _form.MinimumSize = new Size(AppConfiguration.WindowSize.Width, AppConfiguration.WindowSize.Height);

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
            _controlFactory.PinMaterialsCheckBox.CheckedChanged += OnPinMaterialsCheckBoxChanged;
            _controlFactory.MaterialTreeView.AfterCheck += OnMaterialNodeChecked;
            _controlFactory.MaterialSearchBox.TextChanged += OnMaterialSearchChanged;

            // Tray icon event handlers
            if (_trayIconManager != null)
            {
                _trayIconManager.ShowApplicationClicked += OnShowApplication;
                _trayIconManager.StartClicked += (s, e) => StartClicked?.Invoke(s, e);
                _trayIconManager.StopClicked += (s, e) => StopClicked?.Invoke(s, e);
                _trayIconManager.ExitClicked += (s, e) => ExitClicked?.Invoke(s, e);
            }
        }

        private void OnMaterialSearchChanged(object? sender, EventArgs e)
        {
            // When search text changes, we need to re-filter and update the material list.
            // The UpdateMaterialList method already has access to the cached material service.
            if (_materialServiceCache != null)
            {
                UpdateMaterialList(_materialServiceCache);
            }
        }

        private void UpdateMaterialSearchAutocomplete()
        {
            if (_controlFactory == null) return;

            var allMaterials = MaterialDataService.GetAll();
            var collection = new AutoCompleteStringCollection();
            // Use the localised name if available, otherwise the fallback name.
            var materialNames = allMaterials.Select(m => !string.IsNullOrEmpty(m.LocalisedName) ? m.LocalisedName : m.Name).ToArray();
            collection.AddRange(materialNames);
            _controlFactory.MaterialSearchBox.AutoCompleteCustomSource = collection;
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

        public void Dispose()
        {
            _fontManager?.Dispose();
            _controlFactory?.Dispose();
            _layoutManager?.Dispose();
            _trayIconManager?.Dispose();
            _watchingAnimationManager?.Dispose();
            _overlayService?.Dispose();
            _appIcon?.Dispose();
            _iconStream?.Dispose();
        }
    }
}