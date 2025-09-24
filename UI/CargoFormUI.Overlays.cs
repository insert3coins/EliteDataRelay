using EliteDataRelay.Configuration;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public void RefreshOverlay()
        {
            _overlayService?.Stop();
            if (AppConfiguration.EnableInfoOverlay || AppConfiguration.EnableCargoOverlay || AppConfiguration.EnableSystemInfoOverlay || AppConfiguration.EnableStationInfoOverlay)
            {
                _overlayService?.Start();
            }
        }

        public void ShowOverlays()
        {
            _overlayService?.Show();
        }

        public void HideOverlays()
        {
            _overlayService?.Hide();
        }

        public void UpdateSessionOverlay(long cargoCollected, long creditsEarned)
        {
            // This assumes the OverlayService has a corresponding method
            // that will format the data and pass it to the overlay form.
            _overlayService?.UpdateSessionOverlay(cargoCollected, creditsEarned);
        }
    }
}