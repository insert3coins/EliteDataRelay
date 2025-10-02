using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// A user control dedicated to displaying mining session statistics.
    /// </summary>
    public class MiningStatsControl : UserControl
    {
        private readonly SessionTrackingService _sessionTracker;
        private Label _lblMiningProfitValue = null!;
        private Label _lblLimpetsUsedValue = null!;
        private Label _lblRefinedValue = null!;

        public MiningStatsControl(SessionTrackingService sessionTracker)
        {
            _sessionTracker = sessionTracker;
            InitializeComponent();
            UpdateLabels();
        }

        private void InitializeComponent()
        {
            // Control Properties
            this.Dock = DockStyle.Top;
            this.AutoSize = true;
            this.BackColor = Color.Transparent;

            // Fonts
            var headerFont = new Font("Verdana", 10F, FontStyle.Regular);
            var valueFont = new Font("Consolas", 11F, FontStyle.Bold);

            // Stats Layout Panel
            var tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 3
            };
            tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++)
            {
                tlpStats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Stat Labels
            var lblMiningProfitHeader = new Label { Text = "Mining Profit:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblMiningProfitValue = new Label { Text = "0 CR", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblLimpetsUsedHeader = new Label { Text = "Limpets Used:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblLimpetsUsedValue = new Label { Text = "0", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblRefinedHeader = new Label { Text = "Refined:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblRefinedValue = new Label { Text = "None", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            // Add labels to TableLayoutPanel
            tlpStats.Controls.Add(lblMiningProfitHeader, 0, 0);
            tlpStats.Controls.Add(_lblMiningProfitValue, 1, 0);
            tlpStats.Controls.Add(lblLimpetsUsedHeader, 0, 1);
            tlpStats.Controls.Add(_lblLimpetsUsedValue, 1, 1);
            tlpStats.Controls.Add(lblRefinedHeader, 0, 2);
            tlpStats.Controls.Add(_lblRefinedValue, 1, 2);

            this.Controls.Add(tlpStats);
        }

        public void UpdateLabels()
        {
            if (this.IsDisposed) return;

            _lblMiningProfitValue.Text = $"{_sessionTracker.MiningProfit:N0} CR";
            _lblLimpetsUsedValue.Text = $"{_sessionTracker.LimpetsUsed}";

            var refinedList = _sessionTracker.RefinedCommodities.ToList();
            if (refinedList.Any())
            {
                // Show the top 3 refined commodities to keep the UI clean
                var topRefined = refinedList
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(3)
                    .Select(kvp => $"{kvp.Key.Substring(0, 1).ToUpper()}{kvp.Key.Substring(1)}: {kvp.Value}");

                _lblRefinedValue.Text = string.Join(", ", topRefined);
            }
            else
            {
                _lblRefinedValue.Text = "None";
            }
        }
    }
}