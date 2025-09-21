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

            var screen = Screen.PrimaryScreen.WorkingArea;
            const int screenEdgePadding = 20;
            const int overlaySpacing = 10; // Space between overlays

            // Define overlay dimensions. These are set in OverlayForm.cs.
            const int leftOverlayHeight = 85;
            const int rightOverlayWidth = 280;
            const int rightOverlayHeight = 400;
            const int materialsOverlayWidth = 340;
            const int materialsOverlayHeight = 500;

            // Calculate default positions.
            // The right (cargo) and left (info) overlays are now stacked vertically on the right.
            // We calculate the total height of the stack to vertically center it on the screen.
            int totalVerticalHeight = 0;
            if (AppConfiguration.EnableLeftOverlay)
            {
                totalVerticalHeight += leftOverlayHeight;
            }
            if (AppConfiguration.EnableRightOverlay)
            {
                totalVerticalHeight += rightOverlayHeight;
            }
            // Add spacing only if both are enabled and will be stacked.
            if (AppConfiguration.EnableLeftOverlay && AppConfiguration.EnableRightOverlay)
            {
                totalVerticalHeight += overlaySpacing;
            }

            int startY = (screen.Height / 2) - (totalVerticalHeight / 2);
            int currentY = startY;

            // Default X for the right-side stack.
            int rightStackX = screen.Width - rightOverlayWidth - screenEdgePadding;

            Point defaultLeftLocation = Point.Empty;
            if (AppConfiguration.EnableLeftOverlay)
            {
                defaultLeftLocation = new Point(rightStackX, currentY);
                currentY += leftOverlayHeight + overlaySpacing;
            }

            Point defaultRightLocation = Point.Empty;
            if (AppConfiguration.EnableRightOverlay)
            {
                defaultRightLocation = new Point(rightStackX, currentY);
            }

            // Default for materials overlay (to the left of the main stack)
            int materialsX = rightStackX - materialsOverlayWidth - screenEdgePadding;
            Point defaultMaterialsLocation = new Point(materialsX, (screen.Height / 2) - (materialsOverlayHeight / 2));

            if (AppConfiguration.EnableLeftOverlay)
            {
                var leftOverlay = new OverlayForm(OverlayForm.OverlayPosition.Left, AppConfiguration.AllowOverlayDrag);
                leftOverlay.PositionChanged += OnOverlayPositionChanged;
                if (AppConfiguration.LeftOverlayLocation != Point.Empty)
                {
                    leftOverlay.Location = AppConfiguration.LeftOverlayLocation;
                }
                else
                {
                    leftOverlay.Location = defaultLeftLocation;
                }
                leftOverlay.Show();
                _leftOverlayForm = leftOverlay;
            }
            if (AppConfiguration.EnableRightOverlay)
            {
                var rightOverlay = new OverlayForm(OverlayForm.OverlayPosition.Right, AppConfiguration.AllowOverlayDrag);
                rightOverlay.PositionChanged += OnOverlayPositionChanged;
                if (AppConfiguration.RightOverlayLocation != Point.Empty)
                {
                    rightOverlay.Location = AppConfiguration.RightOverlayLocation;
                }
                else
                {
                    rightOverlay.Location = defaultRightLocation;
                }
                rightOverlay.Show();
                _rightOverlayForm = rightOverlay;
            }
            if (AppConfiguration.EnableMaterialsOverlay)
            {
                var materialsOverlay = new OverlayForm(OverlayForm.OverlayPosition.Materials, AppConfiguration.AllowOverlayDrag);
                materialsOverlay.PositionChanged += OnOverlayPositionChanged;
                if (AppConfiguration.MaterialsOverlayLocation != Point.Empty)
                {
                    materialsOverlay.Location = AppConfiguration.MaterialsOverlayLocation;
                }
                else
                {
                    materialsOverlay.Location = defaultMaterialsLocation;
                }
                materialsOverlay.Show();
                _materialsOverlay = materialsOverlay;
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