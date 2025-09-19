using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms; // Keep for Screen and fallback logic
using EliteDataRelay.Models;
using EliteDataRelay.UI;

namespace EliteDataRelay.Services
{
    public class OverlayService : IDisposable
    {
        private const string GameProcessName = "EliteDangerous64";
        private OverlayForm? _leftOverlayForm;
        private OverlayForm? _rightOverlayForm;

        public void Start()
        {
            try
            {
                // Use the primary screen's bounds for positioning the overlays, as requested.
                var screenBounds = Screen.PrimaryScreen.Bounds;

                // Create the left overlay for CMDR/Ship/Balance info
                if (_leftOverlayForm == null || _leftOverlayForm.IsDisposed)
                {
                    var newLeftOverlay = new OverlayForm(OverlayForm.OverlayPosition.Left);
                    int overlayHeight = newLeftOverlay.Height;
                    newLeftOverlay.Location = new Point(screenBounds.Left, screenBounds.Top + (screenBounds.Height - overlayHeight) / 2);
                    _leftOverlayForm = newLeftOverlay;
                }

                // Create the right overlay for Cargo info
                if (_rightOverlayForm == null || _rightOverlayForm.IsDisposed)
                {
                    var newRightOverlay = new OverlayForm(OverlayForm.OverlayPosition.Right);
                    int overlayHeight = newRightOverlay.Height;
                    newRightOverlay.Location = new Point(
                        screenBounds.Right - newRightOverlay.Width,
                        screenBounds.Top + (screenBounds.Height - overlayHeight) / 2
                    );
                    _rightOverlayForm = newRightOverlay;
                }

                _leftOverlayForm.Show();
                _rightOverlayForm.Show();
                Debug.WriteLine("[OverlayService] Overlay started.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayService] Failed to start overlay: {ex.Message}");
            }
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

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            _rightOverlayForm?.UpdateCargoList(snapshot.Inventory);
        }

        public void Dispose()
        {
            _leftOverlayForm?.Dispose();
            _rightOverlayForm?.Dispose();
        }
    }
}