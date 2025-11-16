using System;
using System.Diagnostics;
using System.Drawing;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm
    {
        private const int ProspectorBaseHeight = 140;
        private const int ProspectorRowHeight = 20;

        private void ResizeProspectorOverlay()
        {
            if (_position != OverlayPosition.Prospector || _renderPanel == null)
            {
                return;
            }

            int rowCount = Math.Min(_currentProspectorData?.Materials?.Count ?? 0, 6);
            int desiredHeight = ProspectorBaseHeight + Math.Max(rowCount, 1) * ProspectorRowHeight;
            if (Math.Abs(Height - desiredHeight) > 2)
            {
                Size = new Size(Width, desiredHeight);
                ApplyRoundedRegion();
                _stale = true;
            }
        }

        private void OnProspectorPanelPaint(object? sender, PaintEventArgs e)
        {
            if (_renderPanel == null) return;

            try
            {
                if (_stale || _frameCache == null)
                {
                    RenderProspectorFrame();
                    _stale = false;
                }

                if (_frameCache != null)
                {
                    e.Graphics.DrawImageUnscaled(_frameCache, 0, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OverlayForm.Prospector] Paint error: {ex.Message}");
            }
        }

        private void RenderProspectorFrame()
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
            if (AppConfiguration.OverlayShowBorderProspector)
            {
                g.DrawPath(borderPen, path);
            }

            float padding = 14f;
            float y = padding;

            if (_currentProspectorData == null)
            {
                var wait = "Launch a prospector limpet";
                var textSize = g.MeasureString(wait, GameColors.FontNormal);
                g.DrawString(wait, GameColors.FontNormal, GameColors.BrushGrayText,
                    (width - textSize.Width) / 2f,
                    (height - textSize.Height) / 2f);
                return;
            }

            string content = _currentProspectorData.Content?.ToString() ?? "Unknown";
            string remaining = _currentProspectorData.IsDepleted
                ? "Depleted"
                : $"{_currentProspectorData.RemainingPercent:N0}% remaining";

            g.DrawString($"Content: {content}", GameColors.FontNormal, GameColors.BrushWhite, padding, y);
            y += GameColors.FontNormal.GetHeight(g) + 4f;
            g.DrawString(remaining, GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
            y += GameColors.FontSmall.GetHeight(g) + 6f;

            if (!string.IsNullOrEmpty(_currentProspectorData.Motherlode))
            {
                g.DrawString($"Core detected: {_currentProspectorData.Motherlode}", GameColors.FontSmall, GameColors.BrushOrange, padding, y);
                y += GameColors.FontSmall.GetHeight(g) + 6f;
            }

            var materials = _currentProspectorData.Materials;
            if (materials.Count == 0)
            {
                g.DrawString("No materials discovered", GameColors.FontSmall, GameColors.BrushGrayText, padding, y);
                return;
            }

            foreach (var material in materials)
            {
                g.DrawString(material.Name, GameColors.FontNormal, GameColors.BrushWhite, padding, y);
                using var format = new StringFormat { Alignment = StringAlignment.Far };
                var rectValue = new RectangleF(padding, y, width - (padding * 2), GameColors.FontNormal.GetHeight(g));
                g.DrawString($"{material.Percentage:N2}%", GameColors.FontNormal, GameColors.BrushCyan, rectValue, format);
                y += GameColors.FontNormal.GetHeight(g) + 4f;
            }
        }
    }
}
