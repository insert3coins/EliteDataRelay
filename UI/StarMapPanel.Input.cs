using System;
using System.Drawing;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class StarMapPanel
    {
        private bool _isPanning; // Left mouse button
        private bool _isRotating; // Right mouse button

        // Rotation angles for the 3D view
        private float _rotationX = 0.5f; // Pitch
        private float _rotationY = 0.0f; // Yaw

        private void OnMapMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isRotating = true;
                _lastMousePosition = e.Location;
                Cursor = Cursors.NoMove2D;
            }
            else if (e.Button == MouseButtons.Left)
            {
                _isPanning = true;
                _lastMousePosition = e.Location;
                Cursor = Cursors.SizeAll;
            }

            this.Focus(); // Allow mouse wheel to work immediately
        }

        private void OnMapMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _isRotating = false;
                Cursor = Cursors.Default;
            }
        }

        private void OnMapMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isRotating)
            {
                float dx = e.X - _lastMousePosition.X;
                float dy = e.Y - _lastMousePosition.Y;

                // Horizontal movement controls yaw (Y-axis rotation)
                _rotationY += dx * 0.01f;
                // Vertical movement controls pitch (X-axis rotation)
                _rotationX += dy * 0.01f;
                _rotationX = Math.Clamp(_rotationX, -(float)Math.PI / 2f, (float)Math.PI / 2f);

                _lastMousePosition = e.Location;
                Invalidate();
            }
            else if (_isPanning)
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
    }
}