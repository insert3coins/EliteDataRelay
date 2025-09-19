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

            if (AppConfiguration.EnableLeftOverlay)
            {
                _leftOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Left);
                _leftOverlayForm.PositionChanged += OnOverlayPositionChanged;

                if (AppConfiguration.LeftOverlayLocation != Point.Empty)
                {
                    _leftOverlayForm.Location = AppConfiguration.LeftOverlayLocation;
                }
                else
                {
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    const int leftOverlayHeight = 85; // Height is set in OverlayForm.OnLoad
                    _leftOverlayForm.Location = new Point(20, (screen.Height / 2) - (leftOverlayHeight / 2));
                }
                _leftOverlayForm!.Show();
            }

            if (AppConfiguration.EnableRightOverlay)
            {
                _rightOverlayForm = new OverlayForm(OverlayForm.OverlayPosition.Right);
                _rightOverlayForm.PositionChanged += OnOverlayPositionChanged;

                if (AppConfiguration.RightOverlayLocation != Point.Empty)
                {
                    _rightOverlayForm.Location = AppConfiguration.RightOverlayLocation;
                }
                else
                {
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    const int rightOverlayWidth = 280; // Width is set in OverlayForm.OnLoad
                    const int rightOverlayHeight = 400; // Height is set in OverlayForm.OnLoad
                    _rightOverlayForm.Location = new Point(screen.Width - rightOverlayWidth - 20, (screen.Height / 2) - (rightOverlayHeight / 2));
                }
                _rightOverlayForm!.Show();
            }

            if (AppConfiguration.EnableMaterialsOverlay)
            {
                _materialsOverlay = new OverlayForm(OverlayForm.OverlayPosition.Materials);
                _materialsOverlay.PositionChanged += OnOverlayPositionChanged;

                if (AppConfiguration.MaterialsOverlayLocation != Point.Empty)
                {
                    _materialsOverlay.Location = AppConfiguration.MaterialsOverlayLocation;
                }
                else
                {
                    var screen = Screen.PrimaryScreen.WorkingArea;
                    _materialsOverlay.Location = new Point(screen.Width - 280 - 320, (screen.Height / 2) - (500 / 2));
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