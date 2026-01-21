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
        private const string CargoStatusRowTag = "CargoStatusRow";

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

            var gridView = _controlFactory.CargoGridView;
            if (gridView is null) return;

            // Ensure we marshal to UI thread without blocking paint
            if (gridView.InvokeRequired)
            {
                gridView.BeginInvoke(new Action(() => UpdateCargoList(snapshot)));
                return;
            }

            gridView.SuspendLayout();
            try
            {
                gridView.ClearSelection();
                var sortedInventory = snapshot.Items
                    .OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name)
                    .Select(item =>
                    {
                        var displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                        }
                        return new
                        {
                            Name = displayName,
                            item.Count,
                            Category = CommodityDataService.GetCategory(item.Name)
                        };
                    })
                    .ToList();

                if (sortedInventory.Count == 0)
                {
                    gridView.Rows.Clear();

                    string message = snapshot.Count > 0 ? "Cargo manifest updating..." : "Cargo hold is empty.";

                    int rowIndex = gridView.Rows.Add(message, "", "");
                    var row = gridView.Rows[rowIndex];
                    row.Tag = CargoStatusRowTag;

                    var style = new DataGridViewCellStyle(gridView.DefaultCellStyle)
                    {
                        ForeColor = Color.FromArgb(107, 114, 128),
                        Alignment = DataGridViewContentAlignment.MiddleCenter,
                        SelectionBackColor = gridView.DefaultCellStyle.BackColor,
                        SelectionForeColor = Color.FromArgb(107, 114, 128)
                    };
                    row.DefaultCellStyle = style;
                    row.ReadOnly = true;
                }
                else
                {
                    if (gridView.Rows.Count == 1 && gridView.Rows[0].Tag as string == CargoStatusRowTag)
                    {
                        gridView.Rows.Clear();
                    }

                    int index = 0;
                    foreach (var item in sortedInventory)
                    {
                        DataGridViewRow row;
                        if (index < gridView.Rows.Count)
                        {
                            row = gridView.Rows[index];
                        }
                        else
                        {
                            gridView.Rows.Add(item.Name, item.Count, item.Category);
                            row = gridView.Rows[index];
                        }

                        if (!Equals(row.Cells[0].Value, item.Name))
                        {
                            row.Cells[0].Value = item.Name;
                        }
                        if (!Equals(row.Cells[1].Value, item.Count))
                        {
                            row.Cells[1].Value = item.Count;
                        }
                        if (!Equals(row.Cells[2].Value, item.Category))
                        {
                            row.Cells[2].Value = item.Category;
                        }

                        if (row.Tag as string == CargoStatusRowTag)
                        {
                            row.DefaultCellStyle = new DataGridViewCellStyle(gridView.DefaultCellStyle);
                            row.Tag = null;
                        }
                        row.ReadOnly = true;
                        index++;
                    }

                    while (gridView.Rows.Count > sortedInventory.Count)
                    {
                        gridView.Rows.RemoveAt(gridView.Rows.Count - 1);
                    }
                }
            }
            finally
            {
                gridView.ResumeLayout();
            }

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
                _controlFactory.ShipLabel.Cursor = Cursors.Hand;
                _controlFactory.ShipLabel.Click -= OnShipLabelClicked;
                _controlFactory.ShipLabel.Click += OnShipLabelClicked;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipLabel, $"Name: {shipName} ({shipIdent})\nClick to open EDSY");
            }

            // Update the dedicated controls on the "Ship" tab
            // Display the ship type (e.g., "Krait Mk II") and the user-defined name if it's set.
            string nameLabel = shipType;
            if (!string.IsNullOrEmpty(shipName) && shipName != "N/A" && !shipName.Equals(shipType, StringComparison.OrdinalIgnoreCase))
            {
                nameLabel = $"{shipType} \"{shipName}\"";
            }

            // Update the new name and ID labels on the ship tab
            if (_controlFactory.ShipTabNameLabel != null)
            {
                _controlFactory.ShipTabNameLabel.Text = nameLabel;
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipTabNameLabel, $"Ship Type: {shipType}\nName: {shipName}");
            }
            if (_controlFactory.ShipTabIdentLabel != null)
            {
                _controlFactory.ShipTabIdentLabel.Text = $"ID: {shipIdent}";
                _controlFactory.ToolTip.SetToolTip(_controlFactory.ShipTabIdentLabel, $"Ship ID: {shipIdent}");
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

        public void UpdateEdsmStatus(EdsmUploadStatus status)
        {
            if (_controlFactory?.EdsmStatusLabel == null) return;

            var hasCredentials = status.HasCredentials;
            var isActive = status.IsActive;
            string statusText;
            Color backColor;
            if (isActive && hasCredentials)
            {
                statusText = "EDSM: Active";
                backColor = UIConstants.StartButtonActiveColor;
            }
            else if (!hasCredentials)
            {
                statusText = "EDSM: Missing credentials";
                backColor = UIConstants.StopButtonActiveColor;
            }
            else
            {
                statusText = "EDSM: Idle";
                backColor = UIConstants.DefaultButtonBackColor;
            }

            var lastSync = status.LastSuccessfulUploadUtc.HasValue
                ? status.LastSuccessfulUploadUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "never";
            _controlFactory.EdsmStatusLabel.Text = statusText;
            _controlFactory.EdsmStatusLabel.BackColor = backColor;
            _controlFactory.ToolTip.SetToolTip(
                _controlFactory.EdsmStatusLabel,
                $"{statusText}\nLast sync: {lastSync}\nCommander: {(string.IsNullOrWhiteSpace(status.CommanderName) ? "Unknown" : status.CommanderName)}");
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
    }
}
