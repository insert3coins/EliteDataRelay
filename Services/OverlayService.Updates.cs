using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    public partial class OverlayService
    {
        // Auto-hide timer removed; overlay hides on JumpCompleted
        private System.Threading.Timer? _jumpOverlayHideDelayTimer;
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
        }

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            _lastCargoSnapshot = snapshot;
            _rightOverlayForm?.UpdateCargoList(snapshot.Items);
        }

        public void UpdateCargoSize(string size)
        {
            _lastCargoBarText = size;
            _rightOverlayForm?.UpdateCargoSize(size);
        }

        public void UpdateSessionOverlay(long cargo, long credits)
        {
            _lastSessionCargo = cargo;
            _lastSessionCredits = credits;
            _rightOverlayForm?.UpdateSessionCreditsEarned(credits);
            _rightOverlayForm?.UpdateSessionCargoCollected(cargo);
        }

        public void UpdateSystemInfo(SystemInfoData data)
        {
            _lastSystemInfoData = data;
            _explorationOverlayForm?.UpdateSystemInfo(data);
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
            _lastNextJumpData = data;
            EnsureJumpOverlay();
            if (_jumpOverlayForm == null) return;
            _jumpOverlayForm.UpdateJumpInfo(data);
            _jumpOverlayForm.Show();
        }

        public void HideNextJumpOverlay()
        {
            _jumpOverlayForm?.Hide();
        }

        public void HideNextJumpOverlayAfter(TimeSpan delay)
        {
            try
            {
                _jumpOverlayHideDelayTimer?.Dispose();
                _jumpOverlayHideDelayTimer = new System.Threading.Timer(_ =>
                {
                    try { HideNextJumpOverlay(); }
                    catch { /* ignore */ }
                }, null, delay, TimeSpan.FromMilliseconds(-1));
            }
            catch { /* ignore */ }
        }

        #endregion
    }
}
