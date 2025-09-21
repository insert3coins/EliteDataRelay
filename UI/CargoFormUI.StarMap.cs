using System;
using System.Collections.Generic;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public event EventHandler? ScanJournalsClicked;

        private void InitializeStarMap()
        {
            if (_controlFactory != null)
            {
                _controlFactory.ScanJournalsButton.Click += (s, e) => ScanJournalsClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateStarMap(IReadOnlyList<StarSystem> systems, string currentSystem) =>
            _controlFactory?.StarMapPanel.SetSystems(systems, currentSystem);

        public void CenterStarMapOnSystem(string systemName) =>
            _controlFactory?.StarMapPanel.CenterOnSystem(systemName);
    }
}