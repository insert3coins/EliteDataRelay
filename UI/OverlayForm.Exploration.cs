using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

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
        /// Updates system info (e.g., EDSM traffic) for the exploration overlay.
        /// </summary>
        public void UpdateSystemInfo(SystemInfoData data)
        {
            if (_position != OverlayPosition.Exploration) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateSystemInfo(data)));
                return;
            }

            _currentSystemInfo = data;
            _stale = true;
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
                Debug.WriteLine($"[OverlayForm.Exploration] Paint error: {ex.Message}");
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

                // Draw semi-transparent rounded background and optional border
                var rect = new Rectangle(0, 0, width - 1, height - 1);
                using (var path = DrawingUtils.CreateRoundedRectPath(rect, 12))
                using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                using (var borderPen = GameColors.PenBorder2)
                {
                    g.FillPath(bgBrush, path);
                    if (AppConfiguration.OverlayShowBorderExploration)
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Layout constants
                const int padding = 12;
                const int lineHeight = 20;
                int y = padding;

                if (_currentExplorationData == null || string.IsNullOrEmpty(_currentExplorationData.SystemName))
                {
                    // No system data
                    DrawCenteredText(g, "NO SYSTEM DATA", GameColors.FontNormal, GameColors.BrushGrayText,
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
                    string fssText = $"FSS:     {data.FSSProgress:F1}%";
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
                    string fssText = $"FSS:     Complete ({data.TotalBodies} bodies)";
                    g.DrawString(fssText, GameColors.FontNormal, GameColors.BrushGreen, padding, y);
                    y += lineHeight;
                }

                // === DETAILED SCAN STATUS ===
                // Filter out barycentres and belt clusters when counting scanned bodies for display
                int scannedDisplay = data.Bodies.Count(b =>
                    (b.BodyType?.IndexOf("bary", StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                    (b.BodyName?.IndexOf("belt cluster", StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                    (b.BodyName?.IndexOf(" ring", StringComparison.OrdinalIgnoreCase) ?? -1) < 0);

                if (data.TotalBodies > 0)
                {
                    int shown = Math.Min(scannedDisplay, data.TotalBodies);
                    g.DrawString($"Scanned: {shown} / {data.TotalBodies}", GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                }
                else
                {
                    g.DrawString($"Scanned: {scannedDisplay}", GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                }
                y += lineHeight;

                // (Traffic moved to Known System location below)

                // === MAPPED INFO ===
                string mappedText = $"Mapped:  {data.MappedBodies}";
                g.DrawString(mappedText, GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                y += lineHeight;

                // === COMPLETION BADGES (All scanned / All mapped) ===
                bool allScanned = data.TotalBodies > 0 && data.ScannedBodies >= data.TotalBodies;
                int mappable = data.Bodies.Count(b => Services.MappabilityService.IsMappable(b));
                int mapped = data.Bodies.Count(b => b.IsMapped && Services.MappabilityService.IsMappable(b));

                var completionParts = new System.Collections.Generic.List<string>();
                if (allScanned) completionParts.Add("All scanned");
                if (mappable > 0 && mapped >= mappable) completionParts.Add("All mapped");
                if (completionParts.Count > 0)
                {
                    string completionText = "Completion: " + string.Join(" \u2022 ", completionParts);
                    g.DrawString(completionText, GameColors.FontSmall, GameColors.BrushGreen, padding, y);
                    y += lineHeight;
                }

                // Signals removed from overlay

                // === BIOLOGICAL CODEX SUMMARY ===
                int bioCodex = data.CodexBiologicalEntries?.Count ?? 0;
                if (bioCodex > 0)
                {
                    string codexText = $"Codex:  {bioCodex} bio";
                    g.DrawString(codexText, GameColors.FontNormal, GameColors.BrushCyan, padding, y);
                    y += lineHeight;
                }

                // === FIRST DISCOVERIES / MAPPINGS / FOOTFALLS ===
                var firstDiscoveries = data.Bodies.Count(b => !b.WasDiscovered);
                var firstMappings = data.Bodies.Count(b => b.IsMapped && !b.WasMapped);
                var firstFootfalls = data.Bodies.Count(b => b.FirstFootfall);
                bool hasFirsts = firstDiscoveries > 0 || firstMappings > 0 || firstFootfalls > 0;

                if (hasFirsts)
                {
                    // Line 1: First discoveries and mappings
                    string firstsText = "";
                    if (firstDiscoveries > 0) firstsText += $"First: {firstDiscoveries}";
                    if (firstMappings > 0)
                    {
                        if (!string.IsNullOrEmpty(firstsText)) firstsText += "  ";
                        firstsText += $"First mapped: {firstMappings}";
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
                        string footfallText = $"First footfall: {firstFootfalls}";
                        TextRenderer.DrawText(g, footfallText, GameColors.FontNormal,
                                              new Point(padding, y),
                                              GameColors.Gold,
                                              TextFormatFlags.Left | TextFormatFlags.NoPadding);
                        y += lineHeight;
                    }
                }

                // Always show EDSM/system info, even when firsts are present
                bool drewSystemInfo = false;
                if (AppConfiguration.ShowTrafficOnExplorationOverlay &&
                    _currentSystemInfo != null)
                {
                    // Compact, readable formatting with bullet separators, truncated to fit overlay width
                    string trafficText = $"Traffic:  {_currentSystemInfo.TrafficDay:N0} today \u2022 {_currentSystemInfo.TrafficWeek:N0} week \u2022 {_currentSystemInfo.TrafficTotal:N0} total";
                    string trafficDraw = TruncateText(g, trafficText, GameColors.FontSmall, width - padding * 2);
                    g.DrawString(trafficDraw, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                    // Source tag removed per request; show only values
                    y += lineHeight;
                    drewSystemInfo = true;
                }

                if (!drewSystemInfo)
                {
                    if (_currentSystemInfo != null)
                    {
                        var parts = new List<string>();

                        bool hasAllegiance = !string.IsNullOrWhiteSpace(_currentSystemInfo.Allegiance) &&
                                             !string.Equals(_currentSystemInfo.Allegiance, "N/A", StringComparison.OrdinalIgnoreCase);
                        bool hasSecurity = !string.IsNullOrWhiteSpace(_currentSystemInfo.Security) &&
                                           !string.Equals(_currentSystemInfo.Security, "N/A", StringComparison.OrdinalIgnoreCase);

                        if (hasAllegiance)
                            parts.Add($"Allegiance: {_currentSystemInfo.Allegiance}");
                        if (hasSecurity)
                            parts.Add($"Security: {_currentSystemInfo.Security}");
                        if (_currentSystemInfo.Population > 0)
                            parts.Add($"Pop: {_currentSystemInfo.Population:N0}");

                        if (parts.Count == 0)
                        {
                            g.DrawString("Known System", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                            y += lineHeight;
                        }
                        else
                        {
                            // Draw tokens with wrapping to avoid truncation (e.g., long allegiance values)
                            int pixels = DrawWrappedTokens(g, padding, y, width - padding * 2, parts, GameColors.FontSmall, GameColors.BrushGrayText);
                            y += pixels;
                        }
                    }
                    else
                    {
                        g.DrawString("Known System", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                        y += lineHeight;
                    }
                }

                // === SESSION STATISTICS (if available) ===
                if (_currentSessionData != null && _currentSessionData.SystemsVisited > 0)
                {
                    y += 4;
                    g.DrawLine(GameColors.PenGrayDim1, padding, y, width - padding, y);
                    y += 6;

                    string sessionText = $"Session: {_currentSessionData.SystemsVisited} systems";
                    if (_currentSessionData.TotalScans > 0)
                        sessionText += $" • {_currentSessionData.TotalScans} scans";
                    if (_currentSessionData.TotalMapped > 0)
                        sessionText += $" • {_currentSessionData.TotalMapped} mapped";

                    g.DrawString(sessionText, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                }
            }

            Debug.WriteLine($"[OverlayForm.Exploration] Rendered frame: {_currentExplorationData?.SystemName ?? "No System"}");
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

        private static int DrawWrappedTokens(Graphics g, int x, int y, int maxWidth, System.Collections.Generic.IEnumerable<string> tokens, Font font, Brush brush)
        {
            const string sep = "  \u2022  ";
            int lines = 1;
            float dx = 0f;
            bool first = true;
            foreach (var token in tokens)
            {
                string withSep = first ? token : sep + token;
                var size = g.MeasureString(withSep, font);
                if (dx + size.Width > maxWidth && !first)
                {
                    // new line
                    y += (int)Math.Ceiling(font.GetHeight(g));
                    lines++;
                    dx = 0f;
                    withSep = token; // no leading sep on new line
                    size = g.MeasureString(withSep, font);
                }
                g.DrawString(withSep, font, brush, x + dx, y);
                dx += size.Width;
                first = false;
            }
            // total height used in pixels
            int lineHeightPx = (int)Math.Ceiling(font.GetHeight(g));
            return Math.Max(lineHeightPx, lines * lineHeightPx);
        }
    }
}