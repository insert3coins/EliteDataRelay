using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for managing data display on the UI.
    /// </summary>
    public interface IDataDisplayManager
    {
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
        /// <param name="shipType">The ship's type/model.</param>
        void UpdateShipInfo(string shipName, string shipIdent, string shipType);

        /// <summary>
        /// Updates the player's balance display.
        /// </summary>
        /// <param name="balance">The player's credit balance.</param>
        void UpdateBalance(long balance);
    }
}