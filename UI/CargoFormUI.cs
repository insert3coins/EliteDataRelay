using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models.Market;

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

        public event EventHandler? MiningStartClicked;

        public event EventHandler? MiningStopClicked;

        public event EventHandler? TradeFindBestSellClicked;

        public event EventHandler? TradeFindBestBuyClicked;

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
            _controlFactory = new ControlFactory(_fontManager, _sessionTrackingService, this);

            if (_controlFactory.WatchingLabel != null)
            {
                _watchingAnimationManager = new WatchingAnimationManager(_controlFactory.WatchingLabel);
            }

            _layoutManager = new LayoutManager(_form, _controlFactory);

            _form.Resize += OnFormResize;
            _form.Load += OnFormLoad;
            _trayIconManager = new TrayIconManager(_appIcon);
            SetupFormProperties();
            _layoutManager.ApplyLayout();
            SetupEventHandlers();
            DisplayWelcomeMessage();
            PopulateInitialCommodities();
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
            _form.Text = "Elite Data Relay";
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
            _controlFactory.MiningSessionPanel.StartMiningClicked += OnMiningStartClicked;
            _controlFactory.MiningSessionPanel.StopMiningClicked += OnMiningStopClicked;
            _controlFactory.TradeFindBestSellButton.Click += (s, e) => TradeFindBestSellClicked?.Invoke(s, e); // This should call the SELL handler
            _controlFactory.TradeFindBestBuyButton.Click += (s, e) => TradeFindBestBuyClicked?.Invoke(s, e);   // This should call the BUY handler

            _controlFactory.TradeCommodityComboBox.TextChanged += OnTradeCommodityChanged;

            // Tray icon event handlers
            if (_trayIconManager != null)
            {
                _trayIconManager.ShowApplicationClicked += OnShowApplication;
                _trayIconManager.StartClicked += (s, e) => StartClicked?.Invoke(s, e);
                _trayIconManager.StopClicked += (s, e) => StopClicked?.Invoke(s, e);
                _trayIconManager.ExitClicked += (s, e) => ExitClicked?.Invoke(s, e);
            }
        }

        public void OnTradeCommodityChanged(object? sender, EventArgs e)
        {
            if (_controlFactory == null) return;

            // Buttons should only be enabled if a commodity is selected AND the location is known.
            bool isCommoditySelected = !string.IsNullOrWhiteSpace(_controlFactory.TradeCommodityComboBox.Text);
            bool isLocationKnown = _currentLocation != "Unknown";
            _controlFactory.TradeFindBestSellButton.Enabled = isCommoditySelected && isLocationKnown;
            _controlFactory.TradeFindBestBuyButton.Enabled = isCommoditySelected && isLocationKnown;
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

        public void UpdateMiningStats()
        {
            _controlFactory?.MiningSessionPanel?.UpdateStats();
        }

        public string? GetSelectedTradeCommodity() => _controlFactory?.TradeCommodityComboBox.Text;

        public void PopulateCommodities(System.Collections.Generic.IEnumerable<string> commodities)
        {
            if (_controlFactory == null) return;

            var comboBox = _controlFactory.TradeCommodityComboBox;
            comboBox.Items.Clear();
            comboBox.Items.AddRange(commodities.ToArray());
        }

        private void PopulateInitialCommodities()
        {
            var allCommodities = ItemNameService.GetAllCommodityNames().OrderBy(c => c);
            PopulateCommodities(allCommodities);
        }

        public void UpdateTradeResults(System.Collections.Generic.List<MarketInfo> results, bool isSellSearch)
        {
            if (_controlFactory == null) return;
            Trace.WriteLine($"[TradeUI] Received {results.Count} results to display.");

            var listView = _controlFactory.TradeResultsListView;
            listView.BeginUpdate();
            listView.Items.Clear();

            if (results.Any())
            {
                foreach (var result in results)
                {
                    // For a SELL search, we care about the station's BUY price and its DEMAND.
                    // For a BUY search, we care about the station's SELL price and its STOCK.
                    var price = result.Commodity?.SellPrice; // Per request, always show the sell price.
                    var supplyDemand = isSellSearch ? result.Commodity?.Demand : result.Commodity?.Stock; // Keep demand for sell, stock for buy.

                    var displayName = result.StationName;
                    Trace.WriteLine($"[TradeUI]   -> Adding station: {displayName}, Price: {price}, Supply/Demand: {supplyDemand}");

                    var item = new ListViewItem(displayName); // The first column is now just the station name.
                    item.SubItems.Add($"{price:N0} cr");
                    item.SubItems.Add($"{supplyDemand:N0}");
                    item.SubItems.Add($"{result.DistanceToArrival:F2} LY");
                    listView.Items.Add(item);
                }
                SetTradeStatus($"Found {results.Count} stations.");
            }
            else
            {
                if (isSellSearch)
                {
                    SetTradeStatus("No stations buying this commodity were found in this area.");
                }
                else
                {
                    SetTradeStatus("No stations selling this commodity were found in this area.");
                }
            }

            listView.EndUpdate();
        }

        public void SetTradeStatus(string text)
        {
            if (_controlFactory?.TradeStatusLabel != null)
            {
                if (_currentLocation == "Unknown")
                {
                    _controlFactory.TradeStatusLabel.Text = "Waiting for location data... Start monitoring to update.";
                }
                else
                {
                    _controlFactory.TradeStatusLabel.Text = text;
                }
            }
        }

        public void StartTradeSearchAnimation()
        {
            _watchingAnimationManager?.Start();
            if (_controlFactory != null)
            {
                _controlFactory.TradeFindBestSellButton.Enabled = false;
                _controlFactory.TradeFindBestBuyButton.Enabled = false;
            }
        }

        public void StopTradeSearchAnimation()
        {
            // Only stop the animation if the main file monitoring is not active.
            // This prevents the trade search from stopping the "Watching..." animation
            // if the main file monitoring is also running.
            _watchingAnimationManager?.StopIfInactive();
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
            _trayIconManager?.SetMonitoringState(startEnabled: !isMonitoring, stopEnabled: isMonitoring);
            _watchingAnimationManager?.SetMonitoringState(isMonitoring);
            _overlayService?.SetVisibility(isMonitoring);
        }

        public void Dispose()
        {
            _controlFactory?.Dispose();
            _layoutManager?.Dispose();
            _trayIconManager?.Dispose();
            _watchingAnimationManager?.Dispose();
            // _shipWireframeDrawer is removed, no need to dispose.
            _fontManager?.Dispose(); // Dispose fonts after the controls that use them.
            _appIcon?.Dispose();
            _iconStream?.Dispose();
        }
    }
}