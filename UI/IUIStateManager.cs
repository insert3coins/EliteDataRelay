namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for managing the state of UI controls.
    /// </summary>
    public interface IUIStateManager
    {
        /// <summary>
        /// Set the enabled state of the start and stop buttons
        /// </summary>
        /// <param name="startEnabled">Whether start button should be enabled</param>
        /// <param name="stopEnabled">Whether stop button should be enabled</param>
        void SetButtonStates(bool startEnabled, bool stopEnabled);

        /// <summary>
        /// Updates non-button UI elements to reflect the monitoring state.
        /// </summary>
        /// <param name="isMonitoring">Whether monitoring is active.</param>
        void UpdateMonitoringVisuals(bool isMonitoring);
    }
}



