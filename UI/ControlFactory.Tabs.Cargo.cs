using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class ControlFactory
    {
        private TabPage CreateCargoTabPage(FontManager fontManager)
        {
            var cargoPage = new TabPage("Cargo");

            // Main ListView to display cargo items
            ListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.VerdanaFont,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, // Use standard window background
                GridLines = false // Cleaner look without grid lines
            };

            // Define columns for the ListView
            ListView.Columns.Add("Commodity", 200, HorizontalAlignment.Left);
            ListView.Columns.Add("Count", 80, HorizontalAlignment.Center);
            ListView.Columns.Add("Category", -2, HorizontalAlignment.Center);

            cargoPage.Controls.Add(ListView);
            return cargoPage;
        }
        private void DisposeCargoTabControls()
        {
            ListView.Dispose();
        }
    }
}