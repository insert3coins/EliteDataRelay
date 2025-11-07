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
        /// <summary>
        /// Updates commander name and marks frame as stale.
        /// </summary>
        public void UpdateCommander(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCommander(text)));
                return;
            }

            _commanderName = text;
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates ship type and marks frame as stale.
        /// </summary>
        public void UpdateShip(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateShip(text)));
                return;
            }

            _shipType = text;
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates balance and marks frame as stale.
        /// </summary>
        public void UpdateBalance(long balance)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateBalance(balance)));
                return;
            }

            _balance = balance;
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates cargo count/capacity and marks frame as stale.
        /// </summary>
        public void UpdateCargo(int count, int? capacity)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargo(count, capacity)));
                return;
            }

            _cargoCount = count;
            _cargoCapacity = capacity;
            ResizeCargoToContent();
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates cargo bar text (visual representation) and marks frame as stale.
        /// </summary>
        public void UpdateCargoSize(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargoSize(text)));
                return;
            }

            _cargoBarText = text;
            ResizeCargoToContent();
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates session cargo collected and marks frame as stale.
        /// </summary>
        public void UpdateSessionCargoCollected(long cargo)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateSessionCargoCollected(cargo)));
                return;
            }

            _sessionCargo = cargo;
            ResizeCargoToContent();
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates session credits earned and marks frame as stale.
        /// </summary>
        public void UpdateSessionCreditsEarned(long credits)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateSessionCreditsEarned(credits)));
                return;
            }

            _sessionCredits = credits;
            ResizeCargoToContent();
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates cargo list and marks frame as stale.
        /// </summary>
        public void UpdateCargoList(IEnumerable<CargoItem> inventory)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargoList(inventory)));
                return;
            }

            _cargoItems = inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name).ToList();
            ResizeCargoToContent();
            _stale = true;
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates ship icon image and marks frame as stale.
        /// Note: ShipIcon overlay always re-renders for animation.
        /// </summary>
        public void UpdateShipIcon(Image? shipIcon)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateShipIcon(shipIcon)));
                return;
            }

            // Don't dispose - the image might be shared/managed elsewhere
            // The old reference will be garbage collected
            _shipIcon = shipIcon;
            _stale = true;
            _renderPanel?.Invalidate();
        }
    }
}
