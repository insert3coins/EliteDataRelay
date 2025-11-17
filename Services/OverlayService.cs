using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;

namespace EliteDataRelay.Services
{
    public partial class OverlayService : IDisposable
    {
        private OverlayForm? _leftOverlayForm;
        private OverlayForm? _rightOverlayForm;
        private OverlayForm? _sessionOverlayForm;
        private OverlayForm? _explorationOverlayForm;
        private OverlayForm? _miningOverlayForm;
        private OverlayForm? _prospectorOverlayForm;
        private OverlayForm? _jumpOverlayForm;

        // Cache last known data to restore on overlay refresh
        private SystemExplorationData? _lastExplorationData;
        private ExplorationSessionData? _lastExplorationSessionData;
        private SystemInfoData? _lastSystemInfoData;
        
        private string? _lastCommanderName;
        private string? _lastShipType;
        private long? _lastBalance;
        private int? _lastCargoCount;
        private int? _lastCargoCapacity;
        private string? _lastCargoBarText;
        private SessionOverlayData? _lastSessionOverlayData;
        private CargoSnapshot? _lastCargoSnapshot;
        private MiningOverlayData? _lastMiningOverlayData;
        private ProspectorOverlayData? _lastProspectorOverlayData;

        // Debounce for exploration overlay updates to avoid rapid churn at startup
        private System.Threading.Timer? _explorationDebounceTimer;
        private readonly object _explorationDebounceLock = new object();
        private TimeSpan _explorationDebounceDelay = TimeSpan.FromMilliseconds(500);
        private Form? _overlayOwner;
        private System.Threading.Timer? _miningOverlayHideTimer;
        private System.Threading.Timer? _prospectorOverlayHideTimer;
        private bool _forceShowAllOverlays;
        private bool _overlaysVisible;

        

        private void EnsureOverlaysCreated(Form? owner = null, bool force = false)
        {
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayService] No primary screen detected. Overlays will not be created.");
                return;
            }

            var screen = primaryScreen.WorkingArea;
            if (owner != null)
            {
                _overlayOwner = owner;
            }
            var overlayOwner = _overlayOwner;

            if (_leftOverlayForm == null && (force || AppConfiguration.EnableInfoOverlay))
            {
                _leftOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Info, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _leftOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_rightOverlayForm == null && (force || AppConfiguration.EnableCargoOverlay))
            {
                _rightOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Cargo, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _rightOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_sessionOverlayForm == null && (force || AppConfiguration.EnableSessionOverlay))
            {
                _sessionOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Session, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _sessionOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_explorationOverlayForm == null && (force || AppConfiguration.EnableExplorationOverlay))
            {
                _explorationOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Exploration, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _explorationOverlayForm.PositionChanged += OnOverlayPositionChanged;
                System.Diagnostics.Debug.WriteLine("[OverlayService] Exploration overlay created");
            }
            if (_miningOverlayForm == null && (force || AppConfiguration.EnableMiningOverlay))
            {
                _miningOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Mining, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _miningOverlayForm.PositionChanged += OnOverlayPositionChanged;
                _miningOverlayForm.Shown += (_, _) => overlayOwner?.BeginInvoke(new Action(() => overlayOwner.Activate()));
                _miningOverlayForm.FormClosed += (_, _) => overlayOwner?.BeginInvoke(new Action(() => overlayOwner.Activate()));
            }
            else if (!force && _miningOverlayForm != null && !AppConfiguration.EnableMiningOverlay)
            {
                _miningOverlayForm.Close();
                _miningOverlayForm = null;
            }

            if (_prospectorOverlayForm == null && (force || AppConfiguration.EnableProspectorOverlay))
            {
                _prospectorOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Prospector, AppConfiguration.AllowOverlayDrag, overlayOwner);
                _prospectorOverlayForm.PositionChanged += OnOverlayPositionChanged;
                _prospectorOverlayForm.Shown += (_, _) => overlayOwner?.BeginInvoke(new Action(() => overlayOwner.Activate()));
                _prospectorOverlayForm.FormClosed += (_, _) => overlayOwner?.BeginInvoke(new Action(() => overlayOwner.Activate()));
            }
            else if (!force && _prospectorOverlayForm != null && !AppConfiguration.EnableProspectorOverlay)
            {
                _prospectorOverlayForm.Close();
                _prospectorOverlayForm = null;
            }
            // Next Jump overlay removed/disabled

            PositionOverlays(screen);
        }

        public void Start(Form owner)
        {
            Stop(); // Ensure any existing overlays are closed

            EnsureOverlaysCreated(owner);

            // Show and restore data for Info overlay
            if (_leftOverlayForm != null)
            {
                _leftOverlayForm.Show();
                if (_lastCommanderName != null) _leftOverlayForm.UpdateCommander(_lastCommanderName);
                if (_lastShipType != null) _leftOverlayForm.UpdateShip(_lastShipType);
                if (_lastBalance.HasValue) _leftOverlayForm.UpdateBalance(_lastBalance.Value);
            }

            // Show and restore data for Cargo overlay (auto-hide when empty)
            if (_rightOverlayForm != null)
            {
                bool hasCargo = (_lastCargoCount.HasValue && _lastCargoCount.Value > 0)
                                || (_lastCargoSnapshot?.Items?.Any() == true);
                if (_forceShowAllOverlays || hasCargo) _rightOverlayForm.Show(); else _rightOverlayForm.Hide();

                if (_lastCargoCount.HasValue) _rightOverlayForm.UpdateCargo(_lastCargoCount.Value, _lastCargoCapacity);
                if (_lastCargoBarText != null) _rightOverlayForm.UpdateCargoSize(_lastCargoBarText);
                if (_lastCargoSnapshot != null) _rightOverlayForm.UpdateCargoList(_lastCargoSnapshot.Items);
            }

            if (_sessionOverlayForm != null)
            {
                _sessionOverlayForm.Show();
                if (_lastSessionOverlayData != null)
                {
                    _sessionOverlayForm.UpdateSessionOverlay(_lastSessionOverlayData);
                }
            }
            // Show and restore data for Exploration overlay
            if (_explorationOverlayForm != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OverlayService] Showing exploration overlay at {_explorationOverlayForm.Location}");
                _explorationOverlayForm.Show();

                // Restore last known exploration data after recreating overlay
                if (_lastExplorationData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[OverlayService] Restoring exploration data: {_lastExplorationData.SystemName}");
                    _explorationOverlayForm.UpdateExplorationData(_lastExplorationData);
                }
                if (_lastExplorationSessionData != null)
                {
                    _explorationOverlayForm.UpdateExplorationSessionData(_lastExplorationSessionData);
                }
                if (_lastSystemInfoData != null)
                {
                    _explorationOverlayForm.UpdateSystemInfo(_lastSystemInfoData);
                }
            }

            if (_miningOverlayForm != null)
            {
                _miningOverlayForm.Show();
                _miningOverlayForm.UpdateMiningOverlay(_lastMiningOverlayData);
            }

            if (_prospectorOverlayForm != null)
            {
                if (_forceShowAllOverlays || _lastProspectorOverlayData != null)
                {
                    _prospectorOverlayForm.Show();
                    _prospectorOverlayForm.UpdateProspectorOverlay(_lastProspectorOverlayData);
                }
                else
                {
                    _prospectorOverlayForm.Hide();
                }
            }

            // Jump overlay removed

            // Export overlay positions for OBS
            ExportObsPositions();
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _sessionOverlayForm?.Close();
            _explorationOverlayForm?.Close();
            _miningOverlayForm?.Close();
            _prospectorOverlayForm?.Close();
            _jumpOverlayForm?.Close();

            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _sessionOverlayForm = null;
            _explorationOverlayForm = null;
            _miningOverlayForm = null;
            _prospectorOverlayForm = null;
            _jumpOverlayForm = null;
            _overlayOwner = null;
            _miningOverlayHideTimer?.Dispose();
            _miningOverlayHideTimer = null;
            _prospectorOverlayHideTimer?.Dispose();
            _prospectorOverlayHideTimer = null;

            lock (_explorationDebounceLock)
            {
                _explorationDebounceTimer?.Dispose();
                _explorationDebounceTimer = null;
            }
        }

        public void Show()
        {
            EnsureOverlaysCreated(_overlayOwner);
            if (_forceShowAllOverlays)
            {
                ShowAllEnabledOverlays();
                return;
            }
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _sessionOverlayForm?.Show();
            _explorationOverlayForm?.Show();
            _miningOverlayForm?.Show();
            if (_lastProspectorOverlayData != null)
            {
                _prospectorOverlayForm?.Show();
            }
            else
            {
                _prospectorOverlayForm?.Hide();
            }
            // Jump overlay is transient; do not force show here
        }

        public void Hide()
        {
            EnsureOverlaysCreated(_overlayOwner);
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _sessionOverlayForm?.Hide();
            _explorationOverlayForm?.Hide();
            _miningOverlayForm?.Hide();
            _prospectorOverlayForm?.Hide();
            _jumpOverlayForm?.Hide();
        }

        public void SetOverlayRepositionMode(bool enabled, Form? owner = null)
        {
            if (_forceShowAllOverlays == enabled) return;
            _forceShowAllOverlays = enabled;

            if (enabled)
            {
                _miningOverlayHideTimer?.Dispose();
                _miningOverlayHideTimer = null;
                _prospectorOverlayHideTimer?.Dispose();
                _prospectorOverlayHideTimer = null;
                EnsureOverlaysCreated(owner ?? _overlayOwner, force: true);
                ShowAllEnabledOverlays();
            }
            else
            {
                PersistOverlayLocations();
                SetVisibility(_overlaysVisible);
            }
        }

        /// <summary>
        /// Sets the visibility of the overlays based on the provided boolean.
        /// Only shows overlays that are enabled in the application configuration.
        /// </summary>
        /// <param name="visible">True to show enabled overlays, false to hide all overlays.</param>
        public void SetVisibility(bool visible)
        {
            _overlaysVisible = visible;
            if (_forceShowAllOverlays)
            {
                ShowAllEnabledOverlays();
                return;
            }
            if (visible)
            {
                if (AppConfiguration.EnableInfoOverlay) _leftOverlayForm?.Show();
                if (AppConfiguration.EnableCargoOverlay)
                {
                    bool hasCargo = (_lastCargoCount.HasValue && _lastCargoCount.Value > 0)
                                    || (_lastCargoSnapshot?.Items?.Any() == true);
                    if (hasCargo) _rightOverlayForm?.Show(); else _rightOverlayForm?.Hide();
                }
                if (AppConfiguration.EnableSessionOverlay) _sessionOverlayForm?.Show();
                if (AppConfiguration.EnableExplorationOverlay) _explorationOverlayForm?.Show();
                if (AppConfiguration.EnableMiningOverlay) _miningOverlayForm?.Show();
                if (AppConfiguration.EnableProspectorOverlay)
                {
                    if (_lastProspectorOverlayData != null)
                    {
                        _prospectorOverlayForm?.Show();
                    }
                    else
                    {
                        _prospectorOverlayForm?.Hide();
                    }
                }
            }
            else
            {
                _leftOverlayForm?.Hide();
                _rightOverlayForm?.Hide();
                _sessionOverlayForm?.Hide();
                _explorationOverlayForm?.Hide();
                _miningOverlayForm?.Hide();
                _prospectorOverlayForm?.Hide();
            }
        }

        private void ShowAllEnabledOverlays()
        {
            EnsureOverlaysCreated(_overlayOwner, force: _forceShowAllOverlays);

            if (_forceShowAllOverlays || AppConfiguration.EnableInfoOverlay) _leftOverlayForm?.Show();
            else _leftOverlayForm?.Hide();

            if (_forceShowAllOverlays || AppConfiguration.EnableCargoOverlay) _rightOverlayForm?.Show();
            else _rightOverlayForm?.Hide();

            if (_forceShowAllOverlays || AppConfiguration.EnableSessionOverlay) _sessionOverlayForm?.Show();
            else _sessionOverlayForm?.Hide();

            if (_forceShowAllOverlays || AppConfiguration.EnableExplorationOverlay) _explorationOverlayForm?.Show();
            else _explorationOverlayForm?.Hide();

            if (_forceShowAllOverlays || AppConfiguration.EnableMiningOverlay)
            {
                _miningOverlayForm?.Show();
                _miningOverlayForm?.UpdateMiningOverlay(_lastMiningOverlayData);
            }
            else
            {
                _miningOverlayForm?.Hide();
            }

            if (_forceShowAllOverlays || AppConfiguration.EnableProspectorOverlay)
            {
                _prospectorOverlayForm?.Show();
                _prospectorOverlayForm?.UpdateProspectorOverlay(_lastProspectorOverlayData);
            }
            else
            {
                _prospectorOverlayForm?.Hide();
            }
        }

        private void PersistOverlayLocations()
        {
            if (_leftOverlayForm != null)
                AppConfiguration.InfoOverlayLocation = _leftOverlayForm.Location;
            if (_rightOverlayForm != null)
                AppConfiguration.CargoOverlayLocation = _rightOverlayForm.Location;
            if (_sessionOverlayForm != null)
                AppConfiguration.SessionOverlayLocation = _sessionOverlayForm.Location;
            if (_explorationOverlayForm != null)
                AppConfiguration.ExplorationOverlayLocation = _explorationOverlayForm.Location;
            if (_miningOverlayForm != null)
                AppConfiguration.MiningOverlayLocation = _miningOverlayForm.Location;
            if (_prospectorOverlayForm != null)
                AppConfiguration.ProspectorOverlayLocation = _prospectorOverlayForm.Location;

            AppConfiguration.Save();
            ExportObsPositions();
        }

        /// <summary>
        /// Gets a reference to a specific, active overlay form.
        /// </summary>
        /// <param name="position">The type of overlay to retrieve.</param>
        /// <returns>The <see cref="OverlayForm"/> instance, or null if not active.</returns>
        public OverlayForm? GetOverlay(OverlayForm.OverlayPosition position)
        {
            return position switch
            {
                OverlayForm.OverlayPosition.Info => _leftOverlayForm,
                OverlayForm.OverlayPosition.Cargo => _rightOverlayForm,
                OverlayForm.OverlayPosition.Session => _sessionOverlayForm,
                OverlayForm.OverlayPosition.Exploration => _explorationOverlayForm,
                OverlayForm.OverlayPosition.Mining => _miningOverlayForm,
                OverlayForm.OverlayPosition.Prospector => _prospectorOverlayForm,
                OverlayForm.OverlayPosition.JumpInfo => _jumpOverlayForm,
                _ => null
            };
        }

        public void Dispose()
        {
            Stop();
        }

        private void EnsureJumpOverlay()
        {
            // Next Jump overlay removed; do nothing
        }
    }
}
