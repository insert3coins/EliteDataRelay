using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        /// <summary>
        /// Paint handler for ShipIcon panel - uses bitmap caching for smooth rendering.
        /// </summary>
        private void OnShipIconPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                // Check if we need to rebuild the frame cache
                bool needsRebuild = _frameCache == null ||
                                   _frameCache.Width != _renderPanel.Width ||
                                   _frameCache.Height != _renderPanel.Height ||
                                   _stale;

                if (needsRebuild)
                {
                    RenderShipIconFrame();
                    _stale = false;
                }
                else if (_shipIcon != null)
                {
                    // Just update animation without full rebuild
                    UpdateShipIconAnimation();
                }

                // Draw cached frame to panel
                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.ShipIcon] Paint error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the full ShipIcon overlay frame (background + icon).
        /// Only called when size changes or icon changes.
        /// </summary>
        private void RenderShipIconFrame()
        {
            if (_renderPanel == null) return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;

            if (width <= 0 || height <= 0) return;

            // Recreate background cache if needed
            if (_shipIconBackgroundCache == null ||
                _shipIconBackgroundCache.Width != width ||
                _shipIconBackgroundCache.Height != height)
            {
                _shipIconBackgroundCache?.Dispose();
                _shipIconBackgroundCache = new Bitmap(width, height);

                using (Graphics g = Graphics.FromImage(_shipIconBackgroundCache))
                {
                    GameColors.ConfigureHighQuality(g);
                    g.Clear(Color.Transparent);

                    // Draw semi-transparent background
                    using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                    {
                        g.FillRectangle(bgBrush, 0, 0, width, height);
                    }

                    // Draw border
                    g.DrawRectangle(GameColors.PenBorder2, 0, 0, width - 1, height - 1);
                }
            }

            // Recreate frame cache if needed
            if (_frameCache == null || _frameCache.Width != width || _frameCache.Height != height)
            {
                _frameCache?.Dispose();
                _frameCache = new Bitmap(width, height);
            }

            // Composite background + ship icon
            using (Graphics g = Graphics.FromImage(_frameCache))
            {
                // Draw background
                g.DrawImageUnscaled(_shipIconBackgroundCache, 0, 0);

                if (_shipIcon != null)
                {
                    // Calculate animated vertical offset using sine wave
                    int offsetY = (int)(Math.Sin(_animationPhase) * ANIMATION_AMPLITUDE);

                    // Calculate centered position with animation offset
                    const int iconPadding = 20;
                    int iconWidth = width - iconPadding * 2;
                    int iconHeight = height - iconPadding * 2;

                    int iconX = iconPadding;
                    int iconY = iconPadding + offsetY;

                    // Draw ship icon with high-quality scaling
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(_shipIcon, iconX, iconY, iconWidth, iconHeight);
                }
                else
                {
                    // No ship icon available
                    string noIconText = "No Ship Icon";
                    var textSize = g.MeasureString(noIconText, GameColors.FontNormal);
                    float x = (width - textSize.Width) / 2;
                    float y = (height - textSize.Height) / 2;
                    g.DrawString(noIconText, GameColors.FontNormal, GameColors.BrushGrayText, x, y);
                }
            }

            Debug.WriteLine($"[OverlayForm.ShipIcon] Full render: Icon={((_shipIcon != null) ? "Yes" : "No")}");
        }

        /// <summary>
        /// Updates just the ship icon position for animation (fast path).
        /// Only redraws the icon, not the background.
        /// </summary>
        private void UpdateShipIconAnimation()
        {
            if (_renderPanel == null || _frameCache == null || _shipIconBackgroundCache == null || _shipIcon == null)
                return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;

            using (Graphics g = Graphics.FromImage(_frameCache))
            {
                // Quick redraw: background + icon at new position
                g.DrawImageUnscaled(_shipIconBackgroundCache, 0, 0);

                // Calculate animated vertical offset using sine wave
                int offsetY = (int)(Math.Sin(_animationPhase) * ANIMATION_AMPLITUDE);

                // Calculate centered position with animation offset
                const int iconPadding = 20;
                int iconWidth = width - iconPadding * 2;
                int iconHeight = height - iconPadding * 2;

                int iconX = iconPadding;
                int iconY = iconPadding + offsetY;

                // Draw ship icon
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(_shipIcon, iconX, iconY, iconWidth, iconHeight);
            }
        }
    }
}
