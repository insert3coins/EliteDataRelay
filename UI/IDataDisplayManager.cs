using System;
using System.Collections.Generic;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for managing data display on the UI.
    /// </summary>
    public interface IDataDisplayManager
    {
        event EventHandler? ScanJournalsClicked;
        event EventHandler<string>? SearchSystemClicked;

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
        /// <param name="internalShipName">The ship's internal (journal) name.</param>
        void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName);

        /// <summary>
        /// Updates the player's balance display.
        /// </summary>
        /// <param name="balance">The player's credit balance.</param>
        void UpdateBalance(long balance);

        /// <summary>
        /// Updates the ship tab with the current loadout.
        /// </summary>
        /// <param name="loadout">The full ship loadout data.</param>
        void UpdateShipLoadout(ShipLoadout loadout);

        /// <summary>
        /// Updates the star map display with visited systems.
        /// </summary>
        /// <param name="systems">A list of visited systems.</param>
        /// <param name="currentSystem">The name of the current system to highlight.</param>
        void UpdateStarMap(IReadOnlyList<StarSystem> systems, string currentSystem);

        /// <summary>
        /// Centers the star map view on a specific system.
        /// </summary>
        /// <param name="systemName">The name of the system to center on.</param>
        void CenterStarMapOnSystem(string systemName);

        /// <summary>
        /// Sets the current system and centers the star map view on it.
        /// </summary>
        /// <param name="systemName">The name of the system to set and center on.</param>
        void SetAndCenterStarMapOnSystem(string systemName);

        /// <summary>
        /// Highlights a specific system on the star map.
        /// </summary>
        /// <param name="systemName">The name of the system to highlight, or null to clear.</param>
        void HighlightSearchedSystem(string? systemName);

        /// <summary>
        /// Updates the autocomplete source for the star map search box.
        /// </summary>
        /// <param name="systems">A list of visited systems.</param>
        void UpdateStarMapAutocomplete(IReadOnlyList<StarSystem> systems);

        /// <summary>
        /// Shows or hides the journal scan progress bar.
        /// </summary>
        /// <param name="visible">True to show, false to hide.</param>
        void ShowScanProgress(bool visible);

        /// <summary>
        /// Updates the journal scan progress bar and label.
        /// </summary>
        /// <param name="percentage">The progress percentage (0-100).</param>
        /// <param name="message">The status message to display.</param>
        void UpdateScanProgress(int percentage, string message);
    }
}