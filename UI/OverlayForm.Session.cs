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
                const int desiredHeight = 180;
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

                DrawSessionRow(g, "Session duration", FormatDuration(_sessionDuration), GameColors.BrushWhite, width, padding, ref y);
                DrawSessionRow(g, "Systems visited", _systemsVisited.ToString("N0"), GameColors.BrushWhite, width, padding, ref y);
                DrawSessionRow(g, "Credits earned", _sessionCredits.ToString("N0"), GameColors.BrushOrange, width, padding, ref y);
                DrawSessionRow(g, "Cargo collected", _sessionCargo.ToString("N0"), GameColors.BrushCyan, width, padding, ref y);
            }
        }

        private static void DrawSessionRow(Graphics g, string label, string value, Brush valueBrush, int width, float padding, ref float y)
        {
            g.DrawString(label, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            var valueSize = g.MeasureString(value, GameColors.FontNormal);
            g.DrawString(value, GameColors.FontNormal, valueBrush, width - padding - valueSize.Width, y - 2f);
            y += Math.Max(GameColors.FontSmall.GetHeight(g), GameColors.FontNormal.GetHeight(g)) + 6f;
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
