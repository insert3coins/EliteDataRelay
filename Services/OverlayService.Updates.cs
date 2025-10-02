using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    public partial class OverlayService
    {
        #region Data Update Methods
        // These methods update the UI controls on the specific overlay forms.
        // The OverlayForm itself handles thread safety with InvokeRequired checks.

        public void UpdateCommander(string name)
        {
            _leftOverlayForm?.UpdateCommander(name);
        }

        public void UpdateShip(string shipName, string shipIdent, string shipType)
        {
            _leftOverlayForm?.UpdateShip(shipType);
        }

        public void UpdateShipIcon(Image? shipIcon)
        {
            _shipIconOverlayForm?.UpdateShipIcon(shipIcon);
        }

        public void UpdateBalance(long balance)
        {
            _leftOverlayForm?.UpdateBalance(balance);
        }

        public void UpdateCargo(int count, int? capacity)
        {
            _rightOverlayForm?.UpdateCargo(count, capacity);
        }

        public void UpdateCargoList(CargoSnapshot snapshot) => _rightOverlayForm?.UpdateCargoList(snapshot.Inventory);

        public void UpdateCargoSize(string size) => _rightOverlayForm?.UpdateCargoSize(size);

        public void UpdateSessionOverlay(long cargo, long credits)
        {
            _rightOverlayForm?.UpdateSessionCreditsEarned(credits);
            _rightOverlayForm?.UpdateSessionCargoCollected(cargo);
        }

        public void UpdateMiningSession(SessionTrackingService tracker)
        {
            bool shouldBeVisible = AppConfiguration.EnableMiningOverlay && tracker.IsMiningSessionActive;

            if (shouldBeVisible)
            {
                if (_miningOverlayForm == null)
                {
                    var newMiningOverlay = new OverlayForm(OverlayForm.OverlayPosition.MiningSession, AppConfiguration.AllowOverlayDrag);
                    newMiningOverlay.PositionChanged += OnOverlayPositionChanged;

                    var primaryScreen = Screen.PrimaryScreen;
                    if (primaryScreen == null)
                    {
                        // Cannot position the overlay without a screen.
                        return;
                    }
                    var screen = primaryScreen.WorkingArea;
                    const int screenEdgePadding = 20;

                    // Calculate default position for the bottom-right corner.
                    int xPos = screen.Width - newMiningOverlay.Width - screenEdgePadding; // Right side
                    int yPos = screenEdgePadding; // Top side

                    var defaultLocation = new Point(xPos, yPos);
                    newMiningOverlay.Location = AppConfiguration.MiningOverlayLocation != Point.Empty ? AppConfiguration.MiningOverlayLocation : defaultLocation;
                    newMiningOverlay.Show();

                    _miningOverlayForm = newMiningOverlay;
                }
                _miningOverlayForm.UpdateMiningSession(tracker);
            }
            else
            {
                _miningOverlayForm?.Close();
                _miningOverlayForm = null;
            }
        }

        public void UpdateSystemInfo(SystemInfoData data) { }
        public void UpdateStationInfo(StationInfoData data) { }

        #endregion
    }
}