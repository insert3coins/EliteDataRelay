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
            var miningPage = CreateMiningTabPage(fontManager, sessionTracker);
            var sessionPage = CreateSessionTabPage(fontManager, sessionTracker);
            
            TabControl.TabPages.AddRange(new[] { cargoPage, shipPage, miningPage, sessionPage });
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
        }

        private TabPage CreateSessionTabPage(FontManager fontManager, SessionTrackingService sessionTracker)
        {
            SessionTab?.Dispose();
            SessionTab = new SessionTab(sessionTracker, fontManager);
            return SessionTab;
        }
    }
}
