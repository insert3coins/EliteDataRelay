using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for managing overlay windows.
    /// </summary>
    public interface IOverlayManager
    {
        /// <summary>
        /// Updates the materials overlay with the current material list.
        /// </summary>
        /// <param name="materialService">The material service containing the data.</param>
        void UpdateMaterialsOverlay(IMaterialService materialService);

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
        void UpdateSessionOverlay(long cargoCollected, long creditsEarned);
    }
}