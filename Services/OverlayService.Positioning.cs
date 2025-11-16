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
            Point defaultSessionLocation = Point.Empty;
            var rightForm = _rightOverlayForm;
            if (rightForm != null)
            {
                int y = (screen.Height / 2) - (rightForm.Height / 2);
                defaultRightLocation = new Point(screen.Width - rightForm.Width - screenEdgePadding, y);
            }

            if (_sessionOverlayForm != null)
            {
                defaultSessionLocation = new Point(
                    screen.Width - _sessionOverlayForm.Width - screenEdgePadding,
                    screen.Height - _sessionOverlayForm.Height - screenEdgePadding);
            }

            // --- Calculate positions for the bottom-left overlay stack (Info + Mining) ---
            int totalStackHeight = 0;
            int stackCount = 0;
            if (_leftOverlayForm != null)
            {
                totalStackHeight += _leftOverlayForm.Height;
                stackCount++;
            }
            if (_miningOverlayForm != null)
            {
                if (stackCount > 0) totalStackHeight += overlaySpacing;
                totalStackHeight += _miningOverlayForm.Height;
                stackCount++;
            }

            int currentY = screen.Height - totalStackHeight - screenEdgePadding;

            Point defaultLeftLocation = Point.Empty;
            if (_leftOverlayForm != null)
            {
                defaultLeftLocation = new Point(screenEdgePadding, currentY);
                currentY += _leftOverlayForm.Height + (_miningOverlayForm != null ? overlaySpacing : 0);
            }

            Point defaultMiningLocation = Point.Empty;
            if (_miningOverlayForm != null)
            {
                defaultMiningLocation = new Point(screenEdgePadding, currentY);
            }

            // --- Assign final positions ---
            if (_leftOverlayForm != null)
                _leftOverlayForm.Location = AppConfiguration.InfoOverlayLocation != Point.Empty ? AppConfiguration.InfoOverlayLocation : defaultLeftLocation;
            if (_miningOverlayForm != null)
                _miningOverlayForm.Location = AppConfiguration.MiningOverlayLocation != Point.Empty ? AppConfiguration.MiningOverlayLocation : defaultMiningLocation;

            if (_rightOverlayForm != null)
                _rightOverlayForm.Location = AppConfiguration.CargoOverlayLocation != Point.Empty ? AppConfiguration.CargoOverlayLocation : defaultRightLocation;

            if (_sessionOverlayForm != null)
            {
                var fallbackSession = defaultSessionLocation != Point.Empty
                    ? defaultSessionLocation
                    : new Point(screen.Width - _sessionOverlayForm.Width - screenEdgePadding, screenEdgePadding);
                _sessionOverlayForm.Location = AppConfiguration.SessionOverlayLocation != Point.Empty ? AppConfiguration.SessionOverlayLocation : fallbackSession;
            }

            // Exploration overlay defaults to top-left (already set in config default)
            Point explorationDefault = new Point(screenEdgePadding, screenEdgePadding);
            if (_explorationOverlayForm != null)
                _explorationOverlayForm.Location = AppConfiguration.ExplorationOverlayLocation != Point.Empty ? AppConfiguration.ExplorationOverlayLocation : explorationDefault;

            int topStackY = screenEdgePadding;
            if (_explorationOverlayForm != null)
            {
                topStackY = (_explorationOverlayForm.Location.Y > 0 ? _explorationOverlayForm.Location.Y : screenEdgePadding) + _explorationOverlayForm.Height + overlaySpacing;
            }

            Point defaultProspectorLocation = Point.Empty;
            if (_prospectorOverlayForm != null)
            {
                defaultProspectorLocation = new Point(screenEdgePadding, topStackY);
                topStackY += _prospectorOverlayForm.Height + overlaySpacing;
            }

            if (_prospectorOverlayForm != null)
            {
                _prospectorOverlayForm.Location = AppConfiguration.ProspectorOverlayLocation != Point.Empty
                    ? AppConfiguration.ProspectorOverlayLocation
                    : defaultProspectorLocation;
            }

            // Jump overlay defaults to top-center below top edge
            if (_jumpOverlayForm != null)
            {
                var def = new Point((screen.Width / 2) - (_jumpOverlayForm.Width / 2), screenEdgePadding);
                _jumpOverlayForm.Location = AppConfiguration.JumpOverlayLocation != Point.Empty ? AppConfiguration.JumpOverlayLocation : def;
            }
        }

        private void OnOverlayPositionChanged(object? sender, Point newLocation)
        {
            if (sender == _leftOverlayForm)
                AppConfiguration.InfoOverlayLocation = newLocation;
            else if (sender == _rightOverlayForm)
                AppConfiguration.CargoOverlayLocation = newLocation;
            else if (sender == _sessionOverlayForm)
                AppConfiguration.SessionOverlayLocation = newLocation;
            else if (sender == _explorationOverlayForm)
                AppConfiguration.ExplorationOverlayLocation = newLocation;
            else if (sender == _miningOverlayForm)
                AppConfiguration.MiningOverlayLocation = newLocation;
            else if (sender == _prospectorOverlayForm)
                AppConfiguration.ProspectorOverlayLocation = newLocation;
            else if (sender == _jumpOverlayForm)
                AppConfiguration.JumpOverlayLocation = newLocation;


            AppConfiguration.Save();

            // Export updated positions for OBS
            ExportObsPositions();
        }

        private void ExportObsPositions()
        {
            // OBS compatibility removed; browser overlays recommended for streaming.
        }
    }
}
