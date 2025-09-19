using System;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for cargo form UI management
    /// </summary>
    public interface ICargoFormUI : IDisposable
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

        /// <summary>
        /// Event raised when the session button is clicked
        /// </summary>
        event EventHandler? SessionClicked;

        /// <summary>
        /// Initialize the UI components and layout
        /// </summary>
        /// <param name="form">The main form to initialize</param>
        void InitializeUI(Form form);

        /// <summary>
        /// Update the UI with new cargo data
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to display</param>
        /// <param name="cargoCapacity">The total cargo capacity</param>
        void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity);

        /// <summary>
        /// Updates the header display with the current cargo count.
        /// </summary>
        /// <param name="currentCount">The current number of items in cargo.</param>
        /// <param name="capacity">The total cargo capacity.</param>
        void UpdateCargoHeader(int currentCount, int? capacity);

        /// <summary>
        /// Updates the main display with the current cargo list.
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to display.</param>
        void UpdateCargoList(CargoSnapshot snapshot);

        /// <summary>
        /// Updates the material display with the current material list.
        /// </summary>
        /// <param name="materialService">The material service containing the data.</param>
        void UpdateMaterialList(IMaterialService materialService);

        /// <summary>
        /// Updates the materials overlay with the current material list.
        /// </summary>
        /// <param name="materialService">The material service containing the data.</param>
        void UpdateMaterialsOverlay(IMaterialService materialService);

        /// <summary>
        /// Update the form title.
        /// </summary>
        /// <param name="title">New title text</param>
        void UpdateTitle(string title);

        /// <summary>
        /// Update the location display.
        /// </summary>
        /// <param name="starSystem">The name of the star system.</param>
        void UpdateLocation(string starSystem);

        /// <summary>
        /// Updates the commander name display.
        /// </summary>
        /// <param name="commanderName">The commander's name.</param>
        void UpdateCommanderName(string commanderName);

        /// <summary>
        /// Updates the ship info display.
        /// </summary>
        /// <param name="shipName">The ship's custom name.</param>
        /// <param name="shipIdent">The ship's ID.</param>
        void UpdateShipInfo(string shipName, string shipIdent);

        /// <summary>
        /// Updates the player's balance display.
        /// </summary>
        /// <param name="balance">The player's credit balance.</param>
        void UpdateBalance(long balance);

        /// <summary>
        /// Set the enabled state of the start and stop buttons
        /// </summary>
        /// <param name="startEnabled">Whether start button should be enabled</param>
        /// <param name="stopEnabled">Whether stop button should be enabled</param>
        void SetButtonStates(bool startEnabled, bool stopEnabled);

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

        /// <summary>
        /// Sets the visibility of the session button.
        /// </summary>
        /// <param name="visible">Whether the button should be visible.</param>
        void SetSessionButtonVisibility(bool visible);

        /// <summary>Updates the session data on the overlay.</summary>
        void UpdateSessionOverlay(long cargoCollected, long creditsEarned);
    }
}