using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Tab page that surfaces live session statistics and history.
    /// </summary>
    public class SessionTab : TabPage
    {
        private readonly SessionTrackingService _sessionTracker;
        private readonly Label _durationValue;
        private readonly Label _cargoCollectedValue;
        private readonly Label _creditsValue;
        private readonly Label _profitPerHourValue;
        private readonly Label _currentCargoValue;
        private readonly Label _cargoFillValue;
        private readonly Label _miningDurationValue;
        private readonly Label _miningProfitValue;
        private readonly Label _limpetsValue;
        private readonly Label _cargoFullValue;
        private readonly Label _topCollectedValue;
        private readonly Label _topRefinedValue;
        private readonly ListView _historyList;
        private readonly System.Windows.Forms.Timer _liveUpdateTimer;

        public SessionTab(SessionTrackingService sessionTracker, FontManager fontManager)
        {
            _sessionTracker = sessionTracker ?? throw new ArgumentNullException(nameof(sessionTracker));

            Text = "Session";
            Padding = new Padding(12);

            var summaryTable = CreateSummaryTable(fontManager);
            _durationValue = AddSummaryRow(summaryTable, "Session Duration");
            _creditsValue = AddSummaryRow(summaryTable, "Credits Earned");
            _profitPerHourValue = AddSummaryRow(summaryTable, "Profit / Hour");
            _cargoCollectedValue = AddSummaryRow(summaryTable, "Cargo Collected");
            _currentCargoValue = AddSummaryRow(summaryTable, "Cargo Onboard");
            _cargoFillValue = AddSummaryRow(summaryTable, "Cargo Fill");
            _miningDurationValue = AddSummaryRow(summaryTable, "Mining Duration");
            _miningProfitValue = AddSummaryRow(summaryTable, "Mining Profit");
            _limpetsValue = AddSummaryRow(summaryTable, "Limpets Used");
            _cargoFullValue = AddSummaryRow(summaryTable, "Cargo Hold Full");
            _topCollectedValue = AddSummaryRow(summaryTable, "Top Collected");
            _topRefinedValue = AddSummaryRow(summaryTable, "Top Refined");

            var summaryGroup = CreateSummaryGroup(fontManager, summaryTable);

            _historyList = CreateHistoryList();
            var historyGroup = CreateHistoryGroup(_historyList, fontManager);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            layout.Controls.Add(summaryGroup, 0, 0);
            layout.SetColumnSpan(summaryGroup, 2);
            layout.Controls.Add(historyGroup, 0, 1);
            layout.SetColumnSpan(historyGroup, 2);

            Controls.Add(layout);

            _sessionTracker.SessionUpdated += OnSessionUpdated;
            _sessionTracker.SessionHistoryUpdated += OnSessionHistoryUpdated;

            _liveUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _liveUpdateTimer.Tick += (s, e) => UpdateSummaryLabels();
            _liveUpdateTimer.Start();

            UpdateSummaryLabels();
            RefreshHistory();
        }

        private static TableLayoutPanel CreateSummaryTable(FontManager fontManager)
        {
            return new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 0,
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.AutoSize),
                    new ColumnStyle(SizeType.Percent, 100f)
                },
                Font = fontManager.SegoeUIFont
            };
        }

        private GroupBox CreateSummaryGroup(FontManager fontManager, TableLayoutPanel summaryTable)
        {
            var groupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 1,
                AutoSize = true
            };
            groupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            groupLayout.Controls.Add(summaryTable, 0, 0);

            return new GroupBox
            {
                Text = "Current Session",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = fontManager.SegoeUIFontBold,
                Controls = { groupLayout }
            };
        }

        private static ListView CreateHistoryList()
        {
            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false
            };

            list.Columns.Add("Start (local)", 150);
            list.Columns.Add("Duration", 90);
            list.Columns.Add("Cargo", 70);
            list.Columns.Add("Credits", 90, HorizontalAlignment.Right);
            list.Columns.Add("Mining", 90);
            list.Columns.Add("Limpets", 70, HorizontalAlignment.Right);

            typeof(ListView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(list, true);

            return list;
        }

        private static GroupBox CreateHistoryGroup(ListView historyList, FontManager fontManager)
        {
            return new GroupBox
            {
                Text = "Session History",
                Dock = DockStyle.Fill,
                Font = fontManager.SegoeUIFontBold,
                Controls = { historyList }
            };
        }

        private Label AddSummaryRow(TableLayoutPanel table, string header)
        {
            var headerLabel = new Label
            {
                Text = header,
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 130, 146),
                Margin = new Padding(0, 0, 8, 6),
                Anchor = AnchorStyles.Left
            };

            var valueLabel = new Label
            {
                Text = "--",
                AutoSize = true,
                ForeColor = Color.FromArgb(249, 153, 53),
                Margin = new Padding(0, 0, 0, 6),
                Anchor = AnchorStyles.Left
            };

            var row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(headerLabel, 0, row);
            table.Controls.Add(valueLabel, 1, row);

            return valueLabel;
        }

        private void OnSessionUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateSummaryLabels));
            }
            else
            {
                UpdateSummaryLabels();
            }
        }

        private void OnSessionHistoryUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshHistory));
            }
            else
            {
                RefreshHistory();
            }
        }

        private void UpdateSummaryLabels()
        {
            _durationValue.Text = FormatTimeSpan(_sessionTracker.SessionDuration);
            _cargoCollectedValue.Text = $"{_sessionTracker.TotalCargoCollected:N0} units";
            _creditsValue.Text = $"{_sessionTracker.CreditsEarned:N0} CR";

            var hours = _sessionTracker.SessionDuration.TotalHours;
            var profitPerHour = hours > 0 ? _sessionTracker.CreditsEarned / hours : 0;
            _profitPerHourValue.Text = $"{profitPerHour:N0} CR/hr";

            _currentCargoValue.Text = $"{_sessionTracker.CurrentCargoCount:N0} / {_sessionTracker.CargoCapacity:N0}";
            _cargoFillValue.Text = $"{_sessionTracker.CargoFillPercent:F1}%";
            _cargoFullValue.Text = _sessionTracker.IsCargoHoldFull ? "Yes" : "No";

            _miningDurationValue.Text = FormatTimeSpan(_sessionTracker.MiningDuration);
            _miningProfitValue.Text = $"{_sessionTracker.MiningProfit:N0} CR";
            _limpetsValue.Text = $"{_sessionTracker.LimpetsUsed:N0}";

            _topCollectedValue.Text = FormatCommodityList(_sessionTracker.CollectedCommodities);
            _topRefinedValue.Text = FormatCommodityList(_sessionTracker.RefinedCommodities);
        }

        private void RefreshHistory()
        {
            var history = _sessionTracker.SessionHistory
                .OrderByDescending(record => record.SessionStart)
                .Take(25)
                .ToList();

            _historyList.BeginUpdate();
            _historyList.Items.Clear();

            foreach (var record in history)
            {
                var item = new ListViewItem(record.SessionStart.ToLocalTime().ToString("g"))
                {
                    ToolTipText = $"Ended at {record.SessionEnd.ToLocalTime():g}"
                };
                item.SubItems.Add(FormatTimeSpan(record.SessionDuration));
                item.SubItems.Add($"{record.TotalCargoCollected:N0}");
                item.SubItems.Add($"{record.CreditsEarned:N0}");
                item.SubItems.Add(FormatTimeSpan(record.MiningDuration));
                item.SubItems.Add(record.LimpetsUsed.ToString(CultureInfo.InvariantCulture));

                _historyList.Items.Add(item);
            }

            _historyList.EndUpdate();
        }

        private static string FormatTimeSpan(TimeSpan span) =>
            span.TotalHours >= 1
                ? span.ToString(@"hh\:mm\:ss")
                : span.ToString(@"mm\:ss");

        private static string FormatCommodityList(IReadOnlyDictionary<string, int> commodities)
        {
            if (commodities == null || commodities.Count == 0)
            {
                return "None";
            }

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var entries = commodities
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => $"{textInfo.ToTitleCase(kvp.Key.Replace("_", " "))}: {kvp.Value:N0}");

            return string.Join(", ", entries);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _liveUpdateTimer?.Stop();
                _liveUpdateTimer?.Dispose();
                _sessionTracker.SessionUpdated -= OnSessionUpdated;
                _sessionTracker.SessionHistoryUpdated -= OnSessionHistoryUpdated;
            }

            base.Dispose(disposing);
        }
    }
}
