using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public class StarMapPanel : Panel
    {
        private List<StarSystem> _systems = new List<StarSystem>();
        private string _currentSystem = string.Empty;
        private float _zoom = 0.1f;
        private PointF _panOffset = PointF.Empty;
        private Point _lastMousePosition;
        private bool _isPanning;

        private readonly Brush _systemBrush = new SolidBrush(Color.White);
        private readonly Brush _currentSystemBrush = new SolidBrush(Color.Cyan);
        private readonly Pen _currentSystemPen = new Pen(Color.Cyan, 2);
        private readonly Font _labelFont = new Font("Verdana", 8);

        public StarMapPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;

            MouseDown += OnMapMouseDown;
            MouseUp += OnMapMouseUp;
            MouseMove += OnMapMouseMove;
            MouseWheel += OnMapMouseWheel;
        }

        public void SetSystems(IReadOnlyList<StarSystem> systems, string currentSystem)
        {
            _systems = systems.ToList();
            _currentSystem = currentSystem;
            Invalidate();
        }

        public void CenterOnSystem(string systemName)
        {
            if (string.IsNullOrEmpty(systemName) || !_systems.Any()) return;

            var systemToCenter = _systems.FirstOrDefault(s => s.Name.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
            if (systemToCenter == null) return;

            // We want the system's coordinates to be at the center of the panel.
            // The panel's center is (Width / 2, Height / 2).
            // The final screen position of a point is (worldX * zoom + panX, worldY * zoom + panY).
            // So, we solve for panX and panY:
            // panX = panelCenterX - worldX * zoom
            // panY = panelCenterY - worldY * zoom
            float panelCenterX = this.Width / 2f;
            float panelCenterY = this.Height / 2f;

            _panOffset = new PointF(panelCenterX - ((float)systemToCenter.X * _zoom), panelCenterY - ((float)systemToCenter.Z * _zoom));
            Invalidate();
        }

        private void OnMapMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _lastMousePosition = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        private void OnMapMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        private void OnMapMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                float dx = e.X - _lastMousePosition.X;
                float dy = e.Y - _lastMousePosition.Y;
                _panOffset.X += dx;
                _panOffset.Y += dy;
                _lastMousePosition = e.Location;
                Invalidate();
            }
        }

        private void OnMapMouseWheel(object? sender, MouseEventArgs e)
        {
            float oldZoom = _zoom;
            if (e.Delta > 0)
            {
                _zoom *= 1.25f;
            }
            else
            {
                _zoom /= 1.25f;
            }
            _zoom = Math.Clamp(_zoom, 0.005f, 50f);

            // Zoom towards the mouse cursor
            PointF mousePos = e.Location;
            _panOffset.X = mousePos.X - (mousePos.X - _panOffset.X) * (_zoom / oldZoom);
            _panOffset.Y = mousePos.Y - (mousePos.Y - _panOffset.Y) * (_zoom / oldZoom);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Apply pan and zoom
            g.TranslateTransform(_panOffset.X, _panOffset.Y);
            g.ScaleTransform(_zoom, _zoom);
            
            // Draw systems
            foreach (var system in _systems)
            {
                // Projecting X,Z coordinates to the 2D plane
                float screenX = (float)system.X;
                float screenY = (float)system.Z;

                bool isCurrent = system.Name.Equals(_currentSystem, StringComparison.InvariantCultureIgnoreCase);
                var brush = isCurrent ? _currentSystemBrush : _systemBrush;
                float dotSize = isCurrent ? 6f / _zoom : 3f / _zoom;

                g.FillEllipse(brush, screenX - dotSize / 2, screenY - dotSize / 2, dotSize, dotSize);

                // Draw labels if zoomed in enough
                if (_zoom > 1.5f || isCurrent)
                {
                    g.DrawString(system.Name, _labelFont, brush, screenX + dotSize, screenY);
                }

                if (isCurrent)
                {
                    float circleSize = 20f / _zoom;
                    g.DrawEllipse(_currentSystemPen, screenX - circleSize / 2, screenY - circleSize / 2, circleSize, circleSize);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _systemBrush.Dispose();
                _currentSystemBrush.Dispose();
                _currentSystemPen.Dispose();
                _labelFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}