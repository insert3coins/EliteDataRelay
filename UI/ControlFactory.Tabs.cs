using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
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
            var miningPage = new TabPage("Mining");

            MiningStatsControl = new MiningStatsControl(sessionTracker)
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            miningPage.Controls.Add(MiningStatsControl);

            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, miningPage });
        }

        private void DisposeTabControls()
        {
            TabControl.Dispose();
            DisposeCargoTabControls();
            DisposeShipTabControls();
        }
    }
}