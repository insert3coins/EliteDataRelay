using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
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
                Logger.Info($"[OverlayForm.Cargo] Paint error: {ex.Message}");
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

                // Draw semi-transparent background
                using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                {
                    g.FillRectangle(bgBrush, 0, 0, width, height);
                }

                // Draw border
                g.DrawRectangle(GameColors.PenBorder2, 0, 0, width - 1, height - 1);

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

            Logger.Verbose($"[OverlayForm.Cargo] Rendered frame: {_cargoCount} items");
        }
    }
}




