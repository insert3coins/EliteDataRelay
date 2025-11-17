using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    public partial class OverlayService
    {
        
        
        private void AutoShowHideCargoOverlay()
        {
            // Only manage visibility if overlay exists and is enabled
            if (_rightOverlayForm == null || _rightOverlayForm.IsDisposed) return;

            if (_forceShowAllOverlays)
            {
                if (AppConfiguration.EnableCargoOverlay)
                {
                    _rightOverlayForm.Show();
                }
                return;
            }

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

        private static readonly TimeSpan MiningOverlayHoldDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ProspectorOverlayHoldDuration = TimeSpan.FromSeconds(5);

        public void UpdateMiningOverlay(MiningOverlayData? data)
        {
            if (!AppConfiguration.EnableMiningOverlay)
            {
                _lastMiningOverlayData = data;
                return;
            }

            EnsureOverlaysCreated(_overlayOwner);
            if (_miningOverlayForm == null) return;

            if (_forceShowAllOverlays)
            {
                _miningOverlayHideTimer?.Dispose();
                _miningOverlayHideTimer = null;
                _lastMiningOverlayData = data;
                _miningOverlayForm.Show();
                _miningOverlayForm.UpdateMiningOverlay(data);
                return;
            }

            if (data != null)
            {
                _miningOverlayHideTimer?.Dispose();
                _miningOverlayHideTimer = null;

                _lastMiningOverlayData = data;
                _miningOverlayForm.UpdateMiningOverlay(data);
                _miningOverlayForm.FadeIn(200, allowAnyOverlay: true);
                return;
            }

            var snapshot = _lastMiningOverlayData;
            if (snapshot == null)
            {
                if (_miningOverlayHideTimer != null)
                {
                    return;
                }

                _miningOverlayForm.Hide();
                return;
            }

            _lastMiningOverlayData = null;
            _miningOverlayForm.UpdateMiningOverlay(snapshot);

            _miningOverlayHideTimer?.Dispose();
            _miningOverlayHideTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    _miningOverlayForm?.FadeOutAndHide(250, allowAnyOverlay: true);
                }
                finally
                {
                    _miningOverlayHideTimer?.Dispose();
                    _miningOverlayHideTimer = null;
                }
            }, null, MiningOverlayHoldDuration, Timeout.InfiniteTimeSpan);
        }

        public void UpdateProspectorOverlay(ProspectorOverlayData? data)
        {
            if (!AppConfiguration.EnableProspectorOverlay)
            {
                _lastProspectorOverlayData = data;
                return;
            }

            EnsureOverlaysCreated(_overlayOwner);
            if (_prospectorOverlayForm == null) return;

            if (_forceShowAllOverlays)
            {
                _prospectorOverlayHideTimer?.Dispose();
                _prospectorOverlayHideTimer = null;
                _lastProspectorOverlayData = data;
                _prospectorOverlayForm.Show();
                _prospectorOverlayForm.UpdateProspectorOverlay(data);
                return;
            }

            if (data != null)
            {
                _prospectorOverlayHideTimer?.Dispose();
                _prospectorOverlayHideTimer = null;

                _lastProspectorOverlayData = data;
                _prospectorOverlayForm.UpdateProspectorOverlay(data);
                _prospectorOverlayForm.FadeIn(200, allowAnyOverlay: true);
                return;
            }

            var snapshot = _lastProspectorOverlayData;
            if (snapshot == null)
            {
                if (_prospectorOverlayHideTimer != null) return;
                _prospectorOverlayForm.Hide();
                return;
            }

            _lastProspectorOverlayData = null;
            _prospectorOverlayForm.UpdateProspectorOverlay(snapshot);

            _prospectorOverlayHideTimer?.Dispose();
            _prospectorOverlayHideTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    _prospectorOverlayForm?.FadeOutAndHide(250, allowAnyOverlay: true);
                }
                finally
                {
                    _prospectorOverlayHideTimer?.Dispose();
                    _prospectorOverlayHideTimer = null;
                }
            }, null, ProspectorOverlayHoldDuration, Timeout.InfiniteTimeSpan);
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
