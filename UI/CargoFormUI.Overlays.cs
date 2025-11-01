using EliteDataRelay.Configuration;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public void RefreshOverlay()
        {
            _overlayService?.Stop();
            if (AppConfiguration.EnableInfoOverlay ||
                AppConfiguration.EnableCargoOverlay ||
                AppConfiguration.EnableShipIconOverlay ||
                AppConfiguration.EnableExplorationOverlay ||
                AppConfiguration.EnableJumpOverlay)
            {
                _overlayService?.Start();
            }
        }

        public void UpdateSessionOverlay(long cargoCollected, long creditsEarned)
        {
            // This assumes the OverlayService has a corresponding method
            // that will format the data and pass it to the overlay form.
            _overlayService?.UpdateSessionOverlay(cargoCollected, creditsEarned);
        }
    }
}
