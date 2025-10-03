using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteDataRelay.UI

{
    public class ShipWireframeDrawer : IDisposable
    {
        private readonly PictureBox _canvas;
        private readonly List<Rectangle> _hardpointRects = new List<Rectangle>();
        private int _hoveredHardpointIndex = -1;
        private string _currentShipType = "cobramkiii"; // Default ship

        public event EventHandler<int>? HardpointClicked;

        // Elite Orange color scheme
        private readonly Color _orangeColor = Color.FromArgb(255, 102, 0);
        private readonly Color _lightOrangeColor = Color.FromArgb(255, 153, 68);
        private readonly Color _wireframeColor = Color.FromArgb(200, 255, 120, 0);
        private readonly Color _hardpointFillColor = Color.FromArgb(80, 255, 100, 0);
        private readonly Color _hardpointHoverFillColor = Color.FromArgb(150, 255, 100, 0);

        public ShipWireframeDrawer(PictureBox canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _canvas.Paint += OnCanvasPaint;
            _canvas.MouseMove += OnCanvasMouseMove;
            _canvas.MouseLeave += OnCanvasMouseLeave;
            _canvas.MouseClick += OnCanvasMouseClick;
        }

        public void SetShipType(string shipType)
        {
            // Normalize the ship type to lower case for consistent matching
            var normalizedShipType = shipType.ToLowerInvariant();
            if (_currentShipType != normalizedShipType)
            {
                _currentShipType = normalizedShipType;
                _canvas.Invalidate(); // Redraw with the new ship
            }
        }

        private void OnCanvasPaint(object? sender, PaintEventArgs e)
        {
            // Explicitly clear the canvas with the control's background color.
            // This ensures we start with a clean slate for every paint operation.
            e.Graphics.Clear(_canvas.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Center the drawing
            float centerX = _canvas.Width / 2.0f;
            float centerY = _canvas.Height / 2.0f;
            e.Graphics.TranslateTransform(centerX, centerY);

            using (var pen = new Pen(_wireframeColor, 1.5f))
            {
                var geometry = ShipWireframeData.GetGeometry(_currentShipType);

                foreach (var polygon in geometry.Polygons)
                {
                    e.Graphics.DrawPolygon(pen, polygon);
                }

                foreach (var line in geometry.Lines)
                {
                    e.Graphics.DrawLine(pen, line.Item1, line.Item2);
                }
            }

            // Hardpoints - This part would also need to be made dynamic per-ship
            _hardpointRects.Clear();
            // For now, we'll keep the Cobra hardpoints as a placeholder
            DrawHardpoint(e.Graphics, -90, 30, "H1", 0);
            DrawHardpoint(e.Graphics, 90, 30, "H2", 1);
            DrawHardpoint(e.Graphics, -20, 0, "H3", 2);
            DrawHardpoint(e.Graphics, 20, 0, "H4", 3);
        }

        private void DrawHardpoint(Graphics g, float x, float y, string label, int index)
        {
            var rect = new Rectangle((int)x - 6, (int)y - 6, 12, 12);
            _hardpointRects.Add(rect);

            Color fillColor = (index == _hoveredHardpointIndex) ? _hardpointHoverFillColor : _hardpointFillColor;

            using (var fillBrush = new SolidBrush(fillColor))
            using (var strokePen = new Pen(_orangeColor, 2f))
            using (var textBrush = new SolidBrush(_lightOrangeColor))
            using (var font = new Font("Consolas", 8, FontStyle.Bold))
            {
                g.FillEllipse(fillBrush, rect);
                g.DrawEllipse(strokePen, rect);
                g.DrawString(label, font, textBrush, x - 10, y + 10);
            }
        }

        private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
        {
            var p = e.Location;
            // Adjust for the transform
            p.Offset((int)-(_canvas.Width / 2.0f), (int)-(_canvas.Height / 2.0f));

            int newHoverIndex = _hardpointRects.FindIndex(r => r.Contains(p));
            if (newHoverIndex != _hoveredHardpointIndex)
            {
                _hoveredHardpointIndex = newHoverIndex;
                _canvas.Cursor = (_hoveredHardpointIndex != -1) ? Cursors.Hand : Cursors.Default;
                _canvas.Invalidate();
            }
        }

        private void OnCanvasMouseLeave(object? sender, EventArgs e)
        {
            _hoveredHardpointIndex = -1;
            _canvas.Cursor = Cursors.Default;
            _canvas.Invalidate();
        }

        private void OnCanvasMouseClick(object? sender, MouseEventArgs e)
        {
            if (_hoveredHardpointIndex != -1)
            {
                HardpointClicked?.Invoke(this, _hoveredHardpointIndex);
            }
        }

        public void Dispose()
        {
            _canvas.Paint -= OnCanvasPaint;
            _canvas.MouseMove -= OnCanvasMouseMove;
            _canvas.MouseLeave -= OnCanvasMouseLeave;
            _canvas.MouseClick -= OnCanvasMouseClick;
        }
    }
}