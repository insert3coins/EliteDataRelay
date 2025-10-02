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
        private Label _lblMiningDurationValue = null!;
        private Label _lblProfitPerHourValue = null!;
        private Button _btnStartMining = null!;
        private Button _btnStopMining = null!;
        private TableLayoutPanel _tlpStats = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;

        public MiningStatsControl(SessionTrackingService sessionTracker)
        {
            _sessionTracker = sessionTracker;
            InitializeComponent();
            UpdateLabels();

            // Timer to update duration labels in real-time
            _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) => {
                if (_sessionTracker.IsMiningSessionActive) 
                    UpdateLabels();
            };
        }

        private void InitializeComponent()
        {
            // Control Properties
            this.Dock = DockStyle.Fill;
            this.AutoSize = true;
            this.BackColor = Color.Transparent;

            // Fonts
            var headerFont = new Font("Verdana", 10F, FontStyle.Regular);
            var valueFont = new Font("Consolas", 11F, FontStyle.Bold);

            // Stats Layout Panel
            _tlpStats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 5
            };
            _tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++)
            {
                _tlpStats.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Stat Labels
            var lblMiningProfitHeader = new Label { Text = "Mining Profit:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblMiningProfitValue = new Label { Text = "0 CR", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblLimpetsUsedHeader = new Label { Text = "Limpets Used:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblLimpetsUsedValue = new Label { Text = "0", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblRefinedHeader = new Label { Text = "Refined:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblRefinedValue = new Label { Text = "None", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblMiningDurationHeader = new Label { Text = "Duration:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblMiningDurationValue = new Label { Text = "00:00:00", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            var lblProfitPerHourHeader = new Label { Text = "Profit/Hour:", Font = headerFont, ForeColor = Color.Silver, Anchor = AnchorStyles.Left, AutoSize = true };
            _lblProfitPerHourValue = new Label { Text = "0 CR/hr", Font = valueFont, ForeColor = Color.Orange, Anchor = AnchorStyles.Right, AutoSize = true };

            // Action Buttons
            _btnStartMining = new Button { Text = "Start Mining Session", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
            _btnStartMining.Click += (s, e) => {
                _sessionTracker.StartMiningSession();
                _updateTimer.Start();
            };

            _btnStopMining = new Button { Text = "Stop Mining Session", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
            _btnStopMining.Click += (s, e) => {
                _sessionTracker.StopMiningSession();
                _updateTimer.Stop();
            };

            // Add labels to TableLayoutPanel
            _tlpStats.Controls.Add(lblMiningProfitHeader, 0, 0);
            _tlpStats.Controls.Add(_lblMiningProfitValue, 1, 0);
            _tlpStats.Controls.Add(lblLimpetsUsedHeader, 0, 1);
            _tlpStats.Controls.Add(_lblLimpetsUsedValue, 1, 1);
            _tlpStats.Controls.Add(lblRefinedHeader, 0, 2);
            _tlpStats.Controls.Add(_lblRefinedValue, 1, 2);
            _tlpStats.Controls.Add(lblMiningDurationHeader, 0, 3);
            _tlpStats.Controls.Add(_lblMiningDurationValue, 1, 3);
            _tlpStats.Controls.Add(lblProfitPerHourHeader, 0, 4);
            _tlpStats.Controls.Add(_lblProfitPerHourValue, 1, 4);

            // Main layout panel to hold stats and buttons
            var mainPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            mainPanel.Controls.Add(_btnStartMining);
            mainPanel.Controls.Add(_tlpStats);
            mainPanel.Controls.Add(_btnStopMining);

            this.Controls.Add(mainPanel);
        }

        public void UpdateLabels()
        {
            if (this.IsDisposed) return;
 
            // Toggle visibility based on mining session state
            bool isSessionActive = _sessionTracker.IsMiningSessionActive;
 
            _btnStartMining.Visible = !isSessionActive;
            _btnStopMining.Visible = isSessionActive;
            _tlpStats.Visible = true; // Always show the stats panel

            _lblMiningProfitValue.Text = $"{_sessionTracker.MiningProfit:N0} CR";
            _lblLimpetsUsedValue.Text = $"{_sessionTracker.LimpetsUsed}";
            _lblMiningDurationValue.Text = $"{_sessionTracker.MiningDuration:hh\\:mm\\:ss}";

            double totalHours = _sessionTracker.MiningDuration.TotalHours;
            if (totalHours > 0)
            {
                long profitPerHour = (long)(_sessionTracker.MiningProfit / totalHours);
                _lblProfitPerHourValue.Text = $"{profitPerHour:N0} CR/hr";
            } else {
                _lblProfitPerHourValue.Text = "0 CR/hr";
            }

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