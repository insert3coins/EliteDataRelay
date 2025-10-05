using EliteDataRelay.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public interface ICargoFormUI : IDisposable
    {
        event EventHandler? StartClicked;
        event EventHandler? StopClicked;
        event EventHandler? ExitClicked;
        event EventHandler? AboutClicked;
        event EventHandler? SettingsClicked;
        event EventHandler? SessionClicked;
        event EventHandler? MiningStartClicked;
        event EventHandler? MiningStopClicked;

        void InitializeUI(Form form);
        void SetButtonStates(bool startEnabled, bool stopEnabled);
        void UpdateMonitoringVisuals(bool isMonitoring);
        void UpdateCargoList(CargoSnapshot cargoSnapshot);
        void UpdateCargoDisplay(CargoSnapshot cargoSnapshot, int? capacity);
        void UpdateBalance(long balance);
        void UpdateCommanderName(string commanderName);
        void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName);
        void UpdateShipLoadout(ShipLoadout loadout);
        void UpdateShipStatus(StatusFile status);
        void UpdateStationInfo(StationInfoData data);
        void UpdateMaterials(MaterialsEvent materials);
        void UpdateSystemInfo(SystemInfoData data);
        void UpdateLocation(string location);
        void UpdateSessionOverlay(int cargoCollected, long creditsEarned);
        void UpdateMiningStats();
        void UpdateTitle(string title);        
        void ShowOverlays();
        void HideOverlays();
        void RefreshOverlay();
        void UpdateCargoScrollBar();
    }
}