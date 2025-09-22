using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
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
                // If inventory is empty, check if it's because the hold is empty or because we're waiting for an update.
                string message = snapshot.Count > 0 ? "Cargo manifest updating..." : "Cargo hold is empty.";

                var statusItem = new ListViewItem(message) { ForeColor = SystemColors.GrayText };
                listView.Items.Add(statusItem);
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
            string searchTerm = _controlFactory.MaterialSearchBox.Text;

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
                PopulateMaterialCategory(rawNode, FilterMaterials(pinnedRaw, searchTerm));
                PopulateMaterialCategory(manufacturedNode, FilterMaterials(pinnedManufactured, searchTerm));
                PopulateMaterialCategory(encodedNode, FilterMaterials(pinnedEncoded, searchTerm));
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
                PopulateMaterialCategory(rawNode, FilterMaterials(rawWithPinned, searchTerm));
                PopulateMaterialCategory(manufacturedNode, FilterMaterials(manufacturedWithPinned, searchTerm));
                PopulateMaterialCategory(encodedNode, FilterMaterials(encodedWithPinned, searchTerm));
            }

            rawNode.Expand();
            manufacturedNode.Expand();
            encodedNode.Expand();

            treeView.EndUpdate();

            // Re-hook the event handler.
            treeView.AfterCheck += OnMaterialNodeChecked;
        }

        private IReadOnlyDictionary<string, MaterialItem> FilterMaterials(IReadOnlyDictionary<string, MaterialItem> materials, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return materials;
            }

            var filtered = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var material in materials.Values)
            {
                string displayName = material.Localised ?? material.Name;
                if (displayName.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                {
                    filtered[material.Name] = material;
                }
            }
            return filtered;
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

            // Use the explicit Count property, as Inventory might be empty if we only have a total from a Loadout event.
            int count = snapshot.Count;
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

        public void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName)
        {
            if (_controlFactory == null) return;

            // Update the main UI button in the bottom panel
            if (_controlFactory.ShipLabel != null)
            {
                _controlFactory.ShipLabel.Text = $"Ship: {shipType}";
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipLabel, $"Name: {shipName} ({shipIdent})");
            }

            // Update the dedicated controls on the "Ship" tab
            if (_controlFactory.ShipIconPictureBox != null)
            {
                _controlFactory.ShipIconPictureBox.Image = ShipIconService.GetShipIcon(internalShipName);
            }

            if (_controlFactory.ShipNameLabel != null)
            {
                // Display the ship type (e.g., "Krait Mk II") and the user-defined name if it's set.
                string nameLabel = shipType;
                if (!string.IsNullOrEmpty(shipName) && shipName != "N/A" && !shipName.Equals(shipType, StringComparison.OrdinalIgnoreCase))
                {
                    nameLabel = $"{shipType} \"{shipName}\"";
                }
                _controlFactory.ShipNameLabel.Text = nameLabel;
            }

            if (_controlFactory.ShipIdentLabel != null)
            {
                _controlFactory.ShipIdentLabel.Text = $"ID: {shipIdent}";
            }

            // The overlay does not show the image, so no change to this call is needed.
            _overlayService?.UpdateShip(shipName, shipIdent, shipType);
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

        private void UpdateFullTitleText()
        {
            if (_form == null) return;

            _form.Text = $"{_baseTitle} - Location: {_currentLocation}";
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
    }
}