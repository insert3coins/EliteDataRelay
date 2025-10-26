using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        /// <summary>
        /// Paint handler for Info panel - uses bitmap caching for smooth rendering.
        /// </summary>
        private void OnInfoPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                // If frame is stale, re-render to cache
                if (_stale || _frameCache == null)
                {
                    RenderInfoFrame();
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
                Debug.WriteLine($"[OverlayForm.Info] Paint error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the Info overlay to the cached bitmap.
        /// Layout: CMDR, Ship, Balance with Elite Dangerous styling.
        /// </summary>
        private void RenderInfoFrame()
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
                const int padding = 12;
                const int labelWidth = 70;
                int y = padding;
                const int lineHeight = 20;

                // === CMDR ===
                g.DrawString("CMDR:", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                string cmdrText = string.IsNullOrEmpty(_commanderName) ? "Unknown" : _commanderName;
                g.DrawString(cmdrText, GameColors.FontNormal, GameColors.BrushCyan, padding + labelWidth, y);
                y += lineHeight;

                // === SHIP ===
                g.DrawString("Ship:", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                string shipText = string.IsNullOrEmpty(_shipType) ? "Unknown" : _shipType;
                g.DrawString(shipText, GameColors.FontNormal, GameColors.BrushCyan, padding + labelWidth, y);
                y += lineHeight;

                // === BALANCE ===
                g.DrawString("Balance:", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                string balanceText = $"{_balance:N0} CR";
                g.DrawString(balanceText, GameColors.FontNormal, GameColors.BrushOrange, padding + labelWidth, y);
            }

            Debug.WriteLine($"[OverlayForm.Info] Rendered frame: {_commanderName}");
        }
    }
}
