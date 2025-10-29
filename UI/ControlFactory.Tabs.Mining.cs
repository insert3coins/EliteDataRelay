using EliteDataRelay.Services;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private TabPage CreateMiningTabPage(FontManager fontManager, SessionTrackingService sessionTracker)
        {
            var miningPage = new TabPage("Mining");
            miningPage.BackColor = Color.FromArgb(249, 250, 251); // Match panel's light theme (Gray-50)

            MiningSessionPanel = new MiningSessionPanel(sessionTracker, fontManager)
            {
                Dock = DockStyle.Fill,
            };

            miningPage.Controls.Add(MiningSessionPanel); // Add panel last to be behind buttons

            return miningPage;
        }

        internal void DisposeMiningTabControls()
        {
            MiningSessionPanel?.Dispose();
        }
    }
}



