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
                foreach (var p in gameProcesses) { p.Dispose(); }
            }

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
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    const int leftOverlayHeight = 85; // Height is set in OverlayForm.OnLoad
                    leftOverlay.Location = new Point(20, (screen.Height / 2) - (leftOverlayHeight / 2));
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
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    const int rightOverlayWidth = 280; // Width is set in OverlayForm.OnLoad
                    const int rightOverlayHeight = 400; // Height is set in OverlayForm.OnLoad
                    rightOverlay.Location = new Point(screen.Width - rightOverlayWidth - 20, (screen.Height / 2) - (rightOverlayHeight / 2));
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
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    // Position to the left of the cargo overlay's default spot to avoid overlap.
                    const int rightOverlayWidth = 280;
                    const int materialsOverlayWidth = 340; // Width is set in OverlayForm.OnLoad
                    const int materialsOverlayHeight = 500; // Height is set in OverlayForm.OnLoad
                    const int padding = 20;

                    int materialsX = screen.Width - rightOverlayWidth - padding - materialsOverlayWidth - padding;
                    materialsOverlay.Location = new Point(materialsX, (screen.Height / 2) - (materialsOverlayHeight / 2));
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

        public void UpdateShip(string ship)
        {
            _leftOverlayForm?.UpdateShip($"Ship: {ship}");
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