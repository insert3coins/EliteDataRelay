using EliteDataRelay.Models;
using EliteDataRelay.Models.Market;
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
        event EventHandler? TradeFindBestSellClicked;
        event EventHandler? TradeFindBestBuyClicked;

        void InitializeUI(Form form);
        void SetButtonStates(bool startEnabled, bool stopEnabled);
        void UpdateMonitoringVisuals(bool isMonitoring);
        void UpdateCargoHeader(int count, int? capacity);
        void UpdateCargoList(CargoSnapshot cargoSnapshot);
        void UpdateCargoDisplay(CargoSnapshot cargoSnapshot, int? capacity);
        void UpdateBalance(long balance);
        void UpdateCommanderName(string commanderName);
        void UpdateShipInfo(string shipName, string shipIdent, string shipType, string internalShipName);
        void UpdateShipLoadout(ShipLoadout loadout);
        void UpdateShipStatus(StatusFile status);
        void UpdateStationInfo(StationInfoData data);
        void UpdateSystemInfo(SystemInfoData data);
        void UpdateLocation(string location);
        void UpdateSessionOverlay(int cargoCollected, long creditsEarned);
        void UpdateMiningStats();
        void UpdateTitle(string title);
        string? GetSelectedTradeCommodity();
        void PopulateCommodities(IEnumerable<string> commodities);
        void UpdateTradeResults(List<MarketInfo> results, bool isSellSearch);
        void SetTradeStatus(string text);
        void OnTradeCommodityChanged(object? sender, EventArgs e);
        void StartTradeSearchAnimation();
        void StopTradeSearchAnimation();
        void ShowOverlays();
        void HideOverlays();
        void RefreshOverlay();
    }
}