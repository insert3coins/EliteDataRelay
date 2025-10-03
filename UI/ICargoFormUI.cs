using EliteDataRelay.Models;
using System;
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
        void UpdateCargoHeader(int count, int? capacity);
        void UpdateCargoList(CargoSnapshot snapshot);
        void UpdateCargoDisplay(CargoSnapshot snapshot, int? capacity);
        void UpdateBalance(long balance);
        void UpdateCommanderName(string commanderName);
        void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName);
        void UpdateShipLoadout(ShipLoadout loadout);
        void UpdateShipStatus(StatusFile status);
        void UpdateLocation(string location);
        void UpdateSessionOverlay(long cargoCollected, long creditsEarned);
        void UpdateSystemInfo(SystemInfoData data);
        void UpdateStationInfo(StationInfoData data);
        void UpdateMiningStats();
        void DisplayWelcomeMessage();
        void UpdateTitle(string title);
        void AdjustMessageColumnLayout();
        void InitializeShipTab();
        void ShowForm();
        void ShowOverlays();
        void HideOverlays();
    }
}