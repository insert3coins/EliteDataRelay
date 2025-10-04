using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class ControlFactory
    {
        public ComboBox TradeCommodityComboBox { get; private set; } = null!;
        public Button TradeFindBestSellButton { get; private set; } = null!;
        public Button TradeFindBestBuyButton { get; private set; } = null!;
        public ListView TradeResultsListView { get; private set; } = null!;
        public Label TradeStatusLabel { get; private set; } = null!;

        private TabPage CreateTradeTabPage(FontManager fontManager)
        {
            var tradePage = new TabPage("Trade");
            tradePage.Padding = new Padding(10);

            var mainTradePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
            };
            mainTradePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Search controls
            mainTradePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Results list
            mainTradePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status label

            // --- Search Panel ---
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 10)
            };

            TradeCommodityComboBox = new ComboBox
            {
                Font = fontManager.ConsolasFont,
                Width = 250,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            TradeFindBestSellButton = new Button
            {
                Text = "Find Best Sell",
                Font = fontManager.ConsolasFont,
                AutoSize = true,
                Enabled = false
            };

            TradeFindBestBuyButton = new Button
            {
                Text = "Find Best Buy",
                Font = fontManager.ConsolasFont,
                AutoSize = true,
                Enabled = false
            };

            searchPanel.Controls.Add(new Label { Text = "Commodity:", AutoSize = true, Font = fontManager.ConsolasFont, Padding = new Padding(0, 5, 5, 0) });
            searchPanel.Controls.Add(TradeCommodityComboBox);
            searchPanel.Controls.Add(TradeFindBestSellButton);
            searchPanel.Controls.Add(TradeFindBestBuyButton);

            // --- Results List ---
            TradeResultsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = fontManager.ConsolasFont,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.FixedSingle,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            TradeResultsListView.Columns.Add("Station", 250);
            TradeResultsListView.Columns.Add("Price", 100, HorizontalAlignment.Right);
            TradeResultsListView.Columns.Add("Demand/Supply", 120, HorizontalAlignment.Right);
            TradeResultsListView.Columns.Add("Distance (LY)", 120, HorizontalAlignment.Right);

            // --- Status Label ---
            TradeStatusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Select a commodity and search.",
                Font = fontManager.ConsolasFont,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            mainTradePanel.Controls.Add(searchPanel, 0, 0);
            mainTradePanel.Controls.Add(TradeResultsListView, 0, 1);
            mainTradePanel.Controls.Add(TradeStatusLabel, 0, 2);

            tradePage.Controls.Add(mainTradePanel);

            return tradePage;
        }

        private void DisposeTradeTabControls()
        {
            TradeCommodityComboBox?.Dispose();
            TradeFindBestSellButton?.Dispose();
            TradeFindBestBuyButton?.Dispose();
            TradeResultsListView?.Dispose();
            TradeStatusLabel?.Dispose();
        }
    }
}