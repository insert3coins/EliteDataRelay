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
        private OverlayForm? _explorationOverlayForm;

        // Cache last known data to restore on overlay refresh
        private SystemExplorationData? _lastExplorationData;
        private ExplorationSessionData? _lastExplorationSessionData;
        private string? _lastCommanderName;
        private string? _lastShipType;
        private long? _lastBalance;
        private int? _lastCargoCount;
        private int? _lastCargoCapacity;
        private string? _lastCargoBarText;
        private long? _lastSessionCargo;
        private long? _lastSessionCredits;
        private Image? _lastShipIcon;
        private CargoSnapshot? _lastCargoSnapshot;

        public void Start()
        {
            Stop(); // Ensure any existing overlays are closed

            try
            {
                var gameProcesses = System.Diagnostics.Process.GetProcessesByName("EliteDangerous64");
                // Only show overlays if the game process is actually running.
                if (!gameProcesses.Any())
                {
                    Logger.Verbose("[OverlayService] Elite Dangerous not running. Overlays will not be shown.");
                    return;
                }
            }
            catch { /* Ignore potential access denied errors */ }

            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                Logger.Verbose("[OverlayService] No primary screen detected. Overlays will not be shown.");
                return;
            }

            var screen = primaryScreen.WorkingArea;

            if (AppConfiguration.EnableInfoOverlay)
            {
                _leftOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Info, AppConfiguration.AllowOverlayDrag);
                _leftOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableCargoOverlay)
            {
                _rightOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Cargo, AppConfiguration.AllowOverlayDrag);
                _rightOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableShipIconOverlay)
            {
                _shipIconOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.ShipIcon, AppConfiguration.AllowOverlayDrag);
                _shipIconOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableExplorationOverlay)
            {
                _explorationOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Exploration, AppConfiguration.AllowOverlayDrag);
                _explorationOverlayForm.PositionChanged += OnOverlayPositionChanged;
                Logger.Verbose("[OverlayService] Exploration overlay created");
            }

            PositionOverlays(screen);

            // Show and restore data for Info overlay
            if (_leftOverlayForm != null)
            {
                _leftOverlayForm.Show();
                if (_lastCommanderName != null) _leftOverlayForm.UpdateCommander(_lastCommanderName);
                if (_lastShipType != null) _leftOverlayForm.UpdateShip(_lastShipType);
                if (_lastBalance.HasValue) _leftOverlayForm.UpdateBalance(_lastBalance.Value);
            }

            // Show and restore data for Cargo overlay
            if (_rightOverlayForm != null)
            {
                _rightOverlayForm.Show();
                if (_lastCargoCount.HasValue) _rightOverlayForm.UpdateCargo(_lastCargoCount.Value, _lastCargoCapacity);
                if (_lastCargoBarText != null) _rightOverlayForm.UpdateCargoSize(_lastCargoBarText);
                if (_lastCargoSnapshot != null) _rightOverlayForm.UpdateCargoList(_lastCargoSnapshot.Items);
                if (_lastSessionCredits.HasValue) _rightOverlayForm.UpdateSessionCreditsEarned(_lastSessionCredits.Value);
                if (_lastSessionCargo.HasValue) _rightOverlayForm.UpdateSessionCargoCollected(_lastSessionCargo.Value);
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
                Logger.Verbose($"[OverlayService] Showing exploration overlay at {_explorationOverlayForm.Location}");
                _explorationOverlayForm.Show();

                // Restore last known exploration data after recreating overlay
                if (_lastExplorationData != null)
                {
                    Logger.Verbose($"[OverlayService] Restoring exploration data: {_lastExplorationData.SystemName}");
                    _explorationOverlayForm.UpdateExplorationData(_lastExplorationData);
                }
                if (_lastExplorationSessionData != null)
                {
                    _explorationOverlayForm.UpdateExplorationSessionData(_lastExplorationSessionData);
                }
            }

            // Export overlay positions for OBS
            ExportObsPositions();
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _shipIconOverlayForm?.Close();
            _explorationOverlayForm?.Close();

            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _shipIconOverlayForm = null;
            _explorationOverlayForm = null;
        }

        public void Show()
        {
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _shipIconOverlayForm?.Show();
            _explorationOverlayForm?.Show();
        }

        public void Hide()
        {
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _shipIconOverlayForm?.Hide();
            _explorationOverlayForm?.Hide();
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
                if (AppConfiguration.EnableCargoOverlay) _rightOverlayForm?.Show();
                if (AppConfiguration.EnableShipIconOverlay) _shipIconOverlayForm?.Show();
                if (AppConfiguration.EnableExplorationOverlay) _explorationOverlayForm?.Show();
            }
            else
            {
                _leftOverlayForm?.Hide();
                _rightOverlayForm?.Hide();
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
                OverlayForm.OverlayPosition.ShipIcon => _shipIconOverlayForm,
                OverlayForm.OverlayPosition.Exploration => _explorationOverlayForm,
                _ => null
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
