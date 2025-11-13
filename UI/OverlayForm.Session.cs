using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private void ResizeSessionOverlay()
        {
            if (_position != OverlayPosition.Session || _renderPanel == null || _renderPanel.IsDisposed)
                return;

            try
            {
                const int desiredWidth = 260;
                const int desiredHeight = 165;
                bool widthChanged = Math.Abs(this.Width - desiredWidth) > 2;
                bool heightChanged = Math.Abs(this.Height - desiredHeight) > 2;

                if (widthChanged || heightChanged)
                {
                    this.Size = new Size(desiredWidth, desiredHeight);
                    ApplyRoundedRegion();
                    _stale = true;
                }
            }
            catch
            {
                // Autosize best-effort only.
            }
        }

        private void OnSessionPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                if (_stale || _frameCache == null)
                {
                    RenderSessionFrame();
                    _stale = false;
                }

                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.Session] Paint error: {ex.Message}");
            }
        }

        private void RenderSessionFrame()
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
                    if (AppConfiguration.OverlayShowBorderSession)
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                float padding = 14f;
                float y = padding;

                string header = "SESSION SUMMARY";
                g.DrawString(header, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                y += GameColors.FontSmall.GetHeight(g) + 6f;

                if (!AppConfiguration.EnableSessionTracking)
                {
                    const string disabledText = "Session tracking disabled";
                    var textSize = g.MeasureString(disabledText, GameColors.FontSmall);
                    g.DrawString(disabledText, GameColors.FontSmall, GameColors.BrushGrayText,
                        (width - textSize.Width) / 2f, (height - textSize.Height) / 2f);
                    return;
                }

                DrawSessionRow(g, "Session duration", FormatDuration(_sessionDuration), GameColors.BrushWhite, width, padding, ref y, singleLineValue: true);
                DrawSessionRow(g, "Systems visited", _systemsVisited.ToString("N0"), GameColors.BrushWhite, width, padding, ref y, singleLineValue: true);
                DrawSessionRow(g, "Credits earned", _sessionCredits.ToString("N0"), GameColors.BrushOrange, width, padding, ref y);
                DrawSessionRow(g, "Cargo collected", _sessionCargo.ToString("N0"), GameColors.BrushCyan, width, padding, ref y, singleLineValue: true);
            }
        }

        private static void DrawSessionRow(Graphics g, string label, string value, Brush valueBrush, int width, float padding, ref float y, bool singleLineValue = false)
        {
            if (singleLineValue)
            {
                var labelSize = g.MeasureString(label, GameColors.FontSmall);
                g.DrawString(label, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);

                using var format = new StringFormat { Alignment = StringAlignment.Far };
                var valueRect = new RectangleF(padding, y, width - (padding * 2), GameColors.FontSmall.GetHeight(g));
                g.DrawString(value, GameColors.FontSmall, valueBrush, valueRect, format);

                y += Math.Max(labelSize.Height, valueRect.Height) + 6f;
                return;
            }

            // Label on its own line (left aligned)
            var labelOnlySize = g.MeasureString(label, GameColors.FontSmall);
            g.DrawString(label, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            y += labelOnlySize.Height;

            // Value on the next line (right aligned)
            using (var format = new StringFormat { Alignment = StringAlignment.Far })
            {
                var valueRectFull = new RectangleF(padding, y, width - (padding * 2), GameColors.FontNormal.GetHeight(g));
                g.DrawString(value, GameColors.FontNormal, valueBrush, valueRectFull, format);
                y += valueRectFull.Height + 6f;
            }
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span.TotalSeconds <= 0)
                return "--";

            return span.TotalHours >= 1
                ? span.ToString(@"hh\:mm\:ss")
                : span.ToString(@"mm\:ss");
        }
    }
}
