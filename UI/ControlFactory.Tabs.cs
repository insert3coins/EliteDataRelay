using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private void CreateTabControls(FontManager fontManager, SessionTrackingService sessionTracker, FleetCarrierTrackerService fleetCarrierTracker, MiningTrackerService miningTracker)
        {
            // Tab control to switch between Cargo and Materials
            TabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = fontManager.VerdanaFont,
            };

            var cargoPage = CreateCargoTabPage(fontManager);
            var shipPage = CreateShipTabPage(fontManager);
            var sessionPage = CreateSessionTabPage(fontManager, sessionTracker);
            var miningPage = CreateMiningTabPage(fontManager, miningTracker);
            var fleetCarrierPage = CreateFleetCarrierTabPage(fontManager, fleetCarrierTracker);
            
            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, sessionPage, miningPage, fleetCarrierPage });
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
            if (MiningTab != null)
            {
                MiningTab.Dispose();
                MiningTab = null;
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

        private TabPage CreateMiningTabPage(FontManager fontManager, MiningTrackerService tracker)
        {
            MiningTab?.Dispose();
            MiningTab = new MiningTab(tracker, fontManager);
            return MiningTab;
        }
    }
}
