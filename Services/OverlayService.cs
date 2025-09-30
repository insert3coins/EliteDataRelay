using System;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;

namespace EliteDataRelay.Services
{
    public class OverlayService : IDisposable
    {
        private OverlayForm? _leftOverlayForm;
        private OverlayForm? _rightOverlayForm;
        private OverlayForm? _shipIconOverlayForm;

        public void Start()
        {
            Stop(); // Ensure any existing overlays are closed
            var gameProcesses = System.Diagnostics.Process.GetProcessesByName("EliteDangerous64");

            try
            {
                // Only show overlays if the game process is actually running.
                if (!gameProcesses.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[OverlayService] Elite Dangerous not running. Overlays will not be shown.");
                    return;
                }
            }
            finally
            {
                if (gameProcesses != null)
                {
                    foreach (var p in gameProcesses) { p.Dispose(); }
                }
            }

            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayService] No primary screen detected. Overlays will not be shown.");
                return;
            }

            var screen = primaryScreen.WorkingArea;
            const int screenEdgePadding = 20;
            const int overlaySpacing = 10;

            // Create form instances first to get their actual dimensions.
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

            // Default for Cargo overlay (middle right)
            Point defaultRightLocation = Point.Empty;
            if (_rightOverlayForm != null)
            {
                int y = (screen.Height / 2) - (_rightOverlayForm.Height / 2);
                defaultRightLocation = new Point(screen.Width - _rightOverlayForm.Width - screenEdgePadding, y);
            }

            // --- Calculate positions for the bottom-left overlay stack ---
            // This new logic correctly handles any combination of Info, Ship, and Chat overlays.

            // 1. Calculate the total height of the stack.
            int totalStackHeight = 0;
            if (_leftOverlayForm != null) totalStackHeight += _leftOverlayForm.Height;
            if (_shipIconOverlayForm != null) totalStackHeight += (totalStackHeight > 0 ? overlaySpacing : 0) + _shipIconOverlayForm.Height;

            // 2. Determine the starting Y position for the entire stack (bottom-aligned).
            int currentY = screen.Height - totalStackHeight - screenEdgePadding;

            // 3. Assign positions to each overlay in the stack from top to bottom.
            Point defaultLeftLocation = Point.Empty;
            if (_leftOverlayForm != null)
            {
                defaultLeftLocation = new Point(screenEdgePadding, currentY);
                currentY += _leftOverlayForm.Height + overlaySpacing;
            }

            Point defaultShipIconLocation = Point.Empty;
            if (_shipIconOverlayForm != null)
            {
                defaultShipIconLocation = new Point(screenEdgePadding, currentY);
                currentY += _shipIconOverlayForm.Height + overlaySpacing;
            }

            if (_leftOverlayForm != null)
            {
                if (AppConfiguration.InfoOverlayLocation != Point.Empty)
                {
                    _leftOverlayForm.Location = AppConfiguration.InfoOverlayLocation;
                }
                else
                {
                    _leftOverlayForm.Location = defaultLeftLocation;
                }
                _leftOverlayForm.Show();
            }
            if (_rightOverlayForm != null)
            {
                if (AppConfiguration.CargoOverlayLocation != Point.Empty)
                {
                    _rightOverlayForm.Location = AppConfiguration.CargoOverlayLocation;
                }
                else
                {
                    _rightOverlayForm.Location = defaultRightLocation;
                }
                _rightOverlayForm.Show();
            }
            if (_shipIconOverlayForm != null)
            {
                if (AppConfiguration.ShipIconOverlayLocation != Point.Empty)
                {
                    _shipIconOverlayForm.Location = AppConfiguration.ShipIconOverlayLocation;
                }
                else
                {
                    _shipIconOverlayForm.Location = defaultShipIconLocation;
                }
                _shipIconOverlayForm.Show();
            }
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _shipIconOverlayForm?.Close();
            _leftOverlayForm = null;
            _rightOverlayForm = null;
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

        private void OnOverlayPositionChanged(object? sender, Point newLocation)
        {
            if (sender == _leftOverlayForm)
            {
                AppConfiguration.InfoOverlayLocation = newLocation;
            }
            else if (sender == _rightOverlayForm)
            {
                AppConfiguration.CargoOverlayLocation = newLocation;
            }
            else if (sender == _shipIconOverlayForm)
            {
                AppConfiguration.ShipIconOverlayLocation = newLocation;
            }
            AppConfiguration.Save();
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
            string headerText = capacity.HasValue ? $"Cargo: {count}/{capacity.Value}" : $"Cargo: {count}";
            _rightOverlayForm?.UpdateCargo(count, capacity);
        }

        public void UpdateCargoList(CargoSnapshot snapshot) => _rightOverlayForm?.UpdateCargoList(snapshot.Inventory);

        public void UpdateCargoSize(string size) => _rightOverlayForm?.UpdateCargoSize(size);

        public void UpdateSessionOverlay(long cargo, long credits)
        {
            _rightOverlayForm?.UpdateSessionCreditsEarned(credits);
            _rightOverlayForm?.UpdateSessionCargoCollected(cargo);
        }

        public void UpdateSystemInfo(SystemInfoData data)
        {
            // _systemInfoOverlay?.UpdateSystemInfo(data);
        }

        public void UpdateStationInfo(StationInfoData data)
        {
            // _stationInfoOverlay?.UpdateStationInfo(data);
        }

        #endregion

        public void Dispose()
        {
            Stop();
        }
    }
}