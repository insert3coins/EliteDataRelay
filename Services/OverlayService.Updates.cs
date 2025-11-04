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

            // Also attach latest system info to the jump overlay data so it can render traffic,
            // but only if the names match the current target to avoid double-updates during FSDJump
            if (_lastNextJumpData != null &&
                !string.IsNullOrWhiteSpace(_lastNextJumpData.TargetSystemName) &&
                !string.IsNullOrWhiteSpace(data.SystemName) &&
                string.Equals(_lastNextJumpData.TargetSystemName, data.SystemName, System.StringComparison.OrdinalIgnoreCase))
            {
                bool hadTraffic = _lastNextJumpData.SystemInfo != null &&
                                   ((_lastNextJumpData.SystemInfo.TrafficDay > 0) ||
                                    (_lastNextJumpData.SystemInfo.TrafficWeek > 0) ||
                                    (_lastNextJumpData.SystemInfo.TrafficTotal > 0));
                bool newHasTraffic = (data.TrafficDay > 0) || (data.TrafficWeek > 0) || (data.TrafficTotal > 0);

                // If we already had traffic populated, skip redundant updates for the same target
                if (!hadTraffic || !newHasTraffic)
                {
                    _lastNextJumpData.SystemInfo = data;
                    try { _jumpOverlayForm?.UpdateJumpInfo(_lastNextJumpData); } catch { /* ignore */ }
                }
            }
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
            // Merge with last known data to avoid blanking out details
            // during jump transitions where some fields may be missing.
            var merged = data;
            try
            {
                if (_lastNextJumpData != null)
                {
                    merged = new NextJumpOverlayData
                    {
                        // Prefer new values when present; otherwise keep last known
                        TargetSystemName = !string.IsNullOrWhiteSpace(data.TargetSystemName) ? data.TargetSystemName : _lastNextJumpData.TargetSystemName,
                        StarClass = !string.IsNullOrWhiteSpace(data.StarClass) ? data.StarClass : _lastNextJumpData.StarClass,
                        JumpDistanceLy = data.JumpDistanceLy ?? _lastNextJumpData.JumpDistanceLy,
                        RemainingJumps = data.RemainingJumps ?? _lastNextJumpData.RemainingJumps,
                        SystemInfo = data.SystemInfo ?? _lastNextJumpData.SystemInfo,
                        NextDistanceLy = data.NextDistanceLy ?? _lastNextJumpData.NextDistanceLy,
                        TotalRemainingLy = data.TotalRemainingLy ?? _lastNextJumpData.TotalRemainingLy,
                        CurrentJumpIndex = data.CurrentJumpIndex ?? _lastNextJumpData.CurrentJumpIndex,
                        TotalJumps = data.TotalJumps ?? _lastNextJumpData.TotalJumps,
                        Hops = (data.Hops != null && data.Hops.Count > 0) ? data.Hops : _lastNextJumpData.Hops
                    };
                }
            }
            catch { /* ignore merge issues; fall back to provided data */ }

            _lastNextJumpData = merged;
            EnsureJumpOverlay();
            if (_jumpOverlayForm == null) return;
            // If already visible, just update content without replaying fade
            if (_jumpOverlayForm.Visible)
            {
                _jumpOverlayForm.UpdateJumpInfo(merged);
                return;
            }
            _jumpOverlayForm.UpdateJumpInfo(merged);
            _jumpOverlayForm.FadeIn(200);
        }

        public void HideNextJumpOverlay()
        {
            if (_jumpOverlayForm == null) return;
            if (!_jumpOverlayForm.Visible) return;
            _jumpOverlayForm.FadeOutAndHide(200);
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
