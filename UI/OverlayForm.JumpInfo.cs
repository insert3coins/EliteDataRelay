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

                // Header: Next system (left) and next distance (right)
                string header = _currentJump.SystemInfo?.SystemName ?? _currentJump.TargetSystemName ?? "Unknown";
                string rightDistance = _currentJump.NextDistanceLy.HasValue ? $"{_currentJump.NextDistanceLy.Value:F1} ly" : (_currentJump.JumpDistanceLy.HasValue ? $"{_currentJump.JumpDistanceLy.Value:F1} ly" : "");

                float rightWidth = 0;
                if (!string.IsNullOrEmpty(rightDistance))
                {
                    var size = g.MeasureString(rightDistance, GameColors.FontHeader);
                    rightWidth = size.Width;
                    g.DrawString(rightDistance, GameColors.FontHeader, GameColors.BrushCyan, width - padding - rightWidth, y);
                }
                // Truncate header to avoid hitting the right distance or border
                int maxHeaderWidth = (int)(width - padding * 2 - rightWidth - 10);
                string headerDraw = TruncateText(g, header, GameColors.FontHeader, Math.Max(10, maxHeaderWidth));
                g.DrawString(headerDraw, GameColors.FontHeader, GameColors.BrushOrange, padding, y);
                y += lineHeight + 4;

                // Divider
                g.DrawLine(GameColors.PenGrayDim1, padding, y, width - padding, y);
                y += 8;

                // Subheader: Star class + scoopability and remaining jumps
                bool scoopable = IsScoopable(_currentJump.StarClass);
                string classPart = !string.IsNullOrEmpty(_currentJump.StarClass) ? (scoopable ? $"{_currentJump.StarClass} ★" : _currentJump.StarClass!) : "";
                if (!string.IsNullOrEmpty(classPart))
                {
                    string subDraw = TruncateText(g, classPart, GameColors.FontSmall, width - padding * 2);
                    g.DrawString(subDraw, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                    y += lineHeight - 2;
                }

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
            int left = padding;
            int right = width - padding;
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
                // Optional small distance labels under first few hops where we have data
                if (hop.DistanceLy.HasValue && i < 4)
                {
                    string dl = $"{hop.DistanceLy.Value:F1}";
                    var size = g.MeasureString(dl, GameColors.FontSmall);
                    g.DrawString(dl, GameColors.FontSmall, GameColors.BrushGrayText, cx - size.Width / 2, cy + r + 2);
                }
            }

            // Footer: Jump idx/total and remaining
            y += stripHeight + 6;
            string footerL = string.Empty;
            if (data.CurrentJumpIndex.HasValue && data.TotalJumps.HasValue)
            {
                footerL = $"Jump {data.CurrentJumpIndex.Value + 1} of {data.TotalJumps.Value}";
            }
            string footerR = data.TotalRemainingLy.HasValue ? $"Remaining: {data.TotalRemainingLy.Value:F1} ly" : string.Empty;
            if (!string.IsNullOrEmpty(footerL)) g.DrawString(footerL, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            if (!string.IsNullOrEmpty(footerR))
            {
                var size = g.MeasureString(footerR, GameColors.FontSmall);
                g.DrawString(footerR, GameColors.FontSmall, GameColors.BrushGrayText, width - padding - size.Width, y);
            }
            y += 18;
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
