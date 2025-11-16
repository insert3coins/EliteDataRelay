using EliteDataRelay.Models.FleetCarrier;
using EliteDataRelay.Services;
using EliteDataRelay.UI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Windows Forms tab that visualizes fleet carrier data (personal + squadron).
    /// </summary>
    public sealed class FleetCarrierTab : TabPage
    {
        private readonly FleetCarrierTrackerService _tracker;
        private readonly FontManager _fontManager;
        private readonly CarrierSection _personalSection;
        private readonly CarrierSection _squadSection;
        private static readonly ImageList EmptyStateImageList = CreateEmptyStateImageList();
        private readonly ListView _personalStock;
        private readonly ListView _squadStock;
        private readonly System.Windows.Forms.Timer _countdownTimer;

        public FleetCarrierTab(FleetCarrierTrackerService tracker, FontManager fontManager)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));

            Text = "Fleet Carrier";
            Padding = new Padding(10);

            _personalSection = CreateCarrierSection("Personal Carrier");
            _squadSection = CreateCarrierSection("Squadron Carrier");
            _personalStock = CreateStockListView();
            _squadStock = CreateStockListView();

            var summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 10),
                RowCount = 1
            };
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            summaryLayout.Controls.Add(_personalSection.Group, 0, 0);
            summaryLayout.Controls.Add(_squadSection.Group, 1, 0);

            var stockTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = _fontManager.SegoeUIFont
            };
            stockTabs.TabPages.Add(CreateStockTab("Personal Inventory", _personalStock));
            stockTabs.TabPages.Add(CreateStockTab("Squadron Inventory", _squadStock));

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = false,
                SplitterWidth = 4,
                Panel1MinSize = 280,
                Panel2MinSize = 70
            };

            summaryLayout.Dock = DockStyle.Fill;
            splitContainer.Panel1.Controls.Add(summaryLayout);

            stockTabs.Dock = DockStyle.Fill;
            splitContainer.Panel2.Controls.Add(stockTabs);

            Controls.Add(splitContainer);

            var preferredSummaryHeight = summaryLayout.GetPreferredSize(new Size(ClientSize.Width - Padding.Horizontal, 0)).Height + 12;
            ApplySplitterDistance(splitContainer, preferredSummaryHeight);
            splitContainer.SizeChanged += (_, _) => ApplySplitterDistance(splitContainer, preferredSummaryHeight);

            _tracker.PersonalCarrierUpdated += OnPersonalCarrierUpdated;
            _tracker.SquadronCarrierUpdated += OnSquadronCarrierUpdated;

            // Prime UI with any cached state.
            var personal = _tracker.GetPersonalCarrierSnapshot();
            if (personal != null)
            {
                UpdateSection(_personalSection, personal);
                UpdateStockList(_personalStock, personal);
            }

            var squad = _tracker.GetSquadronCarrierSnapshot();
            if (squad != null)
            {
                UpdateSection(_squadSection, squad);
                UpdateStockList(_squadStock, squad);
            }

            _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _countdownTimer.Tick += (s, e) => UpdateCountdowns();
            _countdownTimer.Start();
        }

        private TabPage CreateStockTab(string title, ListView listView)
        {
            var page = new TabPage(title) { Padding = new Padding(6) };
            listView.Dock = DockStyle.Fill;
            page.Controls.Add(listView);
            return page;
        }

        private static ListView CreateStockListView()
        {
            var list = new SafeListView
            {
                View = View.Details,
                HideSelection = false,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                UseCompatibleStateImageBehavior = true,
                CheckBoxes = false,
                StateImageList = EmptyStateImageList,
                BackColor = Color.FromArgb(245, 247, 252),
                ForeColor = Color.FromArgb(35, 39, 48),
                BorderStyle = BorderStyle.FixedSingle
            };

            list.Columns.Add("Commodity", 220, HorizontalAlignment.Left);
            list.Columns.Add("Stock", 90, HorizontalAlignment.Right);
            list.Columns.Add("Outstanding", 110, HorizontalAlignment.Right);
            list.Columns.Add("Sale Price", 110, HorizontalAlignment.Right);
            list.Columns.Add("Notes", 160, HorizontalAlignment.Left);
            return list;
        }

        private CarrierSection CreateCarrierSection(string title)
        {
            var summaryTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(4),
                Font = _fontManager.SegoeUIFont
            };
            summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var statsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 8)
            };
            statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var fuelStat = new ControlFactory.StatPanel("Fuel", "0 t", _fontManager.ConsolasFont, ControlFactory.StatPanelTheme.Light);
            var balanceStat = new ControlFactory.StatPanel("Balance", "0 CR", _fontManager.ConsolasFont, ControlFactory.StatPanelTheme.Light);
            var dockingStat = new ControlFactory.StatPanel("Docking", "Unknown", _fontManager.ConsolasFont, ControlFactory.StatPanelTheme.Light);
            var jumpStat = new ControlFactory.StatPanel("Jump Timer", "Waiting", _fontManager.ConsolasFont, ControlFactory.StatPanelTheme.Light);

            statsGrid.Controls.Add(fuelStat, 0, 0);
            statsGrid.Controls.Add(balanceStat, 1, 0);
            statsGrid.Controls.Add(dockingStat, 0, 1);
            statsGrid.Controls.Add(jumpStat, 1, 1);

            var section = new CarrierSection
            {
                StatusValue = AddSummaryRow(summaryTable, "Status"),
                NameValue = AddSummaryRow(summaryTable, "Name"),
                CallsignValue = AddSummaryRow(summaryTable, "Callsign"),
                LocationValue = AddSummaryRow(summaryTable, "Location"),
                NotoriousValue = AddSummaryRow(summaryTable, "Allow Notorious"),
                DestinationValue = AddSummaryRow(summaryTable, "Destination"),
                DepartureValue = AddSummaryRow(summaryTable, "Departure"),
                FuelStat = fuelStat,
                BalanceStat = balanceStat,
                DockingStat = dockingStat,
                JumpStat = jumpStat
            };

            var crewList = new SafeListView
            {
                View = View.Details,
                Dock = DockStyle.Fill,
                Height = 140,
                HideSelection = false,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = _fontManager.SegoeUIFont,
                UseCompatibleStateImageBehavior = true,
                CheckBoxes = false,
                StateImageList = EmptyStateImageList,
                BackColor = Color.FromArgb(245, 247, 252),
                ForeColor = Color.FromArgb(35, 39, 48),
                BorderStyle = BorderStyle.FixedSingle
            };
            crewList.Columns.Add("Crew Role", 160, HorizontalAlignment.Left);
            crewList.Columns.Add("Status", 120, HorizontalAlignment.Left);

            var groupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3
            };
            groupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            groupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            groupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            groupLayout.Controls.Add(statsGrid, 0, 0);
            groupLayout.Controls.Add(summaryTable, 0, 1);
            groupLayout.Controls.Add(crewList, 0, 2);

            var group = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = _fontManager.SegoeUIFontBold,
                Padding = new Padding(8)
            };
            group.Controls.Add(groupLayout);

            section.Group = group;
            section.CrewList = crewList;
            section.StatusValue.Text = "Waiting for journal data…";
            section.DestinationValue.Text = "No jump queued";
            section.DepartureValue.Text = "–";
            section.JumpStat.SetValue("Waiting");
            return section;
        }

        private static Label AddSummaryRow(TableLayoutPanel table, string header)
        {
            var headerLabel = new Label
            {
                Text = header,
                AutoSize = true,
                ForeColor = Color.WhiteSmoke,
                Margin = new Padding(0, 0, 10, 6)
            };

            var valueLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(249, 153, 53),
                Margin = new Padding(0, 0, 0, 6)
            };

            var row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(headerLabel, 0, row);
            table.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private void OnPersonalCarrierUpdated(object? sender, FleetCarrierState state)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnPersonalCarrierUpdated(sender, state)));
                return;
            }

            UpdateSection(_personalSection, state);
            UpdateStockList(_personalStock, state);
        }

        private void OnSquadronCarrierUpdated(object? sender, FleetCarrierState state)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnSquadronCarrierUpdated(sender, state)));
                return;
            }

            UpdateSection(_squadSection, state);
            UpdateStockList(_squadStock, state);
        }

        private void UpdateSection(CarrierSection section, FleetCarrierState state)
        {
            section.LatestSnapshot = state;
            section.StatusValue.Text = $"Updated {state.LastUpdatedUtc.ToLocalTime():g}";
            section.NameValue.Text = state.DisplayName;
            section.CallsignValue.Text = string.IsNullOrWhiteSpace(state.Callsign) ? "—" : state.Callsign;
            section.LocationValue.Text = $"{state.StarSystem} (Body {state.BodyId})";
            section.FuelStat.SetValue($"{state.FuelLevel:N0} t");
            section.BalanceStat.SetValue($"{state.Balance:N0} CR");
            section.DockingStat.SetValue(string.IsNullOrWhiteSpace(state.DockingAccess) ? "-" : state.DockingAccess);
            section.NotoriousValue.Text = state.AllowNotorious ? "Allowed" : "Denied";

            if (state.Destination.HasDestination)
            {
                var body = string.IsNullOrWhiteSpace(state.Destination.BodyName) ? string.Empty : $" / {state.Destination.BodyName}";
                section.DestinationValue.Text = $"{state.Destination.SystemName}{body}";
                section.DepartureValue.Text = state.Destination.DepartureKnown
                    ? state.Destination.DepartureTimeUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                    : "Pending";
            }
            else
            {
                section.DestinationValue.Text = "No jump queued";
                section.DepartureValue.Text = "–";
            }

            section.JumpStat.SetValue(FormatCountdown(state));
            UpdateCrewList(section.CrewList, state.Crew);
        }

        private static void UpdateCrewList(ListView listView, IReadOnlyDictionary<string, CarrierCrewStatus> crew)
        {
            if (listView.IsDisposed) return;
            if (!listView.IsHandleCreated)
            {
                listView.CreateControl();
            }

            listView.BeginUpdate();
            listView.Items.Clear();
            if (crew.Count == 0)
            {
                listView.Items.Add(new ListViewItem(new[] { "No crew data", string.Empty }));
            }
            else
            {
                foreach (var entry in crew.OrderBy(c => c.Key, StringComparer.CurrentCultureIgnoreCase))
                {
                    var (statusText, textColor, backColor) = entry.Value switch
                    {
                        CarrierCrewStatus.Active => ("Active", Color.FromArgb(0, 115, 74), Color.FromArgb(223, 244, 231)),
                        CarrierCrewStatus.Suspended => ("Unavailable", Color.FromArgb(166, 98, 0), Color.FromArgb(255, 245, 225)),
                        _ => ("Inactive", Color.FromArgb(105, 111, 124), Color.FromArgb(238, 240, 245))
                    };

                    var item = new ListViewItem(entry.Key)
                    {
                        ForeColor = Color.FromArgb(35, 39, 48)
                    };
                    item.UseItemStyleForSubItems = false;
                    var statusSubItem = item.SubItems.Add(statusText);
                    statusSubItem.ForeColor = textColor;
                    statusSubItem.BackColor = backColor;
                    listView.Items.Add(item);
                }
            }
            listView.EndUpdate();
        }

        private static void UpdateStockList(ListView listView, FleetCarrierState state)
        {
            if (listView.IsDisposed) return;
            if (!listView.IsHandleCreated)
            {
                listView.CreateControl();
            }

            listView.BeginUpdate();
            listView.Items.Clear();

            if (state.Stock.Count == 0)
            {
                listView.Items.Add(new ListViewItem(new[] { "No inventory data", string.Empty, string.Empty, string.Empty, string.Empty }));
            }
            else
            {
                foreach (var commodity in state.Stock
                    .OrderByDescending(c => c.StockCount)
                    .ThenBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                {
                    var callouts = new List<string>();
                    if (commodity.Stolen) callouts.Add("Stolen");
                    if (commodity.BlackMarket) callouts.Add("Black Market");
                    if (commodity.Rare) callouts.Add("Rare");
                    if (commodity.SalePrice > 0) callouts.Add("For Sale");
                    if (commodity.OutstandingPurchaseOrders > 0) callouts.Add("Buying");

                    var notes = string.Join(", ", callouts);
                    var textColor = Color.FromArgb(35, 39, 48);
                    var background = listView.BackColor;

                    if (commodity.Stolen)
                    {
                        textColor = Color.FromArgb(160, 32, 32);
                        background = Color.FromArgb(255, 233, 235);
                    }
                    else if (commodity.BlackMarket)
                    {
                        textColor = Color.FromArgb(173, 98, 6);
                        background = Color.FromArgb(255, 244, 221);
                    }
                    else if (commodity.SalePrice > 0)
                    {
                        textColor = Color.FromArgb(12, 102, 168);
                        background = Color.FromArgb(232, 247, 255);
                    }
                    else if (commodity.OutstandingPurchaseOrders > 0)
                    {
                        textColor = Color.FromArgb(21, 125, 81);
                        background = Color.FromArgb(232, 248, 237);
                    }

                    var item = new ListViewItem(commodity.DisplayName)
                    {
                        ForeColor = textColor,
                        BackColor = background
                    };
                    item.UseItemStyleForSubItems = false;
                    item.SubItems.Add($"{commodity.StockCount:N0}");
                    item.SubItems.Add($"{commodity.OutstandingPurchaseOrders:N0}");
                    item.SubItems.Add(commodity.SalePrice > 0 ? $"{commodity.SalePrice:N0} CR" : "–");
                    item.SubItems.Add(notes);
                    listView.Items.Add(item);
                }
            }

            listView.EndUpdate();
        }

        private void UpdateCountdowns()
        {
            if (IsDisposed) return;
            UpdateCountdown(_personalSection);
            UpdateCountdown(_squadSection);
        }

        private static void UpdateCountdown(CarrierSection section)
        {
            if (section.LatestSnapshot == null)
            {
                section.JumpStat.SetValue("—");
                return;
            }

            section.JumpStat.SetValue(FormatCountdown(section.LatestSnapshot));
        }

        private static string FormatCountdown(FleetCarrierState state)
        {
            if (state.CooldownCompleteUtc.HasValue)
            {
                var remaining = state.CooldownCompleteUtc.Value - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return "Ready";
                }
                return remaining.TotalHours >= 1
                    ? remaining.ToString(@"hh\:mm\:ss")
                    : remaining.ToString(@"mm\:ss");
            }

            if (state.JumpDepartureUtc.HasValue)
            {
                var untilDeparture = state.JumpDepartureUtc.Value - DateTime.UtcNow;
                if (untilDeparture <= TimeSpan.Zero)
                {
                    return "Jumping";
                }
                return $"Jump in {untilDeparture:mm\\:ss}";
            }

            return "Idle";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tracker.PersonalCarrierUpdated -= OnPersonalCarrierUpdated;
                _tracker.SquadronCarrierUpdated -= OnSquadronCarrierUpdated;
                _countdownTimer.Stop();
                _countdownTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private static ImageList CreateEmptyStateImageList()
        {
            var list = new ImageList();
            list.Images.Add(new Bitmap(1, 1));
            return list;
        }

        private static void ApplySplitterDistance(SplitContainer splitContainer, int desiredHeight)
        {
            if (splitContainer.Height <= 0)
            {
                return;
            }

            var minimum = splitContainer.Panel1MinSize;
            var maximum = splitContainer.Height - splitContainer.Panel2MinSize - splitContainer.SplitterWidth;
            if (maximum <= minimum)
            {
                return;
            }

            var clamped = Math.Max(minimum, Math.Min(desiredHeight, maximum));
            if (splitContainer.SplitterDistance != clamped)
            {
                splitContainer.SplitterDistance = clamped;
            }
        }

        private sealed class CarrierSection
        {
            public GroupBox Group { get; set; } = null!;
            public Label StatusValue { get; set; } = null!;
            public Label NameValue { get; set; } = null!;
            public Label CallsignValue { get; set; } = null!;
            public Label LocationValue { get; set; } = null!;
            public Label NotoriousValue { get; set; } = null!;
            public Label DestinationValue { get; set; } = null!;
            public Label DepartureValue { get; set; } = null!;
            public ControlFactory.StatPanel FuelStat { get; set; } = null!;
            public ControlFactory.StatPanel BalanceStat { get; set; } = null!;
            public ControlFactory.StatPanel DockingStat { get; set; } = null!;
            public ControlFactory.StatPanel JumpStat { get; set; } = null!;
            public ListView CrewList { get; set; } = null!;
            public FleetCarrierState? LatestSnapshot { get; set; }
        }
    }
}
