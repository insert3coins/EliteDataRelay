using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private void ResizeCargoToContent()
        {
            if (_position != OverlayPosition.Cargo || _renderPanel == null || _renderPanel.IsDisposed)
                return;

            try
            {
                // Layout constants must match renderer
                const float listStartY = 38f; // where cargo list begins
                const float bottomPadding = 12f;
                bool hasSessionPanel = AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay;

                using (var g = _renderPanel.CreateGraphics())
                {
                    GameColors.ConfigureHighQuality(g);
                    float rowHeight = GameColors.FontSmall.GetHeight(g);

                    int itemLines = _cargoItems?.Any() == true ? _cargoItems.Count() : 1; // show one line for empty message
                    float contentHeight = listStartY + (itemLines * rowHeight);

                    // Reserve space for the optional session panel (approximate used in renderer)
                    if (hasSessionPanel)
                    {
                        contentHeight += 80f; // matches requiredSpace used during render
                    }
                    else
                    {
                        contentHeight += 10f; // small bottom breathing room
                    }

                    // Add padding and clamp to sensible min
                    int desiredHeight = (int)System.Math.Ceiling(contentHeight + bottomPadding);
                    int minHeight = 120; // avoid collapsing too small
                    // Keep within screen working area
                    var wa = Screen.FromControl(this).WorkingArea;
                    int maxHeight = System.Math.Max(minHeight, wa.Height - 40);

                    // Avoid negative or zero sizes
                    if (desiredHeight < minHeight) desiredHeight = minHeight;
                    if (desiredHeight > maxHeight) desiredHeight = maxHeight;

                    // Only resize if meaningfully different to avoid churn
                    if (System.Math.Abs(this.Height - desiredHeight) > 2)
                    {
                        this.Size = new Size(this.Width, desiredHeight);
                        // Re-apply rounded region to match new size
                        ApplyRoundedRegion();
                        // Mark frame stale to redraw background with new bounds
                        _stale = true;
                    }
                }
            }
            catch
            {
                // Best-effort autosize; ignore measurement errors
            }
        }

        /// <summary>
        /// Paint handler for Cargo panel - uses bitmap caching for smooth rendering.
        /// </summary>
        private void OnCargoPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                // If frame is stale, re-render to cache
                if (_stale || _frameCache == null)
                {
                    RenderCargoFrame();
                    _stale = false;
                }

                // Draw cached frame to panel
                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.Cargo] Paint error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the Cargo overlay to the cached bitmap.
        /// Layout: Header (Cargo count + bar), cargo list, optional session stats.
        /// </summary>
        private void RenderCargoFrame()
        {
            if (_renderPanel == null) return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;

            if (width <= 0 || height <= 0) return;

            // Dispose old frame and create new one
            _frameCache?.Dispose();
            _frameCache = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(_frameCache))
            {
                // Configure high-quality rendering
                GameColors.ConfigureHighQuality(g);

                // Clear background
                g.Clear(Color.Transparent);

                // Draw semi-transparent rounded background and optional border
                var rect = new Rectangle(0, 0, width - 1, height - 1);
                using (var path = DrawingUtils.CreateRoundedRectPath(rect, 12))
                using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                using (var borderPen = GameColors.PenBorder2)
                {
                    g.FillPath(bgBrush, path);
                    if (AppConfiguration.OverlayShowBorderCargo)
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Layout constants
                const float padding = 12f;
                const float headerY = 10f;
                const float listStartY = 38f;
                float y = listStartY;

                // === HEADER ===
                // "Cargo:" label (left)
                g.DrawString("Cargo:", GameColors.FontSmall, GameColors.BrushGrayText, padding, headerY);
                var cargoHeaderSize = g.MeasureString("Cargo:", GameColors.FontSmall);

                // Cargo count "128/256" (center)
                string cargoCountText = _cargoCapacity.HasValue
                    ? $"{_cargoCount}/{_cargoCapacity.Value}"
                    : $"{_cargoCount}";
                var cargoCountSize = g.MeasureString(cargoCountText, GameColors.FontNormal);

                // Cargo bar "▰▰▱▱..." (right)
                var cargoBarSize = g.MeasureString(_cargoBarText, GameColors.FontSmall);
                g.DrawString(_cargoBarText, GameColors.FontSmall, GameColors.BrushGrayText,
                             width - cargoBarSize.Width - padding, headerY);

                // Center the count between header and bar
                float leftEdge = padding + cargoHeaderSize.Width;
                float rightEdge = width - cargoBarSize.Width - padding;
                float centeredX = leftEdge + ((rightEdge - leftEdge - cargoCountSize.Width) / 2);
                g.DrawString(cargoCountText, GameColors.FontNormal, GameColors.BrushCyan, centeredX, headerY);

                // === CARGO LIST ===
                if (!_cargoItems.Any())
                {
                    g.DrawString("Cargo hold is empty.", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                }
                else
                {
                    const float nameX = padding;
                    const float countX = 200f;

                    foreach (var item in _cargoItems)
                    {
                        // Check if we're running out of space (leave room for session panel if enabled)
                        float remainingHeight = height - y;
                        bool hasSessionPanel = AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay;
                        float requiredSpace = hasSessionPanel ? 80f : 20f;

                        if (remainingHeight < requiredSpace)
                            break; // Stop drawing if we've run out of space

                        string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                        }

                        g.DrawString(displayName ?? string.Empty, GameColors.FontSmall, GameColors.BrushWhite, nameX, y);
                        g.DrawString(item.Count.ToString(), GameColors.FontSmall, GameColors.BrushWhite, countX, y);

                        y += GameColors.FontSmall.GetHeight(g);
                    }
                }

                // === SESSION STATISTICS (bottom panel) ===
                if (AppConfiguration.EnableSessionTracking && AppConfiguration.ShowSessionOnOverlay)
                {
                    float sessionY = height - 65f;

                    // Separator line
                    g.DrawLine(GameColors.PenGrayDim1, padding, sessionY, width - padding, sessionY);
                    sessionY += 8f;

                    // Session Credits
                    g.DrawString("Session CR:", GameColors.FontSmall, GameColors.BrushGrayText, padding, sessionY);
                    string sessionCreditsText = $"{_sessionCredits:N0}";
                    g.DrawString(sessionCreditsText, GameColors.FontSmall, GameColors.BrushOrange, padding + 100f, sessionY);
                    sessionY += 20f;

                    // Session Cargo
                    g.DrawString("Session Cargo:", GameColors.FontSmall, GameColors.BrushGrayText, padding, sessionY);
                    string sessionCargoText = $"{_sessionCargo}";
                    g.DrawString(sessionCargoText, GameColors.FontSmall, GameColors.BrushCyan, padding + 100f, sessionY);
                }
            }

            Debug.WriteLine($"[OverlayForm.Cargo] Rendered frame: {_cargoCount} items");
        }
    }
}
