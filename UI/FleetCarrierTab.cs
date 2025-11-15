using EliteDataRelay.Models.FleetCarrier;
using EliteDataRelay.Services;
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
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 10)
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

            Controls.Add(stockTabs);
            Controls.Add(summaryLayout);

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
            var list = new ListView
            {
                View = View.Details,
                HideSelection = false,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
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

            var section = new CarrierSection
            {
                StatusValue = AddSummaryRow(summaryTable, "Status"),
                NameValue = AddSummaryRow(summaryTable, "Name"),
                CallsignValue = AddSummaryRow(summaryTable, "Callsign"),
                LocationValue = AddSummaryRow(summaryTable, "Location"),
                FuelValue = AddSummaryRow(summaryTable, "Fuel"),
                BalanceValue = AddSummaryRow(summaryTable, "Balance"),
                DockingValue = AddSummaryRow(summaryTable, "Docking Access"),
                NotoriousValue = AddSummaryRow(summaryTable, "Allow Notorious"),
                DestinationValue = AddSummaryRow(summaryTable, "Destination"),
                DepartureValue = AddSummaryRow(summaryTable, "Departure"),
                CountdownValue = AddSummaryRow(summaryTable, "Cooldown")
            };

            var crewList = new ListView
            {
                View = View.Details,
                Dock = DockStyle.Fill,
                Height = 140,
                HideSelection = false,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = _fontManager.SegoeUIFont
            };
            crewList.Columns.Add("Crew Role", 160, HorizontalAlignment.Left);
            crewList.Columns.Add("Status", 120, HorizontalAlignment.Left);

            var groupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2
            };
            groupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            groupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            groupLayout.Controls.Add(summaryTable, 0, 0);
            groupLayout.Controls.Add(crewList, 0, 1);

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
            section.CountdownValue.Text = "–";
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
            section.FuelValue.Text = $"{state.FuelLevel:N0} t";
            section.BalanceValue.Text = $"{state.Balance:N0} CR";
            section.DockingValue.Text = string.IsNullOrWhiteSpace(state.DockingAccess) ? "—" : state.DockingAccess;
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

            section.CountdownValue.Text = FormatCountdown(state);
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
                    var item = new ListViewItem(entry.Key)
                    {
                        ForeColor = entry.Value == CarrierCrewStatus.Active ? Color.FromArgb(144, 238, 144) : Color.WhiteSmoke
                    };
                    item.SubItems.Add(entry.Value.ToString());
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
                    var notes = commodity.Stolen ? "Stolen" : string.Empty;
                    if (commodity.BlackMarket)
                    {
                        notes = string.IsNullOrEmpty(notes) ? "Black Market" : $"{notes}, Black Market";
                    }

                    if (commodity.SalePrice > 0)
                    {
                        notes = string.IsNullOrEmpty(notes) ? "For Sale" : $"{notes}, For Sale";
                    }
                    else if (commodity.OutstandingPurchaseOrders > 0)
                    {
                        notes = string.IsNullOrEmpty(notes) ? "Buying" : $"{notes}, Buying";
                    }

                    var item = new ListViewItem(commodity.DisplayName);
                    item.SubItems.Add($"{commodity.StockCount:N0}");
                    item.SubItems.Add($"{commodity.OutstandingPurchaseOrders:N0}");
                    item.SubItems.Add(commodity.SalePrice > 0 ? $"{commodity.SalePrice:N0} CR" : "—");
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
                section.CountdownValue.Text = "—";
                return;
            }

            section.CountdownValue.Text = FormatCountdown(section.LatestSnapshot);
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

        private sealed class CarrierSection
        {
            public GroupBox Group { get; set; } = null!;
            public Label StatusValue { get; set; } = null!;
            public Label NameValue { get; set; } = null!;
            public Label CallsignValue { get; set; } = null!;
            public Label LocationValue { get; set; } = null!;
            public Label FuelValue { get; set; } = null!;
            public Label BalanceValue { get; set; } = null!;
            public Label DockingValue { get; set; } = null!;
            public Label NotoriousValue { get; set; } = null!;
            public Label DestinationValue { get; set; } = null!;
            public Label DepartureValue { get; set; } = null!;
            public Label CountdownValue { get; set; } = null!;
            public ListView CrewList { get; set; } = null!;
            public FleetCarrierState? LatestSnapshot { get; set; }
        }
    }
}
