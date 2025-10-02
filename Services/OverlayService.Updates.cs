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

        public void ManageMiningOverlay(SessionTrackingService tracker)
        {
            bool shouldBeVisible = AppConfiguration.EnableMiningOverlay && tracker.IsMiningSessionActive;

            if (shouldBeVisible && _miningOverlayForm == null)
            {
                // Create and show the form
                _miningOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.MiningSession, AppConfiguration.AllowOverlayDrag);
                _miningOverlayForm.PositionChanged += OnOverlayPositionChanged;

                // Position and show
                PositionMiningOverlay();
                _miningOverlayForm.Show();
            }
            else if (!shouldBeVisible && _miningOverlayForm != null)
            {
                // Close and dispose the form
                _miningOverlayForm.Close();
                _miningOverlayForm = null;
            }
        }

        public void UpdateMiningSession(SessionTrackingService tracker)
        {
            // This method is now only responsible for updating the data on an existing form.
            // The creation and visibility are handled by ManageMiningOverlay.
            _miningOverlayForm?.UpdateMiningSession(tracker);
        }

        public void UpdateSystemInfo(SystemInfoData data) { }
        public void UpdateStationInfo(StationInfoData data) { }

        #endregion
    }
}