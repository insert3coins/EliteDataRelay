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
        public void UpdateCargo(int count, int? capacity)
        {
            UpdateLabel(_cargoHeaderLabel, "Cargo:");
            UpdateLabel(_cargoSizeLabel, capacity.HasValue ? $"{count}/{capacity.Value}" : $"{count}");
        }
        public void UpdateCargoSize(string text) => UpdateLabel(_cargoBarLabel, text);
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

        public void UpdateShipIcon(Image? shipIcon)
        {
            if (InvokeRequired) { Invoke(new Action(() => _shipIconPictureBox.Image = shipIcon)); }
            else { _shipIconPictureBox.Image = shipIcon; }
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