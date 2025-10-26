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

        public void UpdateCargoList(CargoSnapshot snapshot) => _rightOverlayForm?.UpdateCargoList(snapshot.Items);

        public void UpdateCargoSize(string size) => _rightOverlayForm?.UpdateCargoSize(size);

        public void UpdateSessionOverlay(long cargo, long credits)
        {
            _rightOverlayForm?.UpdateSessionCreditsEarned(credits);
            _rightOverlayForm?.UpdateSessionCargoCollected(cargo);
        }

        public void UpdateSystemInfo(SystemInfoData data) { }
        public void UpdateStationInfo(StationInfoData data) { }

        public void UpdateExplorationData(SystemExplorationData? data)
        {
            _explorationOverlayForm?.UpdateExplorationData(data);
        }

        #endregion
    }
}