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
    public partial class OverlayForm
    {
        public void UpdateCommander(string text) => UpdateLabel(_cmdrValueLabel, text);
        public void UpdateShip(string text) => UpdateLabel(_shipValueLabel, text);
        public void UpdateBalance(long balance) => UpdateLabel(_balanceValueLabel, $"{balance:N0} CR");
        public void UpdateCargo(int count, int? capacity) => UpdateLabel(_cargoHeaderLabel, capacity.HasValue ? $"Cargo: {count}/{capacity.Value}" : $"Cargo: {count}");
        public void UpdateCargoSize(string text) => UpdateLabel(_cargoSizeLabel, text);
        public void UpdateSessionCargoCollected(long cargo)
        {
            // Only update if the label was created
            if (_sessionCargoValueLabel != null)
            {
                UpdateLabel(_sessionCargoValueLabel, $"{cargo}");
            }
        }

        public void UpdateSessionCreditsEarned(long credits)
        {
            if (_sessionCreditsValueLabel != null)
            {
                UpdateLabel(_sessionCreditsValueLabel, $"{credits:N0}");
            }
        }

        public void UpdateCargoList(IEnumerable<CargoItem> inventory)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargoList(inventory)));
                return;
            }

            _cargoItems = inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name).ToList();
            _cargoListPanel?.Invalidate();
        }

        public void UpdateSystemInfo(SystemInfoData data)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateSystemInfo(data)));
                return;
            }

            UpdateLabel(_systemNameLabel, data.SystemName, AppConfiguration.OverlayTextColor); // Main system name
            UpdateLabel(_allegianceLabel, data.Allegiance, AppConfiguration.OverlayTextColor);
            UpdateLabel(_governmentLabel, data.Government, AppConfiguration.OverlayTextColor);
            UpdateLabel(_economyLabel, data.Economy, AppConfiguration.OverlayTextColor);
            UpdateLabel(_securityLabel, data.Security, GetColorForSecurityLevel(data.Security));
            UpdateLabel(_populationLabel, $"{data.Population:N0}", AppConfiguration.OverlayTextColor);
            UpdateLabel(_factionLabel, $"{data.ControllingFaction} ({data.FactionState})", AppConfiguration.OverlayTextColor);
        }

        public void UpdateStationInfo(StationInfoData data)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStationInfo(data)));
                return;
            }

            // If we are undocked, hide the overlay. Otherwise, show it.
            this.Visible = data.StationName != "Undocked";
            if (!this.Visible) return;

            UpdateLabel(_stationNameLabel, data.StationName, AppConfiguration.OverlayTextColor);
            UpdateLabel(_stationTypeLabel, data.StationType, SystemColors.GrayText);
            UpdateLabel(_stationAllegianceLabel, data.Allegiance, AppConfiguration.OverlayTextColor);
            UpdateLabel(_stationGovernmentLabel, data.Government, AppConfiguration.OverlayTextColor);
            UpdateLabel(_stationEconomyLabel, data.Economy, AppConfiguration.OverlayTextColor);
            UpdateLabel(_stationFactionLabel, data.ControllingFaction, AppConfiguration.OverlayTextColor);

            // Update services
            _servicesPanel.Controls.Clear();
            AddServiceLabel("Refuel", data.HasRefuel);
            AddServiceLabel("Repair", data.HasRepair);
            AddServiceLabel("Rearm", data.HasRearm);
            AddServiceLabel("Outfitting", data.HasOutfitting);
            AddServiceLabel("Shipyard", data.HasShipyard);
            AddServiceLabel("Market", data.HasMarket);
        }

        private void AddServiceLabel(string name, bool isAvailable)
        {
            var label = new Label
            {
                Text = name,
                Font = _listFont,
                ForeColor = isAvailable ? Color.SkyBlue : SystemColors.GrayText,
                BackColor = Color.FromArgb(50, 80, 80, 80), // Dark, semi-transparent background for the tag
                Padding = new Padding(5, 3, 5, 3),
                Margin = new Padding(0, 0, 5, 5),
                AutoSize = true
            };
            _servicesPanel.Controls.Add(label);
        }

        private Color GetColorForSecurityLevel(string securityLevel)
        {
            if (string.IsNullOrEmpty(securityLevel)) return AppConfiguration.OverlayTextColor;

            return securityLevel.ToLowerInvariant() switch
            {
                "high" => Color.SkyBlue,
                "medium" => Color.Goldenrod,
                "low" => Color.Orange,
                "anarchy" => Color.Crimson,
                _ => AppConfiguration.OverlayTextColor
            };
        }

        private void UpdateLabel(Label label, string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => label.Text = text)); }
            else { label.Text = text; }
        }

        private void UpdateLabel(Label label, string text, Color color)
        {
            if (InvokeRequired) { Invoke(new Action(() => { label.Text = text; label.ForeColor = color; })); }
            else { label.Text = text; label.ForeColor = color; }
        }
    }
}