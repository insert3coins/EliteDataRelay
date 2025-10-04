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
                    BackColor = Color.Transparent,
                    Font = _listFont
                };
                _cargoListPanel.Paint += OnCargoListPanelPaint;

                // Initialize labels that will be used for drawing the header text.
                _cargoHeaderLabel = CreateOverlayLabel(Point.Empty, _labelFont); // This was named _cargoLabel before, but _cargoHeaderLabel is more descriptive
                _cargoSizeLabel = CreateOverlayLabel(Point.Empty, _listFont);
                _cargoBarLabel = CreateOverlayLabel(Point.Empty, _listFont); // This was using _labelFont

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
            else if (_position == OverlayPosition.ShipIcon)
            {
                this.Padding = new Padding(1); // Add padding to allow the form's border to be visible.
                this.Size = new Size(320, 320); // Match the width of the Info overlay and keep it square
                _shipIconPictureBox = new PictureBox
                {
                    // Undock the picture box to allow for manual positioning for animation.
                    Dock = DockStyle.None,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 20),
                    Location = new Point(10, 10),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    // Set the background of the PictureBox itself to be semi-transparent.
                    // The image will be drawn opaquely on top of this background.
                    BackColor = Color.Transparent,
                };
                Controls.Add(_shipIconPictureBox);
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