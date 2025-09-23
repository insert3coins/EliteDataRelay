using System;
using System.Collections.Generic;
using EliteDataRelay.Configuration;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm : Form
    {
        public enum OverlayPosition
        {
            Info,
            Cargo,
            Materials
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

        // New fields for the materials overlay
        private IReadOnlyDictionary<string, MaterialItem> _rawMaterials = new Dictionary<string, MaterialItem>();
        private IReadOnlyDictionary<string, MaterialItem> _manufacturedMaterials = new Dictionary<string, MaterialItem>();
        private IReadOnlyDictionary<string, MaterialItem> _encodedMaterials = new Dictionary<string, MaterialItem>();
        private Panel _materialsListPanel = null!;

        private int _scrollOffset = 0;
        private int _totalContentHeight = 0;
        private bool _isMouseOverMaterialsPanel;
        private readonly bool _allowDrag;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        private readonly OverlayPosition _position;

        public OverlayForm(OverlayPosition position, bool allowDrag)
        {
            _position = position;
            _allowDrag = allowDrag;

            // Start with zero opacity to prevent flickering/flashing during initialization.
            this.Opacity = 0;

            // Enable double buffering to reduce flicker and rendering artifacts. This is a standard
            // technique to prevent visual glitches like stray lines or bars on transparent forms.
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            switch (_position)
            {
                case OverlayPosition.Info:
                    this.Text = "Elite Data Relay: Info";
                    break;
                case OverlayPosition.Cargo:
                    this.Text = "Elite Data Relay: Cargo";
                    break;
                case OverlayPosition.Materials:
                    this.Text = "Elite Data Relay: Materials";
                    break;
                default:
                    this.Text = "Elite Data Relay Overlay";
                    break;
            }

            // Apply appearance settings from configuration for semi-transparent background.
            // A form's BackColor cannot have an alpha component. We use the opaque version of the color
            // and rely on the form's Opacity property to handle the transparency.
            this.BackColor = Color.FromArgb(255, AppConfiguration.OverlayBackgroundColor);

            _labelFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _listFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);

            // Create controls in the constructor to ensure they exist before the form is shown.
            // This prevents a race condition where update methods could be called before OnLoad completes.
            if (_position == OverlayPosition.Info)
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
            else if (_position == OverlayPosition.Cargo)
            {
                this.Size = new Size(280, 600);

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
                _cargoLabel = CreateOverlayLabel(Point.Empty, _labelFont);
                _cargoSizeLabel = CreateOverlayLabel(Point.Empty, _listFont);

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

                // Add docked controls. The order is important for layout. Top and Bottom panels
                // are added first to claim their space from the edges. The Fill-docked panel
                // is added last to occupy the remaining area.
                if (bottomPanel != null)
                    Controls.Add(bottomPanel); // Docks to bottom
                Controls.Add(_cargoListPanel); // Fills remaining space
            }
            else if (_position == OverlayPosition.Materials)
            {
                this.Size = new Size(340, 500);

                var topBorder = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Color.FromArgb(100, 100, 100) };
                var bottomBorder = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(100, 100, 100) };

                _materialsListPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = this.BackColor,
                    Font = _listFont
                };
                _materialsListPanel.Paint += OnMaterialsListPanelPaint;
                _materialsListPanel.MouseWheel += OnMaterialsPanelMouseWheel;
                _materialsListPanel.MouseEnter += OnMaterialsPanelMouseEnter;
                _materialsListPanel.MouseLeave += OnMaterialsPanelMouseLeave;

                // Add docked controls in the correct order for them to stack properly.
                Controls.Add(topBorder);
                Controls.Add(bottomBorder);
                Controls.Add(_materialsListPanel);
            }

            // Wire up dragging for the form and all its children, recursively.
            AttachDragHandlers(this);
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

            // Now that the form is fully initialized and invisible, set opacity to 1 to show it.
            // This prevents the user from seeing any part of the form's construction.
            this.Opacity = AppConfiguration.OverlayOpacity / 100.0;
        }

        private void OnMaterialsPanelMouseEnter(object? sender, EventArgs e)
        {
            _isMouseOverMaterialsPanel = true;
            _materialsListPanel.Invalidate();
        }

        private void OnMaterialsPanelMouseLeave(object? sender, EventArgs e)
        {
            _isMouseOverMaterialsPanel = false;
            _materialsListPanel.Invalidate();
        }

        private void OnMaterialsPanelMouseWheel(object? sender, MouseEventArgs e)
        {
            // A standard mouse wheel tick is 120. We'll scroll by a fraction of that for smoother scrolling.
            int scrollAmount = e.Delta / -4;
            _scrollOffset += scrollAmount;

            // Clamp the scroll offset to prevent scrolling beyond the content.
            int maxScroll = Math.Max(0, _totalContentHeight - _materialsListPanel.Height);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

            // Trigger a repaint to show the new scroll position.
            _materialsListPanel.Invalidate();
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
            if (_allowDrag && e.Button == MouseButtons.Left)
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
                // 1. Draw the header text first at the top of the panel.
                e.Graphics.DrawString(_cargoLabel.Text, _labelFont, textBrush, 10, 5);
                var cargoTextSize = e.Graphics.MeasureString(_cargoLabel.Text, _labelFont);
                e.Graphics.DrawString(_cargoSizeLabel.Text, _listFont, textBrush, 10 + cargoTextSize.Width + 10, 8);

                // 2. Draw the cargo list, starting below the header area.
                float y = 35.0f;
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

        private void OnMaterialsListPanelPaint(object? sender, PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var textBrush = new SolidBrush(AppConfiguration.OverlayTextColor);
            using var headerBrush = new SolidBrush(Color.FromArgb(255, 140, 0)); // Orange for headers
            using var fullBrush = new SolidBrush(Color.Orange);

            // --- Define content width, leaving space for scrollbar if needed ---
            const int scrollbarWidth = 8;
            int contentWidth = _materialsListPanel.Width;
            bool scrollbarVisible = _totalContentHeight > _materialsListPanel.Height;
            if (scrollbarVisible)
            {
                contentWidth -= (scrollbarWidth + 4); // scrollbar + padding
            }

            // Save the original transform state and apply our custom scroll offset
            var originalTransform = e.Graphics.Transform;
            // Apply the current scroll position to the graphics context
            e.Graphics.TranslateTransform(0, -_scrollOffset);

            float y = 5.0f;

            var rawToDraw = _rawMaterials;
            var manufacturedToDraw = _manufacturedMaterials;
            var encodedToDraw = _encodedMaterials;

            if (AppConfiguration.PinMaterialsMode)
            {
                // When pinning, we want to show all pinned materials, even if the count is zero.
                // So, we build the lists to draw from the pinned configuration, not from the player's current inventory.
                var pinnedRaw = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);
                var pinnedManufactured = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);
                var pinnedEncoded = new Dictionary<string, MaterialItem>(StringComparer.InvariantCultureIgnoreCase);

                var allMaterialDefinitions = MaterialDataService.GetAll().ToDictionary(m => m.Name, m => m, StringComparer.InvariantCultureIgnoreCase);

                foreach (var pinnedMaterialName in AppConfiguration.PinnedMaterials)
                {
                    if (allMaterialDefinitions.TryGetValue(pinnedMaterialName, out var def))
                    {
                        // Check if we have this material in our inventory to get the real count.
                        _rawMaterials.TryGetValue(pinnedMaterialName, out var existingRaw);
                        _manufacturedMaterials.TryGetValue(pinnedMaterialName, out var existingManufactured);
                        _encodedMaterials.TryGetValue(pinnedMaterialName, out var existingEncoded);

                        // Use the existing item if found, otherwise create a new one with a count of 0.
                        var itemToShow = existingRaw ?? existingManufactured ?? existingEncoded ?? new MaterialItem { Name = def.Name, Localised = def.LocalisedName, Count = 0 };

                        // Add to the correct dictionary for drawing.
                        switch (def.Category.ToLowerInvariant())
                        {
                            case "raw": pinnedRaw[def.Name] = itemToShow; break;
                            case "manufactured": pinnedManufactured[def.Name] = itemToShow; break;
                            case "encoded": pinnedEncoded[def.Name] = itemToShow; break;
                        }
                    }
                }
                rawToDraw = pinnedRaw;
                manufacturedToDraw = pinnedManufactured;
                encodedToDraw = pinnedEncoded;
            }


            y = DrawMaterialCategory(e, "Raw", rawToDraw, y, textBrush, headerBrush, fullBrush, contentWidth);
            y += 10; // Add extra space between categories
            y = DrawMaterialCategory(e, "Manufactured", manufacturedToDraw, y, textBrush, headerBrush, fullBrush, contentWidth);
            y += 10;
            y = DrawMaterialCategory(e, "Encoded", encodedToDraw, y, textBrush, headerBrush, fullBrush, contentWidth);
            
            // Store the total height of the content so we know our scroll limits
            _totalContentHeight = (int)y;

            // Restore the transform to draw the scrollbar in a fixed position
            e.Graphics.Transform = originalTransform;

            // --- Draw Custom Scrollbar ---
            if (scrollbarVisible && _isMouseOverMaterialsPanel)
            {
                var scrollbarRect = new Rectangle(_materialsListPanel.Width - scrollbarWidth - 2, 2, scrollbarWidth, _materialsListPanel.Height - 4);

                // Draw the track
                using var trackBrush = new SolidBrush(Color.FromArgb(50, 128, 128, 128));
                e.Graphics.FillRectangle(trackBrush, scrollbarRect);

                // Calculate thumb size and position
                float visibleRatio = (float)_materialsListPanel.Height / _totalContentHeight;
                int thumbHeight = (int)(scrollbarRect.Height * visibleRatio);
                thumbHeight = Math.Max(thumbHeight, 20); // Minimum thumb height

                int scrollableHeight = _totalContentHeight - _materialsListPanel.Height;
                float scrollRatio = scrollableHeight > 0 ? (float)_scrollOffset / scrollableHeight : 0;
                int thumbY = scrollbarRect.Y + (int)(scrollRatio * (scrollbarRect.Height - thumbHeight));

                // Draw the thumb
                var thumbRect = new Rectangle(scrollbarRect.X, thumbY, scrollbarWidth, thumbHeight);
                using var thumbBrush = new SolidBrush(Color.FromArgb(150, 255, 140, 0)); // Semi-transparent orange
                e.Graphics.FillRectangle(thumbBrush, thumbRect);
            }
        }

        private float DrawMaterialCategory(PaintEventArgs e, string name, IReadOnlyDictionary<string, MaterialItem> materials, float y, Brush textBrush, Brush headerBrush, Brush fullBrush, int contentWidth)
        {
            const float xName = 10.0f;
            float currentY = y;

            // Draw category header
            e.Graphics.DrawString(name, _labelFont, headerBrush, xName, currentY);
            currentY += _labelFont.GetHeight(e.Graphics);

            if (!materials.Any())
            {
                using (var grayBrush = new SolidBrush(SystemColors.GrayText))
                {
                    // Add an indented "None" to indicate an empty category.
                    e.Graphics.DrawString("   - None -", _listFont, grayBrush, xName, currentY);
                }
                currentY += _listFont.GetHeight(e.Graphics);
                return currentY;
            }

            using var rightAlignFormat = new StringFormat { Alignment = StringAlignment.Far };

            foreach (var material in materials.Values.OrderBy(m => m.Localised ?? m.Name))
            {
                string nameToDisplay = !string.IsNullOrEmpty(material.Localised) ? material.Localised : material.Name;
                string displayName = nameToDisplay;
                if (!string.IsNullOrEmpty(displayName))
                {
                    displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                }

                int maxCount = MaterialDataService.GetMaxCount(material.Name);

                var brush = textBrush;
                if (maxCount > 0 && material.Count >= maxCount)
                {
                    brush = fullBrush; // Use highlight color for full materials
                }

                string countText = maxCount > 0 ? $"{material.Count} / {maxCount}" : material.Count.ToString();

                // Draw the material name on the left.
                e.Graphics.DrawString(displayName, _listFont, brush, xName, currentY);

                // Define a rectangle for the count text and draw it right-aligned to prevent overlap with the scrollbar.
                var countRect = new RectangleF(xName, currentY, contentWidth - xName - 5, _listFont.GetHeight(e.Graphics)); // -5 for right padding
                e.Graphics.DrawString(countText, _listFont, brush, countRect, rightAlignFormat);

                currentY += _listFont.GetHeight(e.Graphics);
            }

            return currentY;
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

        public void UpdateMaterials(IMaterialService materialService)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateMaterials(materialService)));
                return;
            }

            _rawMaterials = materialService.RawMaterials;
            _manufacturedMaterials = materialService.ManufacturedMaterials;
            _encodedMaterials = materialService.EncodedMaterials;

            _materialsListPanel?.Invalidate();
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