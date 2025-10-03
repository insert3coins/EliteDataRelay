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

        public void Start()
        {
            Stop(); // Ensure any existing overlays are closed

            try
            {
                var gameProcesses = System.Diagnostics.Process.GetProcessesByName("EliteDangerous64");
                // Only show overlays if the game process is actually running.
                if (!gameProcesses.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[OverlayService] Elite Dangerous not running. Overlays will not be shown.");
                    return;
                }
                foreach (var p in gameProcesses) { p.Dispose(); }
            }
            catch { /* Ignore potential access denied errors */ }

            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayService] No primary screen detected. Overlays will not be shown.");
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

            PositionOverlays(screen);

            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _shipIconOverlayForm?.Show();
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _shipIconOverlayForm?.Close();

            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _shipIconOverlayForm = null;
        }

        public void Show()
        {
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _shipIconOverlayForm?.Show();
        }

        public void Hide()
        {
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _shipIconOverlayForm?.Hide();
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
                _ => null
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }
}