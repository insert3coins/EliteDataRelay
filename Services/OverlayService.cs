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

        public void Start()
        {
            Stop(); // Ensure any existing overlays are closed

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
        }

        public void Stop()
        {
            _leftOverlayForm?.Close();
            _rightOverlayForm?.Close();
            _leftOverlayForm = null;
            _rightOverlayForm = null;
        }

        public void Show()
        {
            _leftOverlayForm?.Show();
            _rightOverlayForm?.Show();
        }

        public void Hide()
        {
            _leftOverlayForm?.Hide();
            _rightOverlayForm?.Hide();
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
            AppConfiguration.Save();
        }

        #region Data Update Methods
        // These methods would update the UI controls on the specific overlay forms.
        // Using BeginInvoke ensures thread safety, as data updates can come from background threads.

        public void UpdateCommander(string name) => _leftOverlayForm?.BeginInvoke(() =>
        {
            _leftOverlayForm.UpdateCommander($"CMDR: {name}");
        });

        public void UpdateShip(string ship) => _leftOverlayForm?.BeginInvoke(() =>
        {
            _leftOverlayForm.UpdateShip($"Ship: {ship}");
        });

        public void UpdateBalance(long balance) => _leftOverlayForm?.BeginInvoke(() =>
        {
            _leftOverlayForm.UpdateBalance($"Balance: {balance:N0} CR");
        });

        public void UpdateCargo(int count, int? capacity) => _rightOverlayForm?.BeginInvoke(() =>
        {
            string headerText = capacity.HasValue ? $"Cargo: {count}/{capacity.Value}" : $"Cargo: {count}";
            _rightOverlayForm.UpdateCargo(headerText);
        });

        public void UpdateCargoList(CargoSnapshot snapshot) => _rightOverlayForm?.BeginInvoke(() => { _rightOverlayForm.UpdateCargoList(snapshot.Inventory); });

        public void UpdateCargoSize(string size) => _rightOverlayForm?.BeginInvoke(() => { _rightOverlayForm.UpdateCargoSize(size); });

        public void UpdateSessionOverlay(long cargo, long credits) => _rightOverlayForm?.BeginInvoke(() => {
            _rightOverlayForm.UpdateSessionCreditsEarned($"CR/hr: {credits:N0}");
            _rightOverlayForm.UpdateSessionCargoCollected($"Cargo/hr: {cargo}");
        });

        #endregion

        public void Dispose()
        {
            Stop();
        }
    }
}