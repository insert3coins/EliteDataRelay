using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private void InitializeControls()
        {
            if (_position == OverlayPosition.Info)
            {
                this.Size = new Size(320, 85);
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

                _cmdrValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                _shipValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                _balanceValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);

                detailsTable.Controls.Add(CreateHeaderLabel("CMDR:"), 0, 0);
                detailsTable.Controls.Add(_cmdrValueLabel, 1, 0);
                detailsTable.Controls.Add(CreateHeaderLabel("Ship:"), 0, 1);
                detailsTable.Controls.Add(_shipValueLabel, 1, 1);
                detailsTable.Controls.Add(CreateHeaderLabel("Balance:"), 0, 2);
                detailsTable.Controls.Add(_balanceValueLabel, 1, 2);

                Controls.Add(detailsTable);
            }
            else if (_position == OverlayPosition.Cargo)
            {
                this.Size = new Size(280, 600);

                var contentPanel = new Panel
                {
                    Location = new Point(1, 1),
                    Size = new Size(this.ClientSize.Width - 2, this.ClientSize.Height - 2),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.Transparent
                };

                // Create a Panel to custom-draw the cargo list. This is more reliable for
                // dragging and gives us full control over the appearance.
                _cargoListPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = this.BackColor,
                    Font = _listFont
                };
                _cargoListPanel.Paint += OnCargoListPanelPaint;

                // Initialize labels that will be used for drawing the header text.
                _cargoHeaderLabel = CreateOverlayLabel(Point.Empty, _labelFont); // This was named _cargoLabel before, but _cargoHeaderLabel is more descriptive
                _cargoSizeLabel = CreateOverlayLabel(Point.Empty, _listFont);

                Panel? bottomPanel = null;
                Panel? bottomSeparator = null;
                if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
                {
                    bottomPanel = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 60, // Increased height to accommodate two stacked labels
                        BackColor = Color.Transparent
                    };

                    bottomSeparator = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(100, 100, 100) };

                    var sessionTablePanel = new TableLayoutPanel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.Transparent,
                        Padding = new Padding(10, 5, 10, 5),
                        ColumnCount = 2,
                        RowCount = 2,
                    };
                    sessionTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    sessionTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                    _sessionCreditsValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                    _sessionCargoValueLabel = CreateOverlayLabel(Point.Empty, _labelFont);

                    sessionTablePanel.Controls.Add(CreateHeaderLabel("Session CR:"), 0, 0);
                    sessionTablePanel.Controls.Add(_sessionCreditsValueLabel, 1, 0);
                    sessionTablePanel.Controls.Add(CreateHeaderLabel("Session Cargo:"), 0, 1);
                    sessionTablePanel.Controls.Add(_sessionCargoValueLabel, 1, 1);
                    bottomPanel.Controls.Add(sessionTablePanel);
                }

                // Add docked controls. The order is important for layout. Top and Bottom panels
                // are added first to claim their space from the edges. The Fill-docked panel
                // is added last to occupy the remaining area.
                if (bottomPanel != null)
                {
                    // Add the session panel first so it's at the very bottom.
                    contentPanel.Controls.Add(bottomPanel);
                    // Add the separator next, which will dock just above the session panel.
                    if (bottomSeparator != null)
                    {
                        contentPanel.Controls.Add(bottomSeparator);
                    }
                }
                contentPanel.Controls.Add(_cargoListPanel); // Add last to fill remaining space
                Controls.Add(contentPanel);
            }
            else if (_position == OverlayPosition.SystemInfo)
            {
                this.Size = new Size(450, 165);

                // --- Header ---
                var titleLabel = CreateOverlayLabel(new Point(10, 8), _listFont);
                titleLabel.Text = "SYSTEM INFORMATION";
                titleLabel.ForeColor = Color.FromArgb(255, 140, 0); // Header orange

                _systemNameLabel = CreateOverlayLabel(new Point(10, 25), _labelFont);

                var separator = new Label { Height = 2, Dock = DockStyle.None, BorderStyle = BorderStyle.Fixed3D, BackColor = Color.Gray, Location = new Point(10, 55), Width = this.ClientSize.Width - 20 };

                // --- Details Table ---
                var detailsTable = new TableLayoutPanel
                {
                    Location = new Point(10, 65),
                    AutoSize = true,
                    Width = this.ClientSize.Width - 20,
                    ColumnCount = 4,
                    RowCount = 4,
                    BackColor = Color.Transparent
                };
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Label
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));   // Value
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Label
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));   // Value

                // Create labels for the grid
                _allegianceLabel = CreateOverlayLabel(Point.Empty, _listFont); // Assign created label to field
                _governmentLabel = CreateOverlayLabel(Point.Empty, _listFont); // Assign created label to field
                _economyLabel = CreateOverlayLabel(Point.Empty, _listFont);    // Assign created label to field
                _securityLabel = CreateOverlayLabel(Point.Empty, _listFont);   // Assign created label to field
                _populationLabel = CreateOverlayLabel(Point.Empty, _listFont); // Assign created label to field
                _factionLabel = CreateOverlayLabel(Point.Empty, _listFont);    // Assign created label to field
                _factionLabel.AutoSize = false;
                _factionLabel.Dock = DockStyle.Fill;

                // Add labels to the table
                detailsTable.Controls.Add(CreateHeaderLabel("Allegiance:"), 0, 0);
                detailsTable.Controls.Add(_allegianceLabel, 1, 0);
                detailsTable.Controls.Add(CreateHeaderLabel("Gov:"), 2, 0);
                detailsTable.Controls.Add(_governmentLabel, 3, 0);

                detailsTable.Controls.Add(CreateHeaderLabel("Economy:"), 0, 1);
                detailsTable.Controls.Add(_economyLabel, 1, 1);
                detailsTable.Controls.Add(CreateHeaderLabel("Security:"), 2, 1);
                detailsTable.Controls.Add(_securityLabel, 3, 1);

                detailsTable.Controls.Add(CreateHeaderLabel("Population:"), 0, 2);
                detailsTable.Controls.Add(_populationLabel, 1, 2);

                detailsTable.Controls.Add(CreateHeaderLabel("Faction:"), 0, 3);
                detailsTable.SetColumnSpan(_factionLabel, 3); // Let the faction span the remaining columns
                detailsTable.Controls.Add(_factionLabel, 1, 3);

                Controls.Add(titleLabel);
                Controls.Add(_systemNameLabel);
                Controls.Add(separator);
                Controls.Add(detailsTable);
            }
            else if (_position == OverlayPosition.StationInfo)
            {
                this.Size = new Size(450, 260);

                var titleLabel = CreateOverlayLabel(new Point(10, 8), _listFont);
                titleLabel.Text = "STATION INFORMATION";
                titleLabel.ForeColor = Color.FromArgb(255, 140, 0);

                _stationNameLabel = CreateOverlayLabel(new Point(10, 25), _labelFont);
                _stationTypeLabel = CreateOverlayLabel(new Point(10, 50), _listFont);

                var separator = new Label { Height = 2, Dock = DockStyle.None, BorderStyle = BorderStyle.Fixed3D, BackColor = Color.Gray, Location = new Point(10, 75), Width = this.ClientSize.Width - 20 };

                // Use a parent FlowLayoutPanel to stack the details table and services panel vertically.
                var contentFlowPanel = new FlowLayoutPanel
                {
                    Location = new Point(10, 85),
                    Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 90),
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true
                };

                var detailsTable = new TableLayoutPanel
                {
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Width = contentFlowPanel.Width, // Set width to fill parent
                    ColumnCount = 2,
                    RowCount = 4,
                    BackColor = Color.Transparent
                };
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Labels
                detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Values

                _stationAllegianceLabel = CreateOverlayLabel(Point.Empty, _listFont); // Assign created label to field
                _stationGovernmentLabel = CreateOverlayLabel(Point.Empty, _listFont); // Assign created label to field
                _stationEconomyLabel = CreateOverlayLabel(Point.Empty, _listFont);    // Assign created label to field
                _stationFactionLabel = CreateOverlayLabel(Point.Empty, _listFont);    // Assign created label to field
                _stationFactionLabel.AutoSize = false; // Must be false for wrapping and fill to work
                _stationFactionLabel.Dock = DockStyle.Fill;
                _stationFactionLabel.TextAlign = ContentAlignment.MiddleLeft;

                detailsTable.Controls.Add(CreateHeaderLabel("Allegiance:"), 0, 0);
                detailsTable.Controls.Add(_stationAllegianceLabel, 1, 0);

                detailsTable.Controls.Add(CreateHeaderLabel("Government:"), 0, 1);
                detailsTable.Controls.Add(_stationGovernmentLabel, 1, 1);

                detailsTable.Controls.Add(CreateHeaderLabel("Economy:"), 0, 2);
                detailsTable.Controls.Add(_stationEconomyLabel, 1, 2);

                detailsTable.Controls.Add(CreateHeaderLabel("Faction:"), 0, 3);
                detailsTable.Controls.Add(_stationFactionLabel, 1, 3);

                _servicesPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Top, Width = contentFlowPanel.Width, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
                _unavailableServicesPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Top, Width = contentFlowPanel.Width, BackColor = Color.Transparent, Padding = new Padding(0, 5, 0, 0) };

                contentFlowPanel.Controls.Add(detailsTable);
                contentFlowPanel.Controls.Add(_servicesPanel);
                contentFlowPanel.Controls.Add(_unavailableServicesPanel);

                Controls.Add(titleLabel);
                Controls.Add(_stationNameLabel);
                Controls.Add(_stationTypeLabel);
                Controls.Add(separator);
                Controls.Add(contentFlowPanel);
            }
        }

        private Label CreateOverlayLabel(Point location, Font? font = null)
        {
            return new Label
            {
                Location = location,
                AutoSize = true,
                Font = font ?? _labelFont,
                ForeColor = AppConfiguration.OverlayTextColor,
                BackColor = Color.Transparent,
                Text = "" // Default to empty string 
            };
        }

        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = _listFont,
                ForeColor = SystemColors.GrayText, // Use a dimmer color for headers
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }
    }
}