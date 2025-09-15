using System;
using System.Windows.Forms;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.UI
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
        /// Append text to the display
        /// </summary>
        /// <param name="text">Text to append</param>
        void AppendText(string text);

        /// <summary>
        /// Update the form title
        /// </summary>
        /// <param name="title">New title text</param>
        void UpdateTitle(string title);

        /// <summary>
        /// Set the enabled state of the start and stop buttons
        /// </summary>
        /// <param name="startEnabled">Whether start button should be enabled</param>
        /// <param name="stopEnabled">Whether stop button should be enabled</param>
        void SetButtonStates(bool startEnabled, bool stopEnabled);
    }
}