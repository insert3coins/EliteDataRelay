using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class ControlFactory
    {
        public ListView ListView { get; private set; } = null!;

        private TabPage CreateCargoTabPage(FontManager fontManager)
        {
            var cargoPage = new TabPage("Cargo");

            this.ListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.VerdanaFont,
                FullRowSelect = true,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };

            // Add columns for the cargo data
            this.ListView.Columns.Add("Commodity", 200);
            this.ListView.Columns.Add("Quantity", 80);
            this.ListView.Columns.Add("Category", -2); // -2 means it fills the remaining space

            // Add the event handler to prevent selection
            this.ListView.SelectedIndexChanged += ListView_SelectedIndexChanged;

            cargoPage.Controls.Add(this.ListView);

            return cargoPage;
        }

        private void DisposeCargoTabControls()
        {
            if (this.ListView != null)
            {
                this.ListView.SelectedIndexChanged -= ListView_SelectedIndexChanged;
                this.ListView.Dispose();
            }
        }

        private void ListView_SelectedIndexChanged(object? sender, System.EventArgs e)
        {
            // If any item becomes selected, immediately clear the selection.
            if (this.ListView.SelectedIndices.Count > 0)
                this.ListView.SelectedIndices.Clear();
        }
    }
}