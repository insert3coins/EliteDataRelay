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
        private OverlayForm? _shipIconOverlayForm;
        private OverlayForm? _sessionOverlayForm;
        private OverlayForm? _explorationOverlayForm;
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
        private Image? _lastShipIcon;
        private CargoSnapshot? _lastCargoSnapshot;

        // Debounce for exploration overlay updates to avoid rapid churn at startup
        private System.Threading.Timer? _explorationDebounceTimer;
        private readonly object _explorationDebounceLock = new object();
        private TimeSpan _explorationDebounceDelay = TimeSpan.FromMilliseconds(500);

        

        private void EnsureOverlaysCreated(Form? owner = null)
        {
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayService] No primary screen detected. Overlays will not be created.");
                return;
            }

            var screen = primaryScreen.WorkingArea;

            if (_leftOverlayForm == null && AppConfiguration.EnableInfoOverlay)
            {
                _leftOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Info, AppConfiguration.AllowOverlayDrag) { Owner = owner };
                _leftOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_rightOverlayForm == null && AppConfiguration.EnableCargoOverlay)
            {
                _rightOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Cargo, AppConfiguration.AllowOverlayDrag) { Owner = owner };
                _rightOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_sessionOverlayForm == null && AppConfiguration.EnableSessionOverlay)
            {
                _sessionOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Session, AppConfiguration.AllowOverlayDrag) { Owner = owner };
                _sessionOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_shipIconOverlayForm == null && AppConfiguration.EnableShipIconOverlay)
            {
                _shipIconOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.ShipIcon, AppConfiguration.AllowOverlayDrag) { Owner = owner };
                _shipIconOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (_explorationOverlayForm == null && AppConfiguration.EnableExplorationOverlay)
            {
                _explorationOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Exploration, AppConfiguration.AllowOverlayDrag) { Owner = owner };
                _explorationOverlayForm.PositionChanged += OnOverlayPositionChanged;
                System.Diagnostics.Debug.WriteLine("[OverlayService] Exploration overlay created");
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
                if (hasCargo) _rightOverlayForm.Show(); else _rightOverlayForm.Hide();

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

            // Show and restore data for Ship Icon overlay
            if (_shipIconOverlayForm != null)
            {
                _shipIconOverlayForm.Show();
                if (_lastShipIcon != null) _shipIconOverlayForm.UpdateShipIcon(_lastShipIcon);
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

            // Jump overlay removed

            // Export overlay positions for OBS
            ExportObsPositions();
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _sessionOverlayForm?.Close();
            _shipIconOverlayForm?.Close();
            _explorationOverlayForm?.Close();
            _jumpOverlayForm?.Close();

            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _sessionOverlayForm = null;
            _shipIconOverlayForm = null;
            _explorationOverlayForm = null;
            _jumpOverlayForm = null;

            lock (_explorationDebounceLock)
            {
                _explorationDebounceTimer?.Dispose();
                _explorationDebounceTimer = null;
            }
        }

        public void Show()
        {
            EnsureOverlaysCreated(_leftOverlayForm?.Owner); // Pass existing owner if available
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _sessionOverlayForm?.Show();
            _shipIconOverlayForm?.Show();
            _explorationOverlayForm?.Show();
            // Jump overlay is transient; do not force show here
        }

        public void Hide()
        {
            EnsureOverlaysCreated(_leftOverlayForm?.Owner); // Pass existing owner if available
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _sessionOverlayForm?.Hide();
            _shipIconOverlayForm?.Hide();
            _explorationOverlayForm?.Hide();
            _jumpOverlayForm?.Hide();
        }

        /// <summary>
        /// Sets the visibility of the overlays based on the provided boolean.
        /// Only shows overlays that are enabled in the application configuration.
        /// </summary>
        /// <param name="visible">True to show enabled overlays, false to hide all overlays.</param>
        public void SetVisibility(bool visible)
        {
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
                if (AppConfiguration.EnableShipIconOverlay) _shipIconOverlayForm?.Show();
                if (AppConfiguration.EnableExplorationOverlay) _explorationOverlayForm?.Show();
            }
            else
            {
                _leftOverlayForm?.Hide();
                _rightOverlayForm?.Hide();
                _sessionOverlayForm?.Hide();
                _shipIconOverlayForm?.Hide();
                _explorationOverlayForm?.Hide();
            }
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
                OverlayForm.OverlayPosition.ShipIcon => _shipIconOverlayForm,
                OverlayForm.OverlayPosition.Exploration => _explorationOverlayForm,
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
