using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
                // Display the ship name, and on our tool tip, give the full version including the ident.
                _controlFactory.ShipLabel.Text = $"Ship: {shipName}";

                // Update the tooltip to match the full text
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipLabel, $"Ship: {shipName} ({shipIdent})");
                _overlayService?.UpdateShip(shipName, shipIdent);
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