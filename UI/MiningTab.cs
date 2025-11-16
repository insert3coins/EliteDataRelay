using EliteDataRelay.Models.Mining;
using EliteDataRelay.Services;
using EliteDataRelay.UI.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Tab page that surfaces mining session details using the MiningTrackerService.
    /// </summary>
    public sealed class MiningTab : TabPage
    {
        private static readonly ImageList EmptyStateImageList = CreateEmptyStateImageList();
        private readonly MiningTrackerService _tracker;
        private readonly Label _locationValue;
        private readonly Label _durationValue;
        private readonly Label _prospectedValue;
        private readonly Label _crackedValue;
        private readonly Label _prospectorsValue;
        private readonly Label _collectorsValue;
        private readonly Label _refinedValue;
        private readonly Label _materialsValue;
        private readonly Label _contentValue;
        private readonly ListView _oreList;
        private readonly ListView _historyList;
        private readonly ListView _prospectorList;
        private readonly Label _prospectorHeader;
        private readonly System.Windows.Forms.Timer _durationTimer;
        private bool _isLive;

        public MiningTab(MiningTrackerService tracker, FontManager fontManager)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));

            Text = "Mining";
            Padding = new Padding(10);
            Font = fontManager.SegoeUIFont;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var summaryGroup = new GroupBox { Text = "Current Session", Dock = DockStyle.Fill };
            var summaryTable = CreateSummaryTable(fontManager);
            summaryGroup.Controls.Add(summaryTable);

            _locationValue = AddSummaryRow(summaryTable, "Location");
            _durationValue = AddSummaryRow(summaryTable, "Duration");
            _prospectedValue = AddSummaryRow(summaryTable, "Asteroids Prospected");
            _crackedValue = AddSummaryRow(summaryTable, "Asteroids Cracked");
            _prospectorsValue = AddSummaryRow(summaryTable, "Prospectors Fired");
            _collectorsValue = AddSummaryRow(summaryTable, "Collectors Deployed");
            _refinedValue = AddSummaryRow(summaryTable, "Refined (t)");
            _materialsValue = AddSummaryRow(summaryTable, "Materials Collected");
            _contentValue = AddSummaryRow(summaryTable, "Material Contents (L | M | H)");

            var prospectorGroup = new GroupBox { Text = "Latest Prospector", Dock = DockStyle.Fill };
            var prospectorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            prospectorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            prospectorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _prospectorHeader = new Label { Text = "Waiting for prospectors…", AutoSize = true, Dock = DockStyle.Fill };
            _prospectorList = CreateListView();
            _prospectorList.Columns.Add("Material", 220);
            _prospectorList.Columns.Add("Percent", 80, HorizontalAlignment.Right);
            prospectorLayout.Controls.Add(_prospectorHeader, 0, 0);
            prospectorLayout.Controls.Add(_prospectorList, 0, 1);
            prospectorGroup.Controls.Add(prospectorLayout);

            _oreList = CreateListView();
            _oreList.Columns.Add("Commodity", 200);
            _oreList.Columns.Add("Type", 70);
            _oreList.Columns.Add("Refined", 80, HorizontalAlignment.Right);
            _oreList.Columns.Add("Collected", 80, HorizontalAlignment.Right);
            _oreList.Columns.Add("Prospected", 90, HorizontalAlignment.Right);
            _oreList.Columns.Add("Hit Rate", 90, HorizontalAlignment.Right);
            _oreList.Columns.Add("Min %", 80, HorizontalAlignment.Right);
            _oreList.Columns.Add("Max %", 80, HorizontalAlignment.Right);
            _oreList.Columns.Add("Motherlodes", 90, HorizontalAlignment.Right);
            _oreList.Columns.Add("Content (L/M/H)", 140, HorizontalAlignment.Right);
            var oreGroup = new GroupBox { Text = "Session Yield", Dock = DockStyle.Fill };
            oreGroup.Controls.Add(_oreList);

            _historyList = CreateListView();
            _historyList.Columns.Add("Start", 150);
            _historyList.Columns.Add("Duration", 100);
            _historyList.Columns.Add("Location", 180);
            _historyList.Columns.Add("Refined (t)", 100, HorizontalAlignment.Right);
            _historyList.Columns.Add("Prospectors", 100, HorizontalAlignment.Right);
            _historyList.Columns.Add("Collectors", 90, HorizontalAlignment.Right);
            _historyList.Columns.Add("Asteroids (P/C)", 140, HorizontalAlignment.Right);
            var historyGroup = new GroupBox { Text = "Previous Sessions", Dock = DockStyle.Fill };
            historyGroup.Controls.Add(_historyList);

            topLayout.Controls.Add(summaryGroup, 0, 0);
            topLayout.Controls.Add(prospectorGroup, 1, 0);

            mainLayout.Controls.Add(topLayout, 0, 0);

            var bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            bottomLayout.Controls.Add(oreGroup, 0, 0);
            bottomLayout.Controls.Add(historyGroup, 1, 0);

            mainLayout.Controls.Add(bottomLayout, 0, 1);

            Controls.Add(mainLayout);

            _tracker.CurrentSessionUpdated += OnCurrentSessionUpdated;
            _tracker.SessionsUpdated += OnSessionsUpdated;
            _tracker.LatestProspectorUpdated += OnProspectorUpdated;
            _tracker.LiveStateChanged += OnLiveStateChanged;

            _durationTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _durationTimer.Tick += (s, _) => UpdateDuration();
            _durationTimer.Start();

            _isLive = _tracker.IsLive;
            UpdateCurrentSession();
            UpdateHistory();
            UpdateProspector();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tracker.CurrentSessionUpdated -= OnCurrentSessionUpdated;
                _tracker.SessionsUpdated -= OnSessionsUpdated;
                _tracker.LatestProspectorUpdated -= OnProspectorUpdated;
                _tracker.LiveStateChanged -= OnLiveStateChanged;
                _durationTimer?.Dispose();
            }
            base.Dispose(disposing);
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

        private static Label AddSummaryRow(TableLayoutPanel table, string header)
        {
            var headerLabel = new Label
            {
                Text = header,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3)
            };
            var valueLabel = new Label
            {
                Text = "-",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font(table.Font, FontStyle.Bold),
                Margin = new Padding(3)
            };

            var row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(headerLabel, 0, row);
            table.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private static ListView CreateListView()
        {
            var list = new BufferedListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                UseCompatibleStateImageBehavior = true,
                CheckBoxes = false
            };
            list.StateImageList = EmptyStateImageList;
            return list;
        }

        private static ImageList CreateEmptyStateImageList()
        {
            var list = new ImageList();
            list.Images.Add(new Bitmap(1, 1));
            return list;
        }

        private void OnCurrentSessionUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateCurrentSession));
            }
            else
            {
                UpdateCurrentSession();
            }
        }

        private void OnSessionsUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateHistory));
            }
            else
            {
                UpdateHistory();
            }
        }

        private void OnProspectorUpdated(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateProspector));
            }
            else
            {
                UpdateProspector();
            }
        }

        private void OnLiveStateChanged(object? sender, bool isLive)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                _isLive = isLive;
                UpdateCurrentSession();
                UpdateHistory();
                UpdateProspector();
            }));
        }
        else
        {
            _isLive = isLive;
            UpdateCurrentSession();
            UpdateHistory();
            UpdateProspector();
        }
        }

        private void UpdateCurrentSession()
        {
            if (!_isLive || _tracker.CurrentSession == null)
            {
                _locationValue.Text = "No active session";
                _durationValue.Text = "-";
                _prospectedValue.Text = "0";
                _crackedValue.Text = "0";
                _prospectorsValue.Text = "0";
                _collectorsValue.Text = "0";
                _refinedValue.Text = "0";
                _materialsValue.Text = "0";
                _contentValue.Text = "0 | 0 | 0";
                _oreList.Items.Clear();
                return;
            }

            var session = _tracker.CurrentSession;
            _locationValue.Text = string.IsNullOrWhiteSpace(session.Location) ? session.StarSystem : $"{session.Location} · {session.StarSystem}";
            _prospectedValue.Text = session.AsteroidsProspected.ToString("N0");
            _crackedValue.Text = session.AsteroidsCracked.ToString("N0");
            _prospectorsValue.Text = session.ProspectorsFired.ToString("N0");
            _collectorsValue.Text = session.CollectorsDeployed.ToString("N0");
            _refinedValue.Text = session.Items.Where(x => x.Type == MiningItemType.Ore).Sum(x => x.RefinedCount).ToString("N0");
            _materialsValue.Text = session.Items.Where(x => x.Type == MiningItemType.Material).Sum(x => x.CollectedCount).ToString("N0");
            _contentValue.Text = $"{session.LowContent:N0} | {session.MedContent:N0} | {session.HighContent:N0}";

            UpdateDuration();
            RefreshOreList(session);
        }

        private void UpdateDuration()
        {
            if (!_isLive || _tracker.CurrentSession == null)
            {
                _durationValue.Text = "-";
                return;
            }

            var session = _tracker.CurrentSession;
            if (session.TimeStarted == DateTime.MaxValue)
            {
                _durationValue.Text = "00:00:00 · 0.0 t/hr";
                return;
            }

            var end = session.TimeFinished < DateTime.MaxValue ? session.TimeFinished : DateTime.UtcNow;
            var span = end - session.TimeStarted;
            var refined = session.Items.Where(x => x.Type == MiningItemType.Ore).Sum(x => x.RefinedCount);
            var rate = span.TotalHours > 0 ? refined / span.TotalHours : 0;
            _durationValue.Text = $"{span:hh\\:mm\\:ss} · {rate:N1} t/hr";
        }

        private void RefreshOreList(MiningSession session)
        {
            _oreList.BeginUpdate();
            try
            {
                _oreList.Items.Clear();
                var ordered = session.Items
                    .OrderByDescending(x => x.Type == MiningItemType.Ore)
                    .ThenByDescending(x => x.RefinedCount)
                    .ThenByDescending(x => x.CollectedCount);

                foreach (var item in ordered)
                {
                    var hitRate = session.AsteroidsProspected > 0 && item.Type == MiningItemType.Ore
                        ? (double)item.ContentHitCount / session.AsteroidsProspected * 100d
                        : 0d;
                    var listItem = new ListViewItem(item.Name);
                    listItem.SubItems.Add(item.Type.ToString());
                    listItem.SubItems.Add(item.RefinedCount == 0 ? string.Empty : item.RefinedCount.ToString());
                    listItem.SubItems.Add(item.CollectedCount == 0 ? string.Empty : item.CollectedCount.ToString());
                    listItem.SubItems.Add(item.Prospected == 0 ? string.Empty : item.Prospected.ToString());
                    listItem.SubItems.Add(item.Type == MiningItemType.Ore && hitRate > 0 ? $"{hitRate:N2}%" : string.Empty);
                    listItem.SubItems.Add(item.MinPercentage == 0 ? string.Empty : $"{item.MinPercentage:N2}%");
                    listItem.SubItems.Add(item.MaxPercentage == 0 ? string.Empty : $"{item.MaxPercentage:N2}%");
                    listItem.SubItems.Add(item.MotherLoad == 0 ? string.Empty : item.MotherLoad.ToString());
                    listItem.SubItems.Add(item.Type == MiningItemType.Ore
                        ? $"{item.LowContent:N0} / {item.MedContent:N0} / {item.HighContent:N0}"
                        : string.Empty);
                    _oreList.Items.Add(listItem);
                }
            }
            finally
            {
                _oreList.EndUpdate();
            }
        }

        private void UpdateHistory()
        {
            if (!_isLive)
            {
                _historyList.Items.Clear();
                return;
            }

            _historyList.BeginUpdate();
            try
            {
                _historyList.Items.Clear();

                var ordered = _tracker.Sessions.OrderByDescending(x => x.TimeStarted);
                foreach (var session in ordered)
                {
                    var duration = session.TimeFinished < DateTime.MaxValue ? session.TimeFinished - session.TimeStarted : TimeSpan.Zero;
                    var refined = session.Items.Where(x => x.Type == MiningItemType.Ore).Sum(x => x.RefinedCount);
                    var item = new ListViewItem(session.TimeStarted.ToLocalTime().ToString("g"));
                    item.SubItems.Add(duration == TimeSpan.Zero ? "-" : duration.ToString(@"hh\:mm\:ss"));
                    item.SubItems.Add(string.IsNullOrWhiteSpace(session.Location) ? session.StarSystem : session.Location);
                    item.SubItems.Add(refined.ToString("N0"));
                    item.SubItems.Add(session.ProspectorsFired.ToString("N0"));
                    item.SubItems.Add(session.CollectorsDeployed.ToString("N0"));
                    item.SubItems.Add($"{session.AsteroidsProspected:N0} / {session.AsteroidsCracked:N0}");
                    _historyList.Items.Add(item);
                }
            }
            finally
            {
                _historyList.EndUpdate();
            }
        }

        private void UpdateProspector()
        {
            if (!_isLive)
            {
                _prospectorHeader.Text = "Waiting for prospectors…";
                _prospectorList.Items.Clear();
                return;
            }

            var prospector = _tracker.LatestProspector;
            if (prospector == null)
            {
                _prospectorHeader.Text = "Waiting for prospectors…";
                _prospectorList.Items.Clear();
                return;
            }

            _prospectorHeader.Text = $"{prospector.Content} content · Remaining {prospector.Remaining:P0}";
            _prospectorList.BeginUpdate();
            try
            {
                _prospectorList.Items.Clear();
                foreach (var material in prospector.Materials.OrderByDescending(m => m.Proportion))
                {
                    var item = new ListViewItem(material.Name);
                    item.SubItems.Add($"{material.Proportion:P1}");
                    _prospectorList.Items.Add(item);
                }
            }
            finally
            {
                _prospectorList.EndUpdate();
            }
        }

        private sealed class BufferedListView : SafeListView
        {
            public BufferedListView()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                UpdateStyles();
            }
        }
    }
}
