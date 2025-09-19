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

        public event EventHandler<Point>? PositionChanged;
        private bool _isDragging;
        private Point _dragCursorStartPoint;
        private Point _dragFormStartPoint;

        private Label _cmdrLabel = null!;
        private Label _shipLabel = null!;
        private Label _balanceLabel = null!;
        private Label _cargoLabel = null!;
        private Label _sessionCargoCollectedLabel = null!;
        private Label _sessionCreditsEarnedLabel = null!;
        private Panel _cargoListPanel = null!;
        private Label _cargoSizeLabel = null!;
        private IEnumerable<CargoItem> _cargoItems = Enumerable.Empty<CargoItem>();

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

            // Apply appearance settings from configuration
            this.BackColor = AppConfiguration.OverlayBackgroundColor;

            _labelFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _listFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

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
                    ForeColor = AppConfiguration.OverlayTextColor,
                    BackColor = Color.Transparent,
                    Text = "", // Default to empty string
                    Margin = new Padding(10, 5, 0, 0)
                };

                _cargoSizeLabel = CreateOverlayLabel(new Point(0, 0), _listFont);
                _cargoSizeLabel.Margin = new Padding(10, 5, 0, 0);

                // Create a Panel to custom-draw the cargo list. This is more reliable for
                // dragging and gives us full control over the appearance.
                _cargoListPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = this.BackColor,
                    Font = _listFont
                };
                _cargoListPanel.Paint += OnCargoListPanelPaint;

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

                // Add docked controls. For docking to work correctly, the Fill-docked control
                // must be added first to be at the bottom of the Z-order.
                Controls.Add(_cargoListPanel); // Fills remaining space
                if (bottomPanel != null)
                    Controls.Add(bottomPanel); // Docks to bottom
                Controls.Add(topPanel); // Docks to top
            }

            // Wire up dragging for the form and all its children, recursively.
            AttachDragHandlers(this);

            // Now that the form is fully initialized and invisible, set opacity to 1 to show it.
            // This prevents the user from seeing any part of the form's construction.
            this.Opacity = AppConfiguration.OverlayOpacity / 100.0;
        }

        private void AttachDragHandlers(Control control)
        {
            control.MouseDown += OnOverlayMouseDown;
            control.MouseMove += OnOverlayMouseMove;
            control.MouseUp += OnOverlayMouseUp;
            foreach (Control child in control.Controls)
            {
                AttachDragHandlers(child);
            }
        }

        private void OnOverlayMouseDown(object? sender, MouseEventArgs e)
        {
            if (AppConfiguration.AllowOverlayDrag && e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragCursorStartPoint = Cursor.Position;
                _dragFormStartPoint = this.Location;
                this.Cursor = Cursors.SizeAll;
            }
        }

        private void OnOverlayMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentCursorPos = Cursor.Position;
                int deltaX = currentCursorPos.X - _dragCursorStartPoint.X;
                int deltaY = currentCursorPos.Y - _dragCursorStartPoint.Y;
                this.Location = new Point(_dragFormStartPoint.X + deltaX, _dragFormStartPoint.Y + deltaY);
            }
        }

        private void OnOverlayMouseUp(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.Cursor = Cursors.Default;
                // Raise the event to notify the service to save the new position.
                PositionChanged?.Invoke(this, this.Location);
            }
        }

        private void OnCargoListPanelPaint(object? sender, PaintEventArgs e)
        {
            // Use higher quality text rendering for clarity in-game.
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var textBrush = new SolidBrush(AppConfiguration.OverlayTextColor))
            using (var grayBrush = new SolidBrush(SystemColors.GrayText))
            {
                float y = 5.0f;
                const float xName = 10.0f;
                const float xCount = 200.0f;

                var itemsToDraw = _cargoItems.ToList();

                if (!itemsToDraw.Any())
                {
                    e.Graphics.DrawString("Cargo hold is empty.", _listFont, grayBrush, xName, y);
                    return;
                }

                foreach (var item in itemsToDraw)
                {
                    string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                    }

                    if (displayName != null)
                    {
                        e.Graphics.DrawString(displayName, _listFont, textBrush, xName, y);
                        e.Graphics.DrawString(item.Count.ToString(), _listFont, textBrush, xCount, y);
                    }

                    y += _listFont.GetHeight(e.Graphics);
                }
            }
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

            _cargoItems = inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name).ToList();
            _cargoListPanel?.Invalidate();
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
            if (disposing)
            {
                _labelFont?.Dispose();
                _listFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}