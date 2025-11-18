using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private const int MiningSummaryHeight = 250;

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

            var rows = new (string Label, string Value)[]
            {
                ("Location", _currentMiningData.Location),
                ("Duration", $"{_currentMiningData.Duration:hh\\:mm\\:ss} · {_currentMiningData.RefinedPerHour:N1} t/hr"),
                ("Limpets remaining", _currentMiningData.LimpetsRemaining.HasValue ? $"{_currentMiningData.LimpetsRemaining.Value:N0}" : "Unknown"),
                ("Collectors deployed", _currentMiningData.CollectorsDeployed.ToString("N0")),
                ("Prospectors fired", _currentMiningData.ProspectorsFired.ToString("N0")),
                ("Asteroids prospected", _currentMiningData.AsteroidsProspected.ToString("N0")),
                ("Asteroids cracked", _currentMiningData.AsteroidsCracked.ToString("N0")),
                ("Refined (t)", $"{_currentMiningData.TotalRefined:N0}"),
                ("Materials collected", _currentMiningData.MaterialsCollected.ToString("N0")),
                ("Content hits", $"L {_currentMiningData.LowContent:N0} / M {_currentMiningData.MedContent:N0} / H {_currentMiningData.HighContent:N0}")
            };

            int labelColumnWidth = rows.Max(r => TextRenderer.MeasureText(r.Label + ":", GameColors.FontSmall).Width);
            int valueColumnWidth = rows.Max(r => TextRenderer.MeasureText(r.Value ?? string.Empty, GameColors.FontSmall).Width);
            int desiredWidth = (int)Math.Ceiling(padding * 2 + labelColumnWidth + 16 + valueColumnWidth);
            int autoWidth = Math.Max(320, desiredWidth);
            if (Math.Abs(autoWidth - this.Width) > 2)
            {
                this.Width = autoWidth;
                _renderPanel.Width = autoWidth;
                ApplyRoundedRegion();
            }

            float labelWidth = labelColumnWidth + 8f;

            foreach (var row in rows)
            {
                DrawLabelValue(g, row.Label, row.Value, padding, labelWidth, ref y);
            }
        }

        private static void DrawLabelValue(Graphics g, string label, string value, float padding, float labelWidth, ref float y)
        {
            g.DrawString(label + ":", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            g.DrawString(value, GameColors.FontSmall, GameColors.BrushWhite, padding + labelWidth, y);
            y += GameColors.FontSmall.GetHeight(g) + 4f;
        }

    }
}



