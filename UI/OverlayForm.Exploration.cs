using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Services;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        /// <summary>
        /// Updates exploration data and marks the frame as stale for re-rendering.
        /// Inspired by SrvSurvey's PlotBase2 stale tracking system.
        /// </summary>
        public void UpdateExplorationData(SystemExplorationData? systemData)
        {
            if (_position != OverlayPosition.Exploration) return;

            _currentExplorationData = systemData;
            _stale = true; // Mark frame as needing re-render

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateExplorationData(systemData)));
                return;
            }

            // Trigger repaint with new data
            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Updates session data for the exploration overlay.
        /// </summary>
        public void UpdateExplorationSessionData(ExplorationSessionData? sessionData)
        {
            if (_position != OverlayPosition.Exploration) return;

            _currentSessionData = sessionData;
            _stale = true;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateExplorationSessionData(sessionData)));
                return;
            }

            _renderPanel?.Invalidate();
        }

        /// <summary>
        /// Paint handler for exploration panel - uses bitmap caching for smooth rendering.
        /// Based on SrvSurvey's PlotBase2 pattern.
        /// </summary>
        private void OnExplorationPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                // If frame is stale, re-render to cache
                if (_stale || _frameCache == null)
                {
                    RenderExplorationFrame();
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
                Logger.Info($"[OverlayForm.Exploration] Paint error: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders the exploration overlay to the cached bitmap.
        /// Uses Elite Dangerous color palette and custom drawing.
        /// </summary>
        private void RenderExplorationFrame()
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
                const int lineHeight = 20;
                int y = padding;

                if (_currentExplorationData == null || string.IsNullOrEmpty(_currentExplorationData.SystemName))
                {
                    // No system data
                    DrawCenteredText(g, Properties.Strings.Overlay_NoSystemData, GameColors.FontNormal, GameColors.BrushGrayText,
                                     new Rectangle(0, height / 2 - 10, width, 20));
                    return;
                }

                var data = _currentExplorationData;

                // === HEADER: System Name ===
                string systemName = TruncateText(g, data.SystemName, GameColors.FontHeader, width - padding * 2);
                g.DrawString(systemName, GameColors.FontHeader, GameColors.BrushOrange, padding, y);
                y += lineHeight + 4;

                // Draw separator line (use gray like other separators)
                g.DrawLine(GameColors.PenGrayDim1, padding, y, width - padding, y);
                y += 8;

                // === FSS DETECTION STATUS ===
                // Show FSS progress if not complete
                if (data.FSSProgress > 0 && data.FSSProgress < 100)
                {
                    string fssText = $"{0}";
                    if (data.TotalBodies > 0)
                    {
                        // Calculate detected bodies from percentage
                        int detectedBodies = (int)Math.Round(data.TotalBodies * (data.FSSProgress / 100.0));
                        fssText += $"  ({detectedBodies}/{data.TotalBodies} detected)";
                    }
                    g.DrawString(fssText, GameColors.FontNormal, GameColors.BrushOrange, padding, y);
                    y += lineHeight;
                }
                else if (data.FSSProgress >= 100 && data.TotalBodies > 0)
                {
                    // FSS Complete
                    string fssText = string.Format(Properties.Strings.Overlay_FSS_CompleteFormat, data.TotalBodies);
                    g.DrawString(fssText, GameColors.FontNormal, GameColors.BrushGreen, padding, y);
                    y += lineHeight;
                }

                // === DETAILED SCAN STATUS ===
                string bodiesText = data.TotalBodies > 0
                    ? $"{0}"
                    : string.Format(Properties.Strings.Overlay_ScannedFormat, data.ScannedBodies);

                g.DrawString(bodiesText, GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                y += lineHeight;

                // === MAPPED INFO ===
                string mappedText = string.Format(Properties.Strings.Overlay_MappedFormat, data.MappedBodies);
                g.DrawString(mappedText, GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                y += lineHeight;

                // === FIRST DISCOVERIES / MAPPINGS / FOOTFALLS ===
                var firstDiscoveries = data.Bodies.Count(b => !b.WasDiscovered);
                var firstMappings = data.Bodies.Count(b => b.IsMapped && !b.WasMapped);
                var firstFootfalls = data.Bodies.Count(b => b.FirstFootfall);

                if (firstDiscoveries > 0 || firstMappings > 0 || firstFootfalls > 0)
                {
                    // Line 1: First discoveries and mappings
                    string firstsText = "";
                    if (firstDiscoveries > 0) firstsText += $"⭐ {firstDiscoveries} First";
                    if (firstMappings > 0)
                    {
                        if (!string.IsNullOrEmpty(firstsText)) firstsText += "  ";
                        firstsText += $"🗺️ {firstMappings} Mapped";
                    }

                    if (!string.IsNullOrEmpty(firstsText))
                    {
                        // Use TextRenderer for better emoji support. Graphics.DrawString can fail to render color emojis.
                        TextRenderer.DrawText(g, firstsText, GameColors.FontNormal,
                                              new Point(padding, y),
                                              GameColors.Gold,
                                              TextFormatFlags.Left | TextFormatFlags.NoPadding);
                        y += lineHeight;
                    }

                    // Line 2: First footfalls (if any)
                    if (firstFootfalls > 0)
                    {
                        string footfallText = $"👣 {firstFootfalls} First Footfall";
                        TextRenderer.DrawText(g, footfallText, GameColors.FontNormal,
                                              new Point(padding, y),
                                              GameColors.Gold,
                                              TextFormatFlags.Left | TextFormatFlags.NoPadding);
                        y += lineHeight;
                    }
                }
                else if (data.Bodies.Any(b => b.WasDiscovered))
                {
                    // Only show Properties.Strings.Overlay_KnownSystem if we have scanned bodies that were already discovered
                    g.DrawString(Properties.Strings.Overlay_KnownSystem, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                    y += lineHeight;
                }

                // === SESSION STATISTICS (if available) ===
                if (_currentSessionData != null && _currentSessionData.SystemsVisited > 0)
                {
                    y += 4;
                    g.DrawLine(GameColors.PenGrayDim1, padding, y, width - padding, y);
                    y += 6;

                    string sessionText = string.Format(Properties.Strings.Overlay_SessionFormat, _currentSessionData.SystemsVisited);
                    if (_currentSessionData.TotalScans > 0)
                        sessionText += $" • {_currentSessionData.TotalScans} scans";
                    if (_currentSessionData.TotalMapped > 0)
                        sessionText += $" • {_currentSessionData.TotalMapped} mapped";

                    g.DrawString(sessionText, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                }
            }

            Logger.Verbose($"[OverlayForm.Exploration] Rendered frame: {_currentExplorationData?.SystemName ?? "No System"}");
        }

        /// <summary>
        /// Truncates text to fit within specified width with ellipsis.
        /// </summary>
        private string TruncateText(Graphics g, string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var size = g.MeasureString(text, font);
            if (size.Width <= maxWidth) return text;

            // Binary search for best fit
            int left = 0, right = text.Length;
            string result = text;

            while (left < right)
            {
                int mid = (left + right + 1) / 2;
                string truncated = text.Substring(0, mid) + "...";
                size = g.MeasureString(truncated, font);

                if (size.Width <= maxWidth)
                {
                    result = truncated;
                    left = mid;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Draws centered text within a rectangle.
        /// </summary>
        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, Rectangle rect)
        {
            var size = g.MeasureString(text, font);
            float x = rect.X + (rect.Width - size.Width) / 2;
            float y = rect.Y + (rect.Height - size.Height) / 2;
            g.DrawString(text, font, brush, x, y);
        }
    }
}





