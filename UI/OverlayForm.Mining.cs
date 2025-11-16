using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private const int MiningSummaryHeight = 230;
        private const int MiningRowHeight = 24;

        private void ResizeMiningOverlay()
        {
            if (_position != OverlayPosition.Mining || _renderPanel == null)
            {
                return;
            }

            int desiredHeight = MiningSummaryHeight;
            if (Math.Abs(Height - desiredHeight) > 2)
            {
                Size = new Size(Width, desiredHeight);
                ApplyRoundedRegion();
                _stale = true;
            }
        }

        private void OnMiningPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                if (_stale || _frameCache == null)
                {
                    RenderMiningFrame();
                    _stale = false;
                }

                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.Mining] Paint error: {ex.Message}");
            }
        }

        private void RenderMiningFrame()
        {
            if (_renderPanel == null) return;

            int width = _renderPanel.Width;
            int height = _renderPanel.Height;
            if (width <= 0 || height <= 0) return;

            _frameCache?.Dispose();
            _frameCache = new Bitmap(width, height);

            using var g = Graphics.FromImage(_frameCache);
            GameColors.ConfigureHighQuality(g);
            g.Clear(Color.Transparent);

            var rect = new Rectangle(0, 0, width - 1, height - 1);
            using var path = DrawingUtils.CreateRoundedRectPath(rect, 12);
            using var bgBrush = new SolidBrush(GameColors.BackgroundDark);
            using var borderPen = GameColors.PenBorder2;
            g.FillPath(bgBrush, path);
            if (AppConfiguration.OverlayShowBorderMining)
            {
                g.DrawPath(borderPen, path);
            }

            float padding = 14f;
            float labelWidth = 200f;
            float y = padding;

            if (_currentMiningData == null)
            {
                var wait = "No active mining session";
                var textSize = g.MeasureString(wait, GameColors.FontNormal);
                g.DrawString(wait, GameColors.FontNormal, GameColors.BrushGrayText,
                    (width - textSize.Width) / 2f,
                    (height - textSize.Height) / 2f);
                return;
            }

            DrawLabelValue(g, "Location", _currentMiningData.Location, padding, labelWidth, ref y);

            string durationText = $"{_currentMiningData.Duration:hh\\:mm\\:ss} - {_currentMiningData.RefinedPerHour:N1} t/hr";
            DrawLabelValue(g, "Duration", durationText, padding, labelWidth, ref y);

            string limpets = _currentMiningData.LimpetsRemaining.HasValue
                ? $"{_currentMiningData.LimpetsRemaining.Value:N0}"
                : "Unknown";
            DrawLabelValue(g, "Limpets remaining", limpets, padding, labelWidth, ref y);

            DrawLabelValue(g, "Prospectors fired", _currentMiningData.ProspectorsFired.ToString("N0"), padding, labelWidth, ref y);

            DrawLabelValue(g, "Asteroids prospected", _currentMiningData.AsteroidsProspected.ToString("N0"), padding, labelWidth, ref y);

            DrawLabelValue(g, "Asteroids cracked", _currentMiningData.AsteroidsCracked.ToString("N0"), padding, labelWidth, ref y);

            DrawLabelValue(g, "Refined (t)", $"{_currentMiningData.TotalRefined:N0}", padding, labelWidth, ref y);

            DrawLabelValue(g, "Materials collected", _currentMiningData.MaterialsCollected.ToString("N0"), padding, labelWidth, ref y);

            string content = $"L {_currentMiningData.LowContent:N0} / M {_currentMiningData.MedContent:N0} / H {_currentMiningData.HighContent:N0}";
            DrawLabelValue(g, "Content hits", content, padding, labelWidth, ref y);
        }

        private static void DrawLabelValue(Graphics g, string label, string value, float padding, float labelWidth, ref float y)
        {
            g.DrawString(label + ":", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            g.DrawString(value, GameColors.FontNormal, GameColors.BrushWhite, padding + labelWidth, y);
            y += GameColors.FontNormal.GetHeight(g) + 4f;
        }

    }
}
