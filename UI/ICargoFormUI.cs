using EliteDataRelay.Models;
using EliteDataRelay.Services;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public interface ICargoFormUI : IDisposable
    {
        // Events
        event EventHandler? StartClicked;
        event EventHandler? StopClicked;
        event EventHandler? ExitClicked;
        event EventHandler? AboutClicked;
        event EventHandler? SettingsClicked;
        event EventHandler? SessionClicked;

        // Methods
        void UpdateShipStatus(StatusFile status);
        void InitializeUI(Form owner);
        void SetButtonStates(bool startEnabled, bool stopEnabled);
        void UpdateMonitoringVisuals(bool isMonitoring);
        void RefreshOverlay();
        void ShowOverlays();
        void HideOverlays();
        void UpdateCargoHeader(int currentCount, int? capacity);
        void UpdateCargoList(CargoSnapshot snapshot);
        void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity);
        void UpdateLocation(string starSystem);
        void UpdateCommanderName(string commanderName);
        void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName);
        void UpdateBalance(long balance);
        void UpdateTitle(string title);
        void UpdateShipLoadout(ShipLoadout loadout);
        void UpdateSessionOverlay(long cargo, long credits);
        void UpdateSystemInfo(SystemInfoData data);
        void UpdateStationInfo(StationInfoData data);
    }
}