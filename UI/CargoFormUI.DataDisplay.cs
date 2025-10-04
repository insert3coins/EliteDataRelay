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

            if (snapshot.Items.Any())
            {
                RestoreDataColumnLayout(); // Set columns for data view
                var sortedInventory = snapshot.Items.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name);
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

        public void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity)
        {
            // First, update the header and count on the main UI and the overlay
            UpdateCargoHeader(snapshot.Count, cargoCapacity);

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

            // Finally, update the list of items
            UpdateCargoList(snapshot);
        }

        public void UpdateLocation(string starSystem)
        {
            _currentLocation = starSystem;

            UpdateFullTitleText();

            if (_controlFactory?.LocationLabel != null)
            {
                var locationText = $"Location: {starSystem}";
                _controlFactory.LocationLabel.Text = locationText;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.LocationLabel, "Click to copy system name");
            }
            _overlayService?.UpdateLocation(starSystem);
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
            // Display the ship type (e.g., "Krait Mk II") and the user-defined name if it's set.
            string nameLabel = shipType;
            if (!string.IsNullOrEmpty(shipName) && shipName != "N/A" && !shipName.Equals(shipType, StringComparison.OrdinalIgnoreCase))
            {
                nameLabel = $"{shipType} \"{shipName}\"";
            }

            // Update the ship image using the new service
            var shipImage = ShipIconService.GetShipIcon(internalShipName);

            // Always update the overlay service, as the overlay might have been recreated
            // and needs to be populated with the current image.
            _overlayService?.UpdateShipIcon(shipImage);

            // Update the picture box on the Ship tab with the new image.
            _controlFactory.ShipWireframePictureBox.Image = shipImage;

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

            _form.Text = $"{_baseTitle} – Location: {_currentLocation}";
        }

        public void DisplayWelcomeMessage()
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
        public void AdjustMessageColumnLayout()
        {
            if (_controlFactory == null) return;
            var listView = _controlFactory.ListView;

            listView.Columns[0].Width = listView.ClientSize.Width;
            listView.Columns[1].Width = 0;
            listView.Columns[2].Width = 0;
        }

        public void RestoreDataColumnLayout()
        {
            if (_controlFactory == null) return;
            var listView = _controlFactory.ListView;
            listView.Columns[0].Width = 200;
            listView.Columns[1].Width = 80;
            listView.Columns[2].Width = -2; // Fill remaining space
        }
    }
}