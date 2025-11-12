using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    public partial class OverlayService
    {
        
        
        private void AutoShowHideCargoOverlay()
        {
            // Only manage visibility if overlay exists and is enabled
            if (_rightOverlayForm == null || _rightOverlayForm.IsDisposed) return;

            bool hasCargo = (_lastCargoCount.HasValue && _lastCargoCount.Value > 0)
                            || (_lastCargoSnapshot?.Items?.Any() == true);

            if (hasCargo)
                _rightOverlayForm.Show();
            else
                _rightOverlayForm.Hide();
        }
        #region Data Update Methods
        // These methods update the UI controls on the specific overlay forms.
        // The OverlayForm itself handles thread safety with InvokeRequired checks.

        public void UpdateCommander(string name)
        {
            _lastCommanderName = name;
            _leftOverlayForm?.UpdateCommander(name);
        }

        public void UpdateShip(string shipName, string shipIdent, string shipType)
        {
            _lastShipType = shipType;
            _leftOverlayForm?.UpdateShip(shipType);
        }

        public void UpdateShipIcon(Image? shipIcon)
        {
            _lastShipIcon = shipIcon;
            _shipIconOverlayForm?.UpdateShipIcon(shipIcon);
        }

        public void UpdateBalance(long balance)
        {
            _lastBalance = balance;
            _leftOverlayForm?.UpdateBalance(balance);
        }

        public void UpdateCargo(int count, int? capacity)
        {
            _lastCargoCount = count;
            _lastCargoCapacity = capacity;
            _rightOverlayForm?.UpdateCargo(count, capacity);
            AutoShowHideCargoOverlay();
        }

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            _lastCargoSnapshot = snapshot;
            _rightOverlayForm?.UpdateCargoList(snapshot.Items);
            AutoShowHideCargoOverlay();
        }

        public void UpdateCargoSize(string size)
        {
            _lastCargoBarText = size;
            _rightOverlayForm?.UpdateCargoSize(size);
        }

        public void UpdateSessionOverlay(SessionOverlayData data)
        {
            _lastSessionOverlayData = data;
            _sessionOverlayForm?.UpdateSessionOverlay(data);
        }

        public void UpdateSystemInfo(SystemInfoData data)
        {
            _lastSystemInfoData = data;
            _explorationOverlayForm?.UpdateSystemInfo(data);

            // Next Jump overlay removed; no further propagation
        }
        public void UpdateStationInfo(StationInfoData data) { }

        public void UpdateExplorationData(SystemExplorationData? data)
        {
            // Cache the data so we can restore it after overlay refresh
            _lastExplorationData = data;
            // Debounce rapid updates (e.g., during startup) so we only render the latest
            lock (_explorationDebounceLock)
            {
                // If this is the first update or the system changed, push immediately to keep UI snappy
                bool pushImmediate = _lastExplorationData == null || data == null ||
                                     (_lastExplorationData?.SystemAddress != data.SystemAddress);

                _explorationDebounceTimer?.Dispose();
                if (pushImmediate)
                {
                    try { _explorationOverlayForm?.UpdateExplorationData(_lastExplorationData); }
                    catch { /* ignore */ }
                }
                else
                {
                    _explorationDebounceTimer = new System.Threading.Timer(_ =>
                    {
                        try { _explorationOverlayForm?.UpdateExplorationData(_lastExplorationData); }
                        catch { /* ignore */ }
                    }, null, _explorationDebounceDelay, System.TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public void UpdateExplorationSessionData(ExplorationSessionData? data)
        {
            // Cache the data so we can restore it after overlay refresh
            _lastExplorationSessionData = data;
            _explorationOverlayForm?.UpdateExplorationSessionData(data);
        }

        public void ShowNextJumpOverlay(NextJumpOverlayData data)
        {
            // Next Jump overlay removed; ignore calls
        }

        public void HideNextJumpOverlay()
        {
            // Next Jump overlay removed; ignore calls
        }

        public void HideNextJumpOverlayAfter(TimeSpan delay)
        {
            // Next Jump overlay removed; ignore calls
        }

        #endregion
    }
}
