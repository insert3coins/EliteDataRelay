using System;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for UI event management.
    /// </summary>
    public interface IUIEventManager
    {
        /// <summary>
        /// Event raised when the start button is clicked
        /// </summary>
        event EventHandler? StartClicked;

        /// <summary>
        /// Event raised when the stop button is clicked  
        /// </summary>
        event EventHandler? StopClicked;

        /// <summary>
        /// Event raised when the exit button is clicked
        /// </summary>
        event EventHandler? ExitClicked;

        /// <summary>
        /// Event raised when the about button is clicked
        /// </summary>
        event EventHandler? AboutClicked;

        /// <summary>
        /// Event raised when the settings button is clicked
        /// </summary>
        event EventHandler? SettingsClicked;

    }
}
