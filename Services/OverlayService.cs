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

            // Default for Cargo overlay (middle right)
            Point defaultRightLocation = Point.Empty;
            if (_rightOverlayForm != null)
            {
                int y = (screen.Height / 2) - (_rightOverlayForm.Height / 2);
                defaultRightLocation = new Point(screen.Width - _rightOverlayForm.Width - screenEdgePadding, y);
            }

            // Default for System Info overlay (bottom right)
            Point defaultSystemInfoLocation = Point.Empty;
            if (_systemInfoOverlay != null)
            {
                int x = screen.Width - _systemInfoOverlay.Width - screenEdgePadding;
                int y = screen.Height - _systemInfoOverlay.Height - screenEdgePadding;
                defaultSystemInfoLocation = new Point(x, y);
            }

            // Default for Info overlay (above System Info)
            Point defaultLeftLocation = Point.Empty;
            if (_leftOverlayForm != null)
            {
                int x = screen.Width - _leftOverlayForm.Width - screenEdgePadding;
                int y = screen.Height - (_systemInfoOverlay?.Height ?? 0) - _leftOverlayForm.Height - screenEdgePadding - overlaySpacing;
                defaultLeftLocation = new Point(x, y);
            }

            // Default for Station Info overlay (bottom left, unchanged)
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

            // Only show the station info overlay if it has valid data (i.e., we are docked).
            // The UpdateStationInfo method handles setting the visibility based on docked status,
            // but a global "Show" command can override it. This check prevents that.
            if (_stationInfoOverlay != null && _stationInfoOverlay.Visible)
            {
                _stationInfoOverlay.Show();
            }
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
            _leftOverlayForm?.UpdateCommander(name);
        }

        public void UpdateShip(string shipName, string shipIdent, string shipType)
        {
            _leftOverlayForm?.UpdateShip(shipType);
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