using EliteDataRelay.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public partial class OverlayForm
    {
        private Label _miningLimpetsUsedValueLabel = null!;
        private Label _miningRefinedValueLabel = null!;
        private Label _miningDurationValueLabel = null!;
        // Keep a reference to the header label so we can hide it.
        private Label _refinedHeaderLabel = null!;

        private void InitializeMiningControls()
        {
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new Size(320, 0);

            var detailsTable = new TableLayoutPanel
            {
                Location = new Point(10, 10),
                AutoSize = true,
                Width = this.ClientSize.Width - 20,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _miningLimpetsUsedValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
            _miningDurationValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
            _miningRefinedValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
            _refinedHeaderLabel = CreateHeaderLabel("Refined:");

            detailsTable.Controls.Add(CreateHeaderLabel("Limpets Used:"), 0, 0);
            detailsTable.Controls.Add(_miningLimpetsUsedValueLabel, 1, 0);
            detailsTable.Controls.Add(CreateHeaderLabel("Duration:"), 0, 1);
            detailsTable.Controls.Add(_miningDurationValueLabel, 1, 1);
            detailsTable.Controls.Add(_refinedHeaderLabel, 0, 2);
            detailsTable.Controls.Add(_miningRefinedValueLabel, 1, 2);

            Controls.Add(detailsTable);
        }

        public void UpdateMiningSession(SessionTrackingService tracker)
        {
            // Store the tracker instance so the timer can use it.
            _sessionTracker = tracker;

            // Start or stop the real-time update timer based on the session state.
            if (tracker.IsMiningSessionActive && _miningUpdateTimer?.Enabled == false)
            {
                _miningUpdateTimer.Start();
            }
            else if (!tracker.IsMiningSessionActive && _miningUpdateTimer?.Enabled == true)
            {
                _miningUpdateTimer.Stop();
            }

            if (tracker.IsMiningSessionActive || tracker.MiningDuration > TimeSpan.Zero)
            {
                UpdateLabel(_miningLimpetsUsedValueLabel, $"{tracker.LimpetsUsed}");
                UpdateLabel(_miningDurationValueLabel, $"{tracker.MiningDuration:hh\\:mm\\:ss}");

                var refinedList = tracker.RefinedCommodities.ToList();
                if (refinedList.Any())
                {
                    _refinedHeaderLabel.Visible = true;
                    _miningRefinedValueLabel.Visible = true;
                    // Display all refined commodities, each on a new line.
                    var allRefined = refinedList.OrderByDescending(kvp => kvp.Value).Select(kvp => $"{kvp.Key.Substring(0, 1).ToUpper()}{kvp.Key.Substring(1)}: {kvp.Value}");
                    UpdateLabel(_miningRefinedValueLabel, string.Join(Environment.NewLine, allRefined));
                }
                else
                {
                    _refinedHeaderLabel.Visible = false;
                    _miningRefinedValueLabel.Visible = false;
                }
            }
        }
    }
}