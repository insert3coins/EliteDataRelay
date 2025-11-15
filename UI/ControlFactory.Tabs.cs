using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private void CreateTabControls(FontManager fontManager, SessionTrackingService sessionTracker, FleetCarrierTrackerService fleetCarrierTracker)
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
            var sessionPage = CreateSessionTabPage(fontManager, sessionTracker);
            var fleetCarrierPage = CreateFleetCarrierTabPage(fontManager, fleetCarrierTracker);
            
            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, miningPage, sessionPage, fleetCarrierPage });
        }

        private void DisposeTabControls()
        {
            TabControl.Dispose();
            DisposeCargoTabControls();
            DisposeShipTabControls();
            if (SessionTab != null)
            {
                SessionTab.Dispose();
                SessionTab = null;
            }
            if (FleetCarrierTab != null)
            {
                FleetCarrierTab.Dispose();
                FleetCarrierTab = null;
            }
        }

        private TabPage CreateSessionTabPage(FontManager fontManager, SessionTrackingService sessionTracker)
        {
            SessionTab?.Dispose();
            SessionTab = new SessionTab(sessionTracker, fontManager);
            return SessionTab;
        }

        private TabPage CreateFleetCarrierTabPage(FontManager fontManager, FleetCarrierTrackerService tracker)
        {
            FleetCarrierTab?.Dispose();
            FleetCarrierTab = new FleetCarrierTab(tracker, fontManager);
            return FleetCarrierTab;
        }
    }
}
