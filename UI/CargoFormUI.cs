using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public class CargoFormUI : ICargoFormUI
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

        private string _baseTitle = "";

        public event EventHandler? StartClicked;

        public event EventHandler? StopClicked;

        public event EventHandler? ExitClicked;

        public event EventHandler? AboutClicked;

        public event EventHandler? SettingsClicked;

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
            _layoutManager = new LayoutManager(_form, _controlFactory);

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

            // Set a minimum size to prevent the window from becoming too small to be useful.
            _form.MinimumSize = new Size(AppConfiguration.FormWidth, AppConfiguration.FormHeight);

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
            if (_trayIconManager != null)
            {
                _trayIconManager.ShowApplicationClicked += OnShowApplication;
                _trayIconManager.StartClicked += (s, e) => StartClicked?.Invoke(s, e);
                _trayIconManager.StopClicked += (s, e) => StopClicked?.Invoke(s, e);
                _trayIconManager.ExitClicked += (s, e) => ExitClicked?.Invoke(s, e);
            }
        }

        private void OnShowApplication(object? sender, EventArgs e) => ShowForm();
        
        private void DisplayWelcomeMessage()
        {
            if (_controlFactory == null) return;
            var listView = _controlFactory.ListView;

            listView.Items.Clear();
            var welcomeItem = new ListViewItem(AppConfiguration.WelcomeMessage);
            welcomeItem.ForeColor = SystemColors.GrayText;
            listView.Items.Add(welcomeItem);

            // The layout is intentionally not adjusted here, as the ListView's ClientSize
            // may not be accurate until the form is loaded. The OnFormLoad event
            // will handle the initial layout adjustment.
            _controlFactory.CargoHeaderLabel.Text = "Cargo: 0";
        }

        /// <summary>
        /// Adjusts the ListView columns to display a single, full-width message (e.g., welcome or empty).
        /// </summary>
        private void AdjustMessageColumnLayout()
        {
            if (_controlFactory == null) return;
            var listView = _controlFactory.ListView;

            listView.Columns[0].Width = listView.ClientSize.Width;
            listView.Columns[1].Width = 0;
            listView.Columns[2].Width = 0;
        }

        /// <summary>
        /// Restores the ListView columns to their default state for displaying cargo data.
        /// </summary>
        private void RestoreDataColumnLayout()
        {
            if (_controlFactory == null) return;
            var listView = _controlFactory.ListView;
            listView.Columns[0].Width = 200;
            listView.Columns[1].Width = 80;
            listView.Columns[2].Width = -2; // Fill remaining space
        }

        public void UpdateCargoHeader(int currentCount, int? capacity)
        {
            if (_controlFactory == null) return;

            string headerText = capacity.HasValue
                ? $"Cargo: {currentCount}/{capacity.Value}"
                : $"Cargo: {currentCount}";
            _controlFactory.CargoHeaderLabel.Text = headerText;
            _overlayService?.UpdateCargo(currentCount, capacity);
        }

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            if (_controlFactory == null) return;

            var listView = _controlFactory.ListView;

            listView.BeginUpdate();
            listView.Items.Clear();

            if (snapshot.Inventory.Any())
            {
                RestoreDataColumnLayout(); // Set columns for data view
                var sortedInventory = snapshot.Inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name);
                foreach (var item in sortedInventory)
                {
                    string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;

                    // Capitalize the first letter for consistent display.
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                    }

                    var listViewItem = new ListViewItem(displayName);
                    listViewItem.SubItems.Add(item.Count.ToString());
                    string category = CommodityDataService.GetCategory(item.Name);
                    listViewItem.SubItems.Add(category);
                    listView.Items.Add(listViewItem);
                }
            }
            else
            {
                var emptyItem = new ListViewItem("Cargo hold is empty.") { ForeColor = SystemColors.GrayText };
                listView.Items.Add(emptyItem);
                AdjustMessageColumnLayout(); // Set columns for single message view
            }
            listView.EndUpdate();

            _overlayService?.UpdateCargoList(snapshot);
        }

        public void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity)
        {
            if (_controlFactory == null) return;

            int count = snapshot.Inventory.Sum(item => item.Count);
            int index = 0;

            // Calculate index based on percentage if capacity is known
            if (cargoCapacity is > 0)
            {
                double percentage = (double)count / cargoCapacity.Value;
                percentage = Math.Clamp(percentage, 0.0, 1.0);

                index = (int)Math.Round(percentage * (UIConstants.CargoSize.Length - 1));
                index = Math.Clamp(index, 0, UIConstants.CargoSize.Length - 1);
            }

            _controlFactory.CargoSizeLabel.Text = UIConstants.CargoSize[index];
            _overlayService?.UpdateCargoSize(UIConstants.CargoSize[index]);
        }

        public void UpdateLocation(string starSystem)
        {
            _currentLocation = starSystem;

            UpdateFullTitleText();
        }

        public void UpdateCommanderName(string commanderName)
        {
            if (_controlFactory?.CommanderLabel != null)
            {
                var cmdrText = $"CMDR: {commanderName}";
                _controlFactory.CommanderLabel.Text = cmdrText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.CommanderLabel, cmdrText);
                _overlayService?.UpdateCommander(commanderName);
            }
        }

        public void UpdateShipInfo(string shipName, string shipIdent)
        {
            if (_controlFactory?.ShipLabel != null)
            {
                // Display the ship name and on our tool tip give the full version
                _controlFactory.ShipLabel.Text = $"Ship: {shipIdent}";

                // Update the tooltip to match the full text
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipLabel, $"Ship: {shipName} ({shipIdent})");
                _overlayService?.UpdateShip(shipIdent);
            }
        }

        public void UpdateBalance(long balance)
        {
            if (_controlFactory?.BalanceLabel != null)
            {
                var balanceText = $"Balance: {balance:N0} CR";
                _controlFactory.BalanceLabel.Text = balanceText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.BalanceLabel, balanceText);
                _overlayService?.UpdateBalance(balance);
            }
        }

        public void UpdateTitle(string title)
        {
            _baseTitle = title;

            UpdateFullTitleText();
        }

        public void SetButtonStates(bool startEnabled, bool stopEnabled)
        {
            if (_controlFactory == null) return;

            if (_controlFactory.StartBtn != null)
            {
                _controlFactory.StartBtn.Enabled = startEnabled;
                // Use a different background color to indicate this is the primary action.
                _controlFactory.StartBtn.BackColor = startEnabled ? UIConstants.StartButtonActiveColor : UIConstants.DefaultButtonBackColor;
            }

            if (_controlFactory.StopBtn != null)
            {
                _controlFactory.StopBtn.Enabled = stopEnabled;
                // Use a different background color to indicate this is the primary action.
                _controlFactory.StopBtn.BackColor = stopEnabled ? UIConstants.StopButtonActiveColor : UIConstants.DefaultButtonBackColor;
            }

            // Also update tray menu items
            _trayIconManager?.SetMonitoringState(startEnabled, stopEnabled);
            // Control the animation
            if (_watchingAnimationManager != null)
            {
                if (stopEnabled) // This means monitoring is now active
                {
                    _watchingAnimationManager.Start();
                    // Only start the overlay service if at least one of the overlays is enabled.
                    if (AppConfiguration.EnableLeftOverlay || AppConfiguration.EnableRightOverlay)
                    {
                        _overlayService?.Start();
                    }
                }
                else // Monitoring is stopped
                {
                    _watchingAnimationManager.Stop();
                    // When monitoring stops, just hide the overlay. It will be destroyed on exit.
                    _overlayService?.Hide();
                }
            }
        }

        private void UpdateFullTitleText()
        {
            if (_form == null) return;

            _form.Text = $"{_baseTitle} - Location: {_currentLocation}";
        }

        public void RefreshOverlay()
        {
            _overlayService?.Stop();
            if (AppConfiguration.EnableLeftOverlay || AppConfiguration.EnableRightOverlay)
            {
                _overlayService?.Start();
            }
        }

        public void ShowOverlays()
        {
            _overlayService?.Show();
        }

        public void HideOverlays()
        {
            _overlayService?.Hide();
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