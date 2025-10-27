using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private HotspotFinderPanel? _hotspotFinderPanel;
        private void CreateTabControls(FontManager fontManager, SessionTrackingService sessionTracker)
        {
            // Tab control to switch between Cargo and Materials
            TabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = fontManager.VerdanaFont,
            };

            var cargoPage = CreateCargoTabPage(fontManager);
            var shipPage = CreateShipTabPage(fontManager);
            var miningPage = CreateMiningTabPage(fontManager, sessionTracker);
            var hotspotsPage = CreateHotspotsTabPage();

            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, miningPage, hotspotsPage });
        }

        private void DisposeTabControls()
        {
            TabControl.Dispose();
            DisposeCargoTabControls();
            DisposeShipTabControls();
            _hotspotFinderPanel?.Dispose();
        }

        private TabPage CreateHotspotsTabPage()
        {
            var page = new TabPage("Hotspots");
            page.BackColor = Color.FromArgb(249, 250, 251);
            _hotspotFinderPanel = new HotspotFinderPanel(new Services.HotspotFinderService());
            _hotspotFinderPanel.Dock = DockStyle.Fill;
            page.Controls.Add(_hotspotFinderPanel);
            return page;
        }
    }
}
