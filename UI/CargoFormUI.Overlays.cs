using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public void UpdateSessionOverlay(SessionOverlayData data)
        {
            // This assumes the OverlayService has a corresponding method
            // that will format the data and pass it to the overlay form.
            _overlayService?.UpdateSessionOverlay(data);
        }
    }
}
