using System;
using System.Collections.Generic;
using EliteDataRelay.Configuration;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm : Form
    {
        public enum OverlayPosition
        {
            Left,
            Right
        }

        // P/Invoke constants for making the form click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private Label _cmdrLabel = null!;
        private Label _shipLabel = null!;
        private Label _balanceLabel = null!;
        private Label _cargoLabel = null!;
        private Label _sessionCargoCollectedLabel = null!;
        private Label _sessionCreditsEarnedLabel = null!;
        private ListView _cargoListView = null!;
        private Label _cargoSizeLabel = null!;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        private readonly OverlayPosition _position;

        public OverlayForm(OverlayPosition position)
        {
            _position = position;

            // Start with zero opacity to prevent flickering/flashing during initialization.
            this.Opacity = 0;

            // Enable double buffering to reduce flicker and rendering artifacts. This is a standard
            // technique to prevent visual glitches like stray lines or bars on transparent forms.
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Text = "Elite Data Relay Overlay";
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            // --- New design ---
            this.BackColor = Color.FromArgb(30, 30, 30); // Dark background

            _labelFont = new Font("Consolas", 11F, FontStyle.Bold);
            _listFont = new Font("Consolas", 11F);
        }

        private Label CreateOverlayLabel(Point location, Font? font = null)
        {
            return new Label
            {
                Location = location,
                AutoSize = true,
                Font = font ?? _labelFont,
                ForeColor = Color.Orange, // High-visibility color for in-game
                BackColor = Color.Transparent,
                Text = "" // Default to empty string 
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Make the form click-through
            int extendedStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            if (_position == OverlayPosition.Left)
            {
                // --- Left-aligned info ---
                this.Size = new Size(280, 85);
                _cmdrLabel = CreateOverlayLabel(new Point(10, 10), _labelFont);
                _shipLabel = CreateOverlayLabel(new Point(10, 35), _labelFont);
                _balanceLabel = CreateOverlayLabel(new Point(10, 60), _labelFont);

                Controls.Add(_cmdrLabel);
                Controls.Add(_shipLabel);
                Controls.Add(_balanceLabel);
            }
            else // Right
            {
                this.Size = new Size(280, 400);

                var topPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 30,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    BackColor = Color.Transparent
                };

                _cargoLabel = new Label
                {
                    AutoSize = true,
                    Font = _labelFont,
                    ForeColor = Color.Orange,
                    BackColor = Color.Transparent,
                    Text = "", // Default to empty string
                    Margin = new Padding(10, 5, 0, 0)
                };

                _cargoSizeLabel = CreateOverlayLabel(new Point(0, 0), _listFont);
                _cargoSizeLabel.Margin = new Padding(10, 5, 0, 0);

                // Create ListView for cargo items
                _cargoListView = new ListView
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    // ListView does not support a truly transparent background.
                    // To blend in, we set its background to match the form's background.
                    BackColor = this.BackColor,
                    ForeColor = Color.Orange,
                    Font = _listFont,
                    GridLines = false,
                    BorderStyle = BorderStyle.None,
                    HeaderStyle = ColumnHeaderStyle.None,
                    FullRowSelect = false
                };
                // Define columns for a standard left-aligned list.
                _cargoListView.Columns.Add("Commodity", -2, HorizontalAlignment.Left);
                _cargoListView.Columns.Add("Count", 60, HorizontalAlignment.Left);

                Panel? bottomPanel = null;
                if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
                {
                    bottomPanel = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 60, // Increased height to accommodate two stacked labels
                        BackColor = Color.Transparent
                    };

                    var sessionFlowPanel = new FlowLayoutPanel {
                        Dock = DockStyle.Fill,
                        FlowDirection = FlowDirection.TopDown, // Stack controls vertically
                        WrapContents = false,
                        BackColor = Color.Transparent
                    };

                    _sessionCargoCollectedLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                    _sessionCargoCollectedLabel.Margin = new Padding(10, 2, 0, 0); // Smaller top margin for the second item
                    _sessionCargoCollectedLabel.AutoSize = true;

                    _sessionCreditsEarnedLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                    _sessionCreditsEarnedLabel.Margin = new Padding(10, 5, 0, 0); // Top margin for the first item
                    _sessionCreditsEarnedLabel.AutoSize = true;

                    // Add CR/hr first so it appears on top
                    sessionFlowPanel.Controls.Add(_sessionCreditsEarnedLabel);
                    sessionFlowPanel.Controls.Add(_sessionCargoCollectedLabel);
                    bottomPanel.Controls.Add(sessionFlowPanel);
                }

                topPanel.Controls.Add(_cargoLabel);
                topPanel.Controls.Add(_cargoSizeLabel);

                // Add controls in reverse order for correct docking
                Controls.Add(_cargoListView); // Fills remaining space
                if (bottomPanel != null)
                    Controls.Add(bottomPanel); // Docks to bottom
                Controls.Add(topPanel); // Docks to top
            }

            // Now that the form is fully initialized and invisible, set opacity to 1 to show it.
            // This prevents the user from seeing any part of the form's construction.
            this.Opacity = 0.85;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw a border to match the new design aesthetic
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
        }

        public void UpdateCommander(string text) => UpdateLabel(_cmdrLabel, text);
        public void UpdateShip(string text) => UpdateLabel(_shipLabel, text);
        public void UpdateBalance(string text) => UpdateLabel(_balanceLabel, text);
        public void UpdateCargo(string text) => UpdateLabel(_cargoLabel, text);
        public void UpdateCargoSize(string text) => UpdateLabel(_cargoSizeLabel, text);
        public void UpdateSessionCargoCollected(string text)
        {
            // Only update if the label was created
            if (_sessionCargoCollectedLabel != null)
            {
                UpdateLabel(_sessionCargoCollectedLabel, text);
            }
        }

        public void UpdateSessionCreditsEarned(string text)
        {
            if (_sessionCreditsEarnedLabel != null)
            {
                UpdateLabel(_sessionCreditsEarnedLabel, text);
            }
        }

        public void UpdateCargoList(IEnumerable<CargoItem> inventory)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCargoList(inventory)));
                return;
            }

            _cargoListView.BeginUpdate();
            _cargoListView.Items.Clear();

            var sortedInventory = inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name);
            foreach (var item in sortedInventory)
            {
                string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                if (!string.IsNullOrEmpty(displayName))
                {
                    displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                }

                var listViewItem = new ListViewItem(new[] { displayName, item.Count.ToString() });
                _cargoListView.Items.Add(listViewItem);
            }

            _cargoListView.EndUpdate();
        }

        private void UpdateLabel(Label label, string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => label.Text = text)); }
            else { label.Text = text; }
        }

         // Clean up any resources being used.
         // <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _labelFont?.Dispose(); _listFont?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}