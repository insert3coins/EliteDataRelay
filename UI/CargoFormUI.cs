using System;
using System.Drawing;
using System.Collections.Generic;
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
            _controlFactory.SessionBtn.Click += (s, e) => SessionClicked?.Invoke(s, e);
            _controlFactory.AboutBtn.Click += (s, e) => AboutClicked?.Invoke(s, e);
            _controlFactory.PinMaterialsCheckBox.CheckedChanged += OnPinMaterialsCheckBoxChanged;
            _controlFactory.MaterialTreeView.AfterCheck += OnMaterialNodeChecked;

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

        private void OnPinMaterialsCheckBoxChanged(object? sender, EventArgs e)
        {
            // If the user changes the pin setting, refresh the list using the cached data.
            if (_materialServiceCache != null)
            {
                UpdateMaterialList(_materialServiceCache);
            }
        }

        private void OnMaterialNodeChecked(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is not string materialName || e.Action == TreeViewAction.Unknown)
            {
                return;
            }

            var pinnedMaterials = AppConfiguration.PinnedMaterials.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            if (e.Node.Checked)
            {
                pinnedMaterials.Add(materialName);
            }
            else
            {
                pinnedMaterials.Remove(materialName);
            }

            AppConfiguration.PinnedMaterials = pinnedMaterials.ToList();
            AppConfiguration.Save();

            // If we are in "pinned only" view, unchecking an item should make it disappear.
            if (_controlFactory != null && _controlFactory.PinMaterialsCheckBox.Checked)
            {
                UpdateMaterialList(_materialServiceCache);
            }
        }
        
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

        public void UpdateMaterialList(IMaterialService materialService)
        {
            _materialServiceCache = materialService; // Cache the latest service instance

            if (_controlFactory == null) return;

            var treeView = _controlFactory.MaterialTreeView;
            // Unhook the event handler while we programmatically update the checked states to prevent it from firing.
            treeView.AfterCheck -= OnMaterialNodeChecked;

            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            var rawNode = treeView.Nodes.Add("Raw");
            var manufacturedNode = treeView.Nodes.Add("Manufactured");
            var encodedNode = treeView.Nodes.Add("Encoded");

            if (_controlFactory.PinMaterialsCheckBox.Checked)
            {
                var pinnedRaw = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);
                var pinnedManufactured = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);
                var pinnedEncoded = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);

                var allMaterialDefinitions = MaterialDataService.GetAll().ToDictionary(m => m.Name, m => m, StringComparer.InvariantCultureIgnoreCase);

                foreach (var pinnedMaterialName in AppConfiguration.PinnedMaterials)
                {
                    if (allMaterialDefinitions.TryGetValue(pinnedMaterialName, out var def))
                    {
                        materialService.RawMaterials.TryGetValue(pinnedMaterialName, out var existingRaw);
                        materialService.ManufacturedMaterials.TryGetValue(pinnedMaterialName, out var existingManufactured);
                        materialService.EncodedMaterials.TryGetValue(pinnedMaterialName, out var existingEncoded);

                        var itemToShow = existingRaw ?? existingManufactured ?? existingEncoded ?? new MaterialItem { Name = def.Name, Localised = def.LocalisedName, Count = 0 };

                        switch (def.Category.ToLowerInvariant())
                        {
                            case "raw": pinnedRaw[def.Name] = itemToShow; break;
                            case "manufactured": pinnedManufactured[def.Name] = itemToShow; break;
                            case "encoded": pinnedEncoded[def.Name] = itemToShow; break;
                        }
                    }
                }
                PopulateMaterialCategory(rawNode, pinnedRaw);
                PopulateMaterialCategory(manufacturedNode, pinnedManufactured);
                PopulateMaterialCategory(encodedNode, pinnedEncoded);
            }
            else
            {
                // We want to show all materials the player has, PLUS any materials that are pinned but have a count of 0.
                // This ensures that a user can always see (and un-pin) a pinned material from this view.
                var allMaterialDefinitions = MaterialDataService.GetAll().ToDictionary(m => m.Name, m => m, StringComparer.InvariantCultureIgnoreCase);

                // Create mutable copies of the player's current materials to augment them.
                var rawWithPinned = new Dictionary<string, MaterialItem>(materialService.RawMaterials, StringComparer.InvariantCultureIgnoreCase);
                var manufacturedWithPinned = new Dictionary<string, MaterialItem>(materialService.ManufacturedMaterials, StringComparer.InvariantCultureIgnoreCase);
                var encodedWithPinned = new Dictionary<string, MaterialItem>(materialService.EncodedMaterials, StringComparer.InvariantCultureIgnoreCase);

                foreach (var pinnedMaterialName in AppConfiguration.PinnedMaterials)
                {
                    // If the pinned material is not already in one of the lists (which means its count is > 0),
                    // then we need to add it with a count of 0 so it's visible.
                    if (!rawWithPinned.ContainsKey(pinnedMaterialName) &&
                        !manufacturedWithPinned.ContainsKey(pinnedMaterialName) &&
                        !encodedWithPinned.ContainsKey(pinnedMaterialName))
                    {
                        if (allMaterialDefinitions.TryGetValue(pinnedMaterialName, out var def))
                        {
                            var zeroCountItem = new MaterialItem { Name = def.Name, Localised = def.LocalisedName, Count = 0 };
                            switch (def.Category.ToLowerInvariant())
                            {
                                case "raw": rawWithPinned[def.Name] = zeroCountItem; break;
                                case "manufactured": manufacturedWithPinned[def.Name] = zeroCountItem; break;
                                case "encoded": encodedWithPinned[def.Name] = zeroCountItem; break;
                            }
                        }
                    }
                }
                PopulateMaterialCategory(rawNode, rawWithPinned);
                PopulateMaterialCategory(manufacturedNode, manufacturedWithPinned);
                PopulateMaterialCategory(encodedNode, encodedWithPinned);
            }

            rawNode.Expand();
            manufacturedNode.Expand();
            encodedNode.Expand();

            treeView.EndUpdate();

            // Re-hook the event handler.
            treeView.AfterCheck += OnMaterialNodeChecked;
        }

        public void UpdateMaterialsOverlay(IMaterialService materialService)
        {
            _materialServiceCache = materialService; // Also cache here for overlay refreshes
            _overlayService?.UpdateMaterials(materialService);
        }

        private void PopulateMaterialCategory(TreeNode categoryNode, IReadOnlyDictionary<string, MaterialItem> materials)
        {
            var pinned = AppConfiguration.PinnedMaterials.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            if (!materials.Any())
            {
                categoryNode.Nodes.Add("None").ForeColor = SystemColors.GrayText;
                return;
            }

            foreach (var material in materials.Values.OrderBy(m => m.Localised ?? m.Name))
            {
                string displayName = !string.IsNullOrEmpty(material.Localised) ? char.ToUpper(material.Localised[0]) + material.Localised.Substring(1) : material.Name;
                int maxCount = MaterialDataService.GetMaxCount(material.Name);
                string text = maxCount > 0 ? $"{displayName} ({material.Count} / {maxCount})" : $"{displayName} ({material.Count})";

                var node = categoryNode.Nodes.Add(text);
                node.Tag = material.Name;
                node.Checked = pinned.Contains(material.Name);

                if (maxCount > 0 && material.Count >= maxCount)
                {
                    node.ForeColor = Color.Orange; // Visual indicator for max capacity
                }
            }
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

            // The Session button should always be visible, but only enabled when monitoring is
            // active and the feature is enabled in settings.
            if (_controlFactory.SessionBtn != null)
            {
                _controlFactory.SessionBtn.Enabled = stopEnabled && AppConfiguration.EnableSessionTracking;
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
                    if (AppConfiguration.EnableLeftOverlay || AppConfiguration.EnableRightOverlay || AppConfiguration.EnableMaterialsOverlay)
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
            if (AppConfiguration.EnableLeftOverlay || AppConfiguration.EnableRightOverlay || AppConfiguration.EnableMaterialsOverlay)
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

        public void UpdateSessionOverlay(long cargoCollected, long creditsEarned)
        {
            // This assumes the OverlayService has a corresponding method
            // that will format the data and pass it to the overlay form.
            _overlayService?.UpdateSessionOverlay(cargoCollected, creditsEarned);
        }
    }
}