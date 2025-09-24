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
        private OverlayForm? _systemInfoOverlay;
        private OverlayForm? _stationInfoOverlay;

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
            const int overlaySpacing = 10; // Space between overlays

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
            if (AppConfiguration.EnableSystemInfoOverlay)
            {
                _systemInfoOverlay = new OverlayForm(OverlayForm.OverlayPosition.SystemInfo, AppConfiguration.AllowOverlayDrag);
                _systemInfoOverlay.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableStationInfoOverlay)
            {
                _stationInfoOverlay = new OverlayForm(OverlayForm.OverlayPosition.StationInfo, AppConfiguration.AllowOverlayDrag);
                _stationInfoOverlay.PositionChanged += OnOverlayPositionChanged;
            }

            // Calculate default positions.
            // The right (cargo) and left (info) overlays are now stacked vertically on the right.
            // We calculate the total height of the stack to vertically center it on the screen.
            int totalVerticalHeight = 0;
            if (_leftOverlayForm != null)
            {
                totalVerticalHeight += _leftOverlayForm.Height;
            }
            if (_rightOverlayForm != null)
            {
                totalVerticalHeight += _rightOverlayForm.Height;
            }
            // Add spacing only if both are enabled and will be stacked.
            if (_leftOverlayForm != null && _rightOverlayForm != null)
            {
                totalVerticalHeight += overlaySpacing;
            }

            int startY = (screen.Height / 2) - (totalVerticalHeight / 2);
            int currentY = startY;

            // Default X for the right-side stack.
            int rightStackX = screen.Width - (_rightOverlayForm?.Width ?? _leftOverlayForm?.Width ?? 0) - screenEdgePadding;

            Point defaultLeftLocation = Point.Empty;
            if (_leftOverlayForm != null)
            {
                defaultLeftLocation = new Point(rightStackX, currentY);
                currentY += _leftOverlayForm.Height + overlaySpacing;
            }

            Point defaultRightLocation = Point.Empty;
            if (_rightOverlayForm != null)
            {
                defaultRightLocation = new Point(rightStackX, currentY);
            }

            // Default for System Info overlay (bottom right)
            Point defaultSystemInfoLocation = Point.Empty;
            if (_systemInfoOverlay != null)
            {
                defaultSystemInfoLocation = new Point(screen.Width - _systemInfoOverlay.Width - screenEdgePadding, screen.Height - _systemInfoOverlay.Height - screenEdgePadding);
            }

            // Default for Station Info overlay (bottom left)
            Point defaultStationInfoLocation = Point.Empty;
            if (_stationInfoOverlay != null)
            {
                defaultStationInfoLocation = new Point(screenEdgePadding, screen.Height - _stationInfoOverlay.Height - screenEdgePadding);
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
            if (_systemInfoOverlay != null)
            {
                if (AppConfiguration.SystemInfoOverlayLocation != Point.Empty)
                {
                    _systemInfoOverlay.Location = AppConfiguration.SystemInfoOverlayLocation;
                }
                else
                {
                    _systemInfoOverlay.Location = defaultSystemInfoLocation;
                }
                _systemInfoOverlay.Show();
            }
            if (_stationInfoOverlay != null)
            {
                if (AppConfiguration.StationInfoOverlayLocation != Point.Empty)
                {
                    _stationInfoOverlay.Location = AppConfiguration.StationInfoOverlayLocation;
                }
                else
                {
                    _stationInfoOverlay.Location = defaultStationInfoLocation;
                }
                _stationInfoOverlay.Show();
            }
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _systemInfoOverlay?.Close();
            _stationInfoOverlay?.Close();
            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _systemInfoOverlay = null;
            _stationInfoOverlay = null;
        }

        public void Show()
        {
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _systemInfoOverlay?.Show();
            _stationInfoOverlay?.Show();
        }

        public void Hide()
        {
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _systemInfoOverlay?.Hide();
            _stationInfoOverlay?.Hide();
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
            else if (sender == _systemInfoOverlay)
            {
                AppConfiguration.SystemInfoOverlayLocation = newLocation;
            }
            else if (sender == _stationInfoOverlay)
            {
                AppConfiguration.StationInfoOverlayLocation = newLocation;
            }
            AppConfiguration.Save();
        }

        #region Data Update Methods
        // These methods update the UI controls on the specific overlay forms.
        // The OverlayForm itself handles thread safety with InvokeRequired checks.

        public void UpdateCommander(string name)
        {
            _leftOverlayForm?.UpdateCommander($"CMDR: {name}");
        }

        public void UpdateShip(string shipName, string shipIdent, string shipType)
        {
            _leftOverlayForm?.UpdateShip($"Ship: {shipType}");
        }

        public void UpdateBalance(long balance)
        {
            _leftOverlayForm?.UpdateBalance($"Balance: {balance:N0} CR");
        }

        public void UpdateCargo(int count, int? capacity)
        {
            string headerText = capacity.HasValue ? $"Cargo: {count}/{capacity.Value}" : $"Cargo: {count}";
            _rightOverlayForm?.UpdateCargo(headerText);
        }

        public void UpdateCargoList(CargoSnapshot snapshot) => _rightOverlayForm?.UpdateCargoList(snapshot.Inventory);

        public void UpdateCargoSize(string size) => _rightOverlayForm?.UpdateCargoSize(size);

        public void UpdateSessionOverlay(long cargo, long credits)
        {
            _rightOverlayForm?.UpdateSessionCreditsEarned($"Session CR: {credits:N0}");
            _rightOverlayForm?.UpdateSessionCargoCollected($"Session Cargo: {cargo}");
        }

        public void UpdateSystemInfo(SystemInfoData data)
        {
            _systemInfoOverlay?.UpdateSystemInfo(data);
        }

        public void UpdateStationInfo(StationInfoData data)
        {
            _stationInfoOverlay?.UpdateStationInfo(data);
        }

        #endregion

        public void Dispose()
        {
            Stop();
        }
    }
}