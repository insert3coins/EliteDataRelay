using EliteDataRelay.Configuration;
using EliteDataRelay.UI;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.Services

{
    public partial class OverlayService
    {
        private void PositionOverlays(Rectangle screen)
        {
            const int screenEdgePadding = 20;
            const int overlaySpacing = 10;

            // Default for Cargo overlay (middle right)
            Point defaultRightLocation = Point.Empty;
            var rightForm = _rightOverlayForm;
            if (rightForm != null)
            {
                int y = (screen.Height / 2) - (rightForm.Height / 2);
                defaultRightLocation = new Point(screen.Width - rightForm.Width - screenEdgePadding, y);
            }

            // --- Calculate positions for the bottom-left overlay stack ---
            int totalStackHeight = 0;
            if (_leftOverlayForm != null) totalStackHeight += _leftOverlayForm.Height;
            if (_shipIconOverlayForm != null) totalStackHeight += (totalStackHeight > 0 ? overlaySpacing : 0) + _shipIconOverlayForm.Height;

            int currentY = screen.Height - totalStackHeight - screenEdgePadding;

            Point defaultLeftLocation = Point.Empty;
            if (_leftOverlayForm != null)
            {
                defaultLeftLocation = new Point(screenEdgePadding, currentY);
                currentY += _leftOverlayForm.Height + overlaySpacing;
            }

            Point defaultShipIconLocation = Point.Empty;
            if (_shipIconOverlayForm != null)
            {
                defaultShipIconLocation = new Point(screenEdgePadding, currentY);
                currentY += _shipIconOverlayForm.Height + overlaySpacing;
            }

            // --- Assign final positions ---
            if (_leftOverlayForm != null)
                _leftOverlayForm.Location = AppConfiguration.InfoOverlayLocation != Point.Empty ? AppConfiguration.InfoOverlayLocation : defaultLeftLocation;

            if (_rightOverlayForm != null)
                _rightOverlayForm.Location = AppConfiguration.CargoOverlayLocation != Point.Empty ? AppConfiguration.CargoOverlayLocation : defaultRightLocation;

            if (_shipIconOverlayForm != null)
                _shipIconOverlayForm.Location = AppConfiguration.ShipIconOverlayLocation != Point.Empty ? AppConfiguration.ShipIconOverlayLocation : defaultShipIconLocation;
        }

        private void PositionMiningOverlay()
        {
            if (_miningOverlayForm == null)
            {
                return;
            }

            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                // Cannot position the overlay without a screen.
                return;
            }
            var screen = primaryScreen.WorkingArea;
            const int screenEdgePadding = 20;

            // Calculate default position for the top-right corner.
            int xPos = screen.Width - _miningOverlayForm.Width - screenEdgePadding; // Right side
            int yPos = screenEdgePadding; // Top side

            var defaultLocation = new Point(xPos, yPos);
            _miningOverlayForm.Location = AppConfiguration.MiningOverlayLocation != Point.Empty ? AppConfiguration.MiningOverlayLocation : defaultLocation;
        }

        private void OnOverlayPositionChanged(object? sender, Point newLocation)
        {
            if (sender == _leftOverlayForm)
                AppConfiguration.InfoOverlayLocation = newLocation;
            else if (sender == _rightOverlayForm)
                AppConfiguration.CargoOverlayLocation = newLocation;
            else if (sender == _shipIconOverlayForm)
                AppConfiguration.ShipIconOverlayLocation = newLocation;
            else if (sender == _miningOverlayForm)
                AppConfiguration.MiningOverlayLocation = newLocation;

            AppConfiguration.Save();
        }
    }
}