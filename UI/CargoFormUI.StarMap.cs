using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class CargoFormUI
    {
        public event EventHandler? ScanJournalsClicked;
        public event EventHandler<string>? SearchSystemClicked;

        private void InitializeStarMap()
        {
            if (_controlFactory != null)
            {
                _controlFactory.ScanJournalsButton.Click += (s, e) => ScanJournalsClicked?.Invoke(this, EventArgs.Empty);
                _controlFactory.ResetStarMapViewButton.Click += (s, e) => _controlFactory.StarMapPanel.ResetView();
                _controlFactory.StarMapSearchButton.Click += OnSearchSystem;
                _controlFactory.StarMapSearchBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        OnSearchSystem(s, EventArgs.Empty);
                        e.SuppressKeyPress = true; // Prevent the 'ding' sound on enter
                    }
                };
            }
        }

        private void OnSearchSystem(object? sender, EventArgs e)
        {
            var systemName = _controlFactory?.StarMapSearchBox.Text;
            if (string.IsNullOrWhiteSpace(systemName))
            {
                // If the search box is empty, clear any highlight.
                SearchSystemClicked?.Invoke(this, string.Empty);
            }
            else
            {
                SearchSystemClicked?.Invoke(this, systemName);
            }
        }

        public void UpdateStarMap(IReadOnlyList<StarSystem> systems, string currentSystem) =>
            _controlFactory?.StarMapPanel.SetSystems(systems, currentSystem);

        public void CenterStarMapOnSystem(string systemName) =>
            _controlFactory?.StarMapPanel.CenterOnSystem(systemName);

        public void SetAndCenterStarMapOnSystem(string systemName) =>
            _controlFactory?.StarMapPanel.SetAndCenterOnSystem(systemName);

        public void HighlightSearchedSystem(string? systemName) =>
            _controlFactory?.StarMapPanel.HighlightSystem(systemName);

        public void UpdateStarMapAutocomplete(IReadOnlyList<StarSystem> systems)
        {
            if (_controlFactory == null) return;

            var collection = new AutoCompleteStringCollection();
            collection.AddRange(systems.Select(s => s.Name).ToArray());

            _controlFactory.StarMapSearchBox.AutoCompleteCustomSource = collection;
        }

        public void ShowScanProgress(bool visible)
        {
            if (_controlFactory == null) return;
            var panel = _controlFactory.StarMapScanProgress.Parent as Control;
            if (panel != null) panel.Visible = visible;

            // Disable buttons during scan
            _controlFactory.ScanJournalsButton.Enabled = !visible;
            _controlFactory.StarMapSearchButton.Enabled = !visible;
        }

        public void UpdateScanProgress(int percentage, string message)
        {
            if (_controlFactory == null) return;
            _controlFactory.StarMapScanProgress.Value = Math.Clamp(percentage, 0, 100);
            _controlFactory.StarMapScanLabel.Text = message;
        }
    }
}