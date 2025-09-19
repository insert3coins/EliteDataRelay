using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms; // Keep for Screen and fallback logic
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.UI;

namespace EliteDataRelay.Services
{
    public class OverlayService : IDisposable
    {
        private const string GameProcessName = "EliteDangerous64";
        private const int LeftHorizontalOffset = 10;
        private const int RightHorizontalOffset = 0;
        private const int VerticalOffset = 0;

        private OverlayForm? _leftOverlayForm;
        private OverlayForm? _rightOverlayForm;

        public void Start()
        {
            try
            {
                // Check if the game is running before showing the overlay.
                // If not, the other services can still run, but the overlay will not appear.
                var gameProcess = Process.GetProcessesByName(GameProcessName).FirstOrDefault();
                if (gameProcess == null || gameProcess.MainWindowHandle == IntPtr.Zero)
                {
                    Debug.WriteLine("[OverlayService] Elite Dangerous process not found. Overlay will not be shown.");
                    return;
                }

                // Use the primary screen's bounds for positioning the overlays, as requested.
                var screenBounds = Screen.PrimaryScreen.Bounds;

                // Create the left overlay for CMDR/Ship/Balance info
                if (AppConfiguration.EnableLeftOverlay && (_leftOverlayForm == null || _leftOverlayForm.IsDisposed))
                {
                    var newLeftOverlay = new OverlayForm(OverlayForm.OverlayPosition.Left);
                    int yPos = screenBounds.Top + (screenBounds.Height - newLeftOverlay.Height) / 2 + VerticalOffset;
                    newLeftOverlay.Location = new Point(screenBounds.Left + LeftHorizontalOffset, yPos);
                    _leftOverlayForm = newLeftOverlay;
                }

                // Create the right overlay for Cargo info
                if (AppConfiguration.EnableRightOverlay && (_rightOverlayForm == null || _rightOverlayForm.IsDisposed))
                {
                    var newRightOverlay = new OverlayForm(OverlayForm.OverlayPosition.Right);
                    int yPos = screenBounds.Top + (screenBounds.Height - newRightOverlay.Height) / 2 + VerticalOffset;
                    newRightOverlay.Location = new Point(
                        screenBounds.Right - newRightOverlay.Width - RightHorizontalOffset,
                        yPos
                    );
                    _rightOverlayForm = newRightOverlay;
                }

                if (AppConfiguration.EnableLeftOverlay && _leftOverlayForm != null && !_leftOverlayForm.IsDisposed)
                {
                    _leftOverlayForm.Show();
                }
                if (AppConfiguration.EnableRightOverlay && _rightOverlayForm != null && !_rightOverlayForm.IsDisposed)
                {
                    _rightOverlayForm.Show();
                }
                Debug.WriteLine("[OverlayService] Overlay started.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayService] Failed to start overlay: {ex.Message}");
            }
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

        public void Stop()
        {
            if (_leftOverlayForm != null && !_leftOverlayForm.IsDisposed)
            {
                _leftOverlayForm.Close();
                _leftOverlayForm = null;
            }
            if (_rightOverlayForm != null && !_rightOverlayForm.IsDisposed)
            {
                _rightOverlayForm.Close();
                _rightOverlayForm = null;
                Debug.WriteLine("[OverlayService] Overlay stopped.");
            }
        }

        public void UpdateCommander(string commanderName) => _leftOverlayForm?.UpdateCommander($"CMDR: {commanderName}");
        public void UpdateShip(string shipName) => _leftOverlayForm?.UpdateShip($"Ship: {shipName}");
        public void UpdateBalance(long balance) => _leftOverlayForm?.UpdateBalance($"Balance: {balance:N0} CR");
        public void UpdateCargo(int count, int? capacity)
        {
            string cargoText = capacity.HasValue
                ? $"Cargo: {count}/{capacity.Value}"
                : $"Cargo: {count}";
            _rightOverlayForm?.UpdateCargo(cargoText);
        }

        public void UpdateCargoSize(string cargoSizeText)
        {
            _rightOverlayForm?.UpdateCargoSize(cargoSizeText);
        }

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            _rightOverlayForm?.UpdateCargoList(snapshot.Inventory);
        }

        public void UpdateSessionOverlay(long cargoCollected, long creditsEarned)
        {
            // Session stats are now on the right overlay.
            // We format the string here before passing it to the form.
            if (_rightOverlayForm != null)
            {
                _rightOverlayForm.UpdateSessionCargoCollected($"Cargo Run: {cargoCollected}");
                _rightOverlayForm.UpdateSessionCreditsEarned($"Credits Run: {creditsEarned:N0}");
            }
        }

        public void Dispose()
        {
            _leftOverlayForm?.Dispose();
            _rightOverlayForm?.Dispose();
        }
    }
}