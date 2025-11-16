using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private void CreateTabControls(FontManager fontManager, SessionTrackingService sessionTracker, MiningTrackerService miningTracker)
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
            
            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, sessionPage, miningPage });
        }

        private void DisposeTabControls()
        {
            TabControl?.Dispose();
            TabControl = null!;
            DisposeCargoTabControls();
            DisposeShipTabControls();
            SessionTab = null;
            MiningTab = null;
        }

        private TabPage CreateSessionTabPage(FontManager fontManager, SessionTrackingService sessionTracker)
        {
            SessionTab?.Dispose();
            SessionTab = new SessionTab(sessionTracker, fontManager);
            return SessionTab;
        }

        private TabPage CreateMiningTabPage(FontManager fontManager, MiningTrackerService tracker)
        {
            MiningTab?.Dispose();
            MiningTab = new MiningTab(tracker, fontManager);
            return MiningTab;
        }
    }
}
