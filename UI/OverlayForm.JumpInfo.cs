using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private NextJumpOverlayData? _currentJump;

        public void UpdateJumpInfo(NextJumpOverlayData data)
        {
            if (_position != OverlayPosition.JumpInfo) return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateJumpInfo(data)));
                return;
            }

            _currentJump = data;
            _stale = true;
            _renderPanel?.Invalidate();
        }

        private void OnJumpInfoPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                if (_stale || _frameCache == null)
                {
                    RenderJumpInfoFrame();
                    _stale = false;
                }

                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.JumpInfo] Paint error: {ex.Message}");
            }
        }

        private void RenderJumpInfoFrame()
        {
            if (_renderPanel == null) return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;
            if (width <= 0 || height <= 0) return;

            _frameCache?.Dispose();
            _frameCache = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(_frameCache))
            {
                GameColors.ConfigureHighQuality(g);
                g.Clear(Color.Transparent);

                var rect = new Rectangle(0, 0, width - 1, height - 1);
                using (var path = DrawingUtils.CreateRoundedRectPath(rect, 12))
                using (var bgBrush = new SolidBrush(GameColors.BackgroundDark))
                using (var borderPen = GameColors.PenBorder2)
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }

                const int padding = 12;
                const int lineHeight = 22;
                int y = padding;

                if (_currentJump == null)
                {
                    DrawCenteredText(g, "JUMPING...", GameColors.FontHeader, GameColors.BrushOrange, new Rectangle(0, height/2 - 10, width, 20));
                    return;
                }

                // Header: Next system (left)
                string header = _currentJump.SystemInfo?.SystemName ?? _currentJump.TargetSystemName ?? "Unknown";
                // Right distance (no pill)
                string distText = _currentJump.NextDistanceLy.HasValue ? $"{_currentJump.NextDistanceLy.Value:F1} ly" : (_currentJump.JumpDistanceLy.HasValue ? $"{_currentJump.JumpDistanceLy.Value:F1} ly" : "");
                int distWidth = 0;
                if (!string.IsNullOrEmpty(distText))
                {
                    var size = g.MeasureString(distText, GameColors.FontHeader);
                    distWidth = (int)Math.Ceiling(size.Width);
                    g.DrawString(distText, GameColors.FontHeader, GameColors.BrushCyan, width - padding - distWidth, y);
                }

                // Truncate header to avoid overlapping the distance on the right
                int maxHeaderWidth = Math.Max(10, width - padding * 2 - distWidth - 10);
                string headerDraw = TruncateText(g, header, GameColors.FontHeader, maxHeaderWidth);
                g.DrawString(headerDraw, GameColors.FontHeader, GameColors.BrushOrange, padding, y);
                y += lineHeight + 6;

                // Divider
                g.DrawLine(GameColors.PenGrayDim1, padding, y, width - padding, y);
                y += 8;

                // Subheader: Star class + scoopability and remaining jumps
                bool scoopable = IsScoopable(_currentJump.StarClass);
                string classPart = ""; // star info disabled
                if (!string.IsNullOrEmpty(classPart))
                {

                // Quick hints for star type (boost/scoopable/hazard)
                {
                    var sci = StarClassHelper.FromCode(_currentJump.StarClass);
                    var hintParts = new System.Collections.Generic.List<string>();
                    if (sci.IsBoostStar) hintParts.Add("Supercharge available");
                    if (!sci.IsScoopable) hintParts.Add("Non-scoopable");
                    if (sci.IsHazard) hintParts.Add("Hazard");
                    if (hintParts.Count > 0)
                    {
                        string hint = string.Join("  •  ", hintParts.ToArray());
                        string hintDraw = TruncateText(g, hint, GameColors.FontSmall, width - padding * 2);
                        g.DrawString(hintDraw, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                        y += lineHeight - 4;
                    }
                }
                }
                else { /* keep spacing minimal when hidden */ y += 2; }
                // Distance is shown in the right-side pill; omit duplicate under the header

                // Route strip (up to ~7 hops)
                y += 2; // add a little space before the strip
                DrawRouteStrip(g, padding, ref y, width);

                // No EDSM/system info here – exploration overlay covers it
            }
        }

        // (TruncateText helper is defined in another partial of OverlayForm; reuse it here.)
        private void DrawRouteStrip(Graphics g, int padding, ref int y, int width)
        {
            var data = _currentJump;
            if (data == null) return;
            var hops = data.Hops;
            if (hops == null || hops.Count == 0) return;

            int stripHeight = 30;
            int dotRadius = 5;
            int cy = y + stripHeight / 2;
            // Add an inner margin so line/dots/labels don't hug the window edges
            int innerMargin = padding + 10;
            int left = innerMargin;
            int right = width - innerMargin;
            using (var pen = GameColors.PenGrayDim1)
            {
                g.DrawLine(pen, left, cy, right, cy);
            }

            int n = hops.Count;
            if (n <= 0) { y += stripHeight; return; }
            float gap = (right - left) / Math.Max(1f, (n - 1));
            for (int i = 0; i < n; i++)
            {
                int cx = left + (int)Math.Round(i * gap);
                var hop = hops[i];
                bool scoop = hop.IsScoopable;
                int r = (i == 0) ? dotRadius + 2 : dotRadius;
                using (var brush = scoop ? GameColors.BrushCyan : GameColors.BrushGrayText)
                {
                    g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
                }
                // Stronger active-hop mark glow on first hop
                if (i == 0)
                {
                    using (var glow = new Pen(Color.FromArgb(120, 0, 180, 255), 3f))
                    {
                        g.DrawEllipse(glow, cx - (r + 4), cy - (r + 4), (r + 4) * 2, (r + 4) * 2);
                    }
                }
                // Destination ring highlight on final hop
                if (i == n - 1)
                {
                    using (var ring = new Pen(Color.FromArgb(200, 0, 180, 255), 2f))
                    {
                        g.DrawEllipse(ring, cx - (r + 3), cy - (r + 3), (r + 3) * 2, (r + 3) * 2);
                    }
                }

                // Distance labels: baseline at left, then per-hop segment distances when available
                if (i == 0)
                {
                    string dl0 = "0.0";
                    var sz0 = g.MeasureString(dl0, GameColors.FontSmall);
                    float lx0 = left - sz0.Width / 2f;
                    lx0 = Math.Max(innerMargin, Math.Min(width - innerMargin - sz0.Width, lx0));
                    g.DrawString(dl0, GameColors.FontSmall, GameColors.BrushGrayText, lx0, cy + r + 2);
                }
                if (hop.DistanceLy.HasValue)
                {
                    string dl = $"{hop.DistanceLy.Value:F1}";
                    var size = g.MeasureString(dl, GameColors.FontSmall);
                    // Clamp label within inner margins to avoid hitting borders
                    float lx = cx - size.Width / 2f;
                    lx = Math.Max(innerMargin, Math.Min(width - innerMargin - size.Width, lx));
                    g.DrawString(dl, GameColors.FontSmall, GameColors.BrushGrayText, lx, cy + r + 2);
                }
            }

            // Progress bar under strip
            y += stripHeight + 8;
            var barRect = new Rectangle(padding + 10, y, width - (padding + 10) * 2, 8);
            using (var bg = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
            using (var fill = new SolidBrush(Color.FromArgb(180, 0, 180, 255)))
            using (var pen = GameColors.PenGrayDim1)
            {
                g.FillRectangle(bg, barRect);
                double frac = 0;
                if (data.CurrentJumpIndex.HasValue && data.TotalJumps.HasValue && data.TotalJumps.Value > 0)
                {
                    frac = Math.Min(1.0, Math.Max(0.0, (double)(data.CurrentJumpIndex.Value + 1) / data.TotalJumps.Value));
                }
                var fillRect = new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * frac), barRect.Height);
                g.FillRectangle(fill, fillRect);
                g.DrawRectangle(pen, barRect);
            }

            // Footer: Jump idx/total (left subtle) and Remaining (right emphasis)
            y += 14;
            string footerL = string.Empty;
            if (data.CurrentJumpIndex.HasValue && data.TotalJumps.HasValue)
            {
                footerL = $"Jump {data.CurrentJumpIndex.Value + 1} of {data.TotalJumps.Value}";
            }
            if (!string.IsNullOrEmpty(footerL)) g.DrawString(footerL, GameColors.FontSmall, GameColors.BrushGrayText, padding + 2, y);
            if (data.TotalRemainingLy.HasValue)
            {
                string num = $"{data.TotalRemainingLy.Value:F1} ly";
                string label = "Remaining ";
                var numSize = g.MeasureString(num, GameColors.FontSmall);
                var labelSize = g.MeasureString(label, GameColors.FontSmall);
                float rx = width - padding - numSize.Width - 2;
                g.DrawString(label, GameColors.FontSmall, GameColors.BrushGrayText, rx - labelSize.Width, y);
                using (var gold = new SolidBrush(GameColors.Gold))
                {
                    g.DrawString(num, GameColors.FontSmall, gold, rx, y);
                }
            }
            y += 18;
        }

        // (Removed DrawPill; distance is rendered as plain text.)

        private void DrawBulletText(Graphics g, int x, int y, string text)
        {
            g.DrawString("•", GameColors.FontSmall, GameColors.BrushGrayText, x, y);
            g.DrawString(" " + text, GameColors.FontSmall, GameColors.BrushGrayText, x + 10, y);
        }

        private static bool IsScoopable(string? starClass)
        {
            if (string.IsNullOrEmpty(starClass)) return false;
            // In Elite Dangerous, O B A F G K M are scoopable
            var c = char.ToUpperInvariant(starClass[0]);
            return c == 'O' || c == 'B' || c == 'A' || c == 'F' || c == 'G' || c == 'K' || c == 'M';
        }
    }

    internal static class StringArrayExtensions
    {
        public static string[] FilterNonEmpty(this string[] parts)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                if (!string.IsNullOrWhiteSpace(p)) list.Add(p);
            }
            return list.ToArray();
        }
    }
}



