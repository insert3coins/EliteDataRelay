using System;
using System.Windows.Forms;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.UI
{
    public interface ICargoFormUI : IDisposable
    {
        event EventHandler? StartClicked;

        event EventHandler? StopClicked;

        event EventHandler? ExitClicked;

        event EventHandler? AboutClicked;

        event EventHandler? SettingsClicked;

        void InitializeUI(Form form);

        void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity);

        void UpdateCargoHeader(int currentCount, int? capacity);

        void UpdateCargoList(CargoSnapshot snapshot);

        void UpdateTitle(string title);

        void UpdateLocation(string starSystem);

        void SetButtonStates(bool startEnabled, bool stopEnabled);
    }
}