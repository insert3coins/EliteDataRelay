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
        private OverlayForm? _materialsOverlay;

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
            if (AppConfiguration.EnableLeftOverlay)
            {
                _leftOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Left, AppConfiguration.AllowOverlayDrag);
                _leftOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableRightOverlay)
            {
                _rightOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Right, AppConfiguration.AllowOverlayDrag);
                _rightOverlayForm.PositionChanged += OnOverlayPositionChanged;
            }
            if (AppConfiguration.EnableMaterialsOverlay)
            {
                _materialsOverlay = new OverlayForm(OverlayForm.OverlayPosition.Materials, AppConfiguration.AllowOverlayDrag);
                _materialsOverlay.PositionChanged += OnOverlayPositionChanged;
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

            // Default for materials overlay (to the left of the main stack)
            int materialsX = rightStackX - (_materialsOverlay?.Width ?? 0) - screenEdgePadding;
            Point defaultMaterialsLocation = new Point(materialsX, (screen.Height / 2) - ((_materialsOverlay?.Height ?? 0) / 2));

            if (_leftOverlayForm != null)
            {
                if (AppConfiguration.LeftOverlayLocation != Point.Empty)
                {
                    _leftOverlayForm.Location = AppConfiguration.LeftOverlayLocation;
                }
                else
                {
                    _leftOverlayForm.Location = defaultLeftLocation;
                }
                _leftOverlayForm.Show();
            }
            if (_rightOverlayForm != null)
            {
                if (AppConfiguration.RightOverlayLocation != Point.Empty)
                {
                    _rightOverlayForm.Location = AppConfiguration.RightOverlayLocation;
                }
                else
                {
                    _rightOverlayForm.Location = defaultRightLocation;
                }
                _rightOverlayForm.Show();
            }
            if (_materialsOverlay != null)
            {
                if (AppConfiguration.MaterialsOverlayLocation != Point.Empty)
                {
                    _materialsOverlay.Location = AppConfiguration.MaterialsOverlayLocation;
                }
                else
                {
                    _materialsOverlay.Location = defaultMaterialsLocation;
                }
                _materialsOverlay.Show();
            }
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _materialsOverlay?.Close();
            _leftOverlayForm = null;
            _rightOverlayForm = null;
            _materialsOverlay = null;
        }

        public void Show()
        {
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
            _materialsOverlay?.Show();
        }

        public void Hide()
        {
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
            _materialsOverlay?.Hide();
        }

        private void OnOverlayPositionChanged(object? sender, Point newLocation)
        {
            if (sender == _leftOverlayForm)
            {
                AppConfiguration.LeftOverlayLocation = newLocation;
            }
            else if (sender == _rightOverlayForm)
            {
                AppConfiguration.RightOverlayLocation = newLocation;
            }
            else if (sender == _materialsOverlay)
            {
                AppConfiguration.MaterialsOverlayLocation = newLocation;
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

        public void UpdateMaterials(IMaterialService materialService)
        {
            _materialsOverlay?.UpdateMaterials(materialService);
        }

        #endregion

        public void Dispose()
        {
            Stop();
        }
    }
}