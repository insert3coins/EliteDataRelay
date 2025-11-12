using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for managing overlay windows.
    /// </summary>
    public interface IOverlayManager
    {
        /// <summary>
        /// Stops and starts the overlay service to apply new settings.
        /// </summary>
        void RefreshOverlay();

        /// <summary>
        /// Shows the overlay windows if they are hidden.
        /// </summary>
        void ShowOverlays();

        /// <summary>
        /// Hides the overlay windows.
        /// </summary>
        void HideOverlays();

        /// <summary>Updates the session data on the overlay.</summary>
        void UpdateSessionOverlay(SessionOverlayData data);
    }
}
