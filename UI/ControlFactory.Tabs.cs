using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        private void CreateTabControls(FontManager fontManager)
        {
            // Tab control to switch between Cargo and Materials
            TabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = fontManager.VerdanaFont,
            };

            var cargoPage = CreateCargoTabPage(fontManager);
            //var shipPage = CreateShipTabPage(fontManager);

            TabControl.TabPages.AddRange(new[] { cargoPage /*, shipPage*/ });
        }

        private void DisposeTabControls()
        {
            TabControl.Dispose();
            DisposeCargoTabControls();
            DisposeShipTabControls();
        }
    }
}