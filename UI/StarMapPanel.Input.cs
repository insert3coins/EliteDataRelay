using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class StarMapPanel
    {
        private string? _hoveredSystemName;
        private bool _isPanning; // Left mouse button
        private bool _isRotating; // Right mouse button
        private bool _isMouseOverInfoCard;

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
            else // Not rotating or panning, so check for hover
            {
                HitTestSystems(e.Location);
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

        private async void OnMapMouseLeave(object? sender, EventArgs e)
        {
            // Add a short delay before hiding the card. This gives the user time
            // to move their mouse onto the info card to use its scrollbar.
            await Task.Delay(300);

            // If the mouse has moved onto the info card itself, don't hide it.
            if (_isMouseOverInfoCard)
            {
                return;
            }

            // Otherwise, clear the hover state and hide the card.
            if (_hoveredSystemName != null)
            {
                _hoveredSystemName = null;
                _systemInfoCard.Visible = false;
                Invalidate(); // Redraw to hide the label
            }
        }

        private void HitTestSystems(Point mouseLocation)
        {
            string? foundSystemName = null;
            StarSystem? foundSystem = null;
            // Iterate backwards so we hit the one on top (drawn last) first.
            for (int i = _lastDrawableSystems.Count - 1; i >= 0; i--)
            {
                var ds = _lastDrawableSystems[i];

                // Calculate the system's position on the screen
                float screenX = ds.RotatedX * _zoom + _panOffset.X;
                float screenY = ds.RotatedY * _zoom + _panOffset.Y;

                // Use a small radius for hit-testing
                const float hitRadius = 5f;
                var dx = mouseLocation.X - screenX;
                var dy = mouseLocation.Y - screenY;

                if ((dx * dx + dy * dy) < (hitRadius * hitRadius))
                {
                    foundSystemName = ds.System.Name;
                    foundSystem = ds.System;
                    break; // Found the topmost system
                }
            }

            // If the hovered system has changed, trigger a redraw
            if (foundSystemName != _hoveredSystemName)
            {
                _hoveredSystemName = foundSystemName;
                Invalidate(); // Redraw to show/hide the new label on the map

                if (foundSystem != null)
                {
                    // Rebuild the info card content only when the hovered system changes.
                    RebuildSystemInfoCard(foundSystem);
                }
            }

            if (foundSystem != null)
            {
                // Update the card's position on every mouse move.
                PositionSystemInfoCard(mouseLocation);
                _systemInfoCard.Visible = true;
            }
            else
            {
                _systemInfoCard.Visible = false;
            }
        }

        private void RebuildSystemInfoCard(StarSystem system)
        {
            // Wire up events to track if the mouse is over the info card.
            // This prevents it from hiding when the user tries to scroll.
            _systemInfoCard.MouseEnter -= OnInfoCardMouseEnter;
            _systemInfoCard.MouseLeave -= OnInfoCardMouseLeave;
            _systemInfoCard.MouseEnter += OnInfoCardMouseEnter;
            _systemInfoCard.MouseLeave += OnInfoCardMouseLeave;

            _systemInfoCard.SuspendLayout();
            _systemInfoCard.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = Point.Empty,
                ColumnCount = 1,
            };

            var titleLabel = new Label { Text = system.Name, Font = _infoTitleFont, ForeColor = Color.White, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            layout.Controls.Add(titleLabel);

            if (system.Bodies.Any())
            {
                var bodyCountLabel = new Label { Text = $"{system.Bodies.Count} bodies discovered", Font = _labelFont, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
                layout.Controls.Add(bodyCountLabel);

                foreach (var body in system.Bodies.OrderBy(b => b.BodyName))
                {
                    string bodyType = body.PlanetClass ?? body.StarType ?? "Unknown Body";
                    string attributes = "";
                    if (body.TerraformState == "Terraformable") attributes += "[T] ";
                    if (body.Landable) attributes += "[L] ";
                    if (body.WasMapped) attributes += "[M] ";

                    var bodyLabel = new Label { Text = $"• {body.BodyName}: {bodyType} {attributes.Trim()}", Font = _labelFont, ForeColor = Color.LightGray, AutoSize = true };
                    layout.Controls.Add(bodyLabel);
                }
            }
            else
            {
                var noDataLabel = new Label { Text = "No exploration data available.", Font = _labelFont, ForeColor = Color.Gray, AutoSize = true };
                layout.Controls.Add(noDataLabel);
            }

            _systemInfoCard.Controls.Add(layout);

            // Because the parent panel has AutoScroll=true, its AutoSize property is disabled.
            // We must manually calculate and set its size based on the content.
            var preferredSize = layout.GetPreferredSize(Size.Empty);

            // Calculate the maximum allowable size for the card, ensuring it's smaller than the map panel itself.
            int maxWidth = Math.Min(this.Width - 40, _systemInfoCard.MaximumSize.Width);
            int maxHeight = Math.Min(this.Height - 40, _systemInfoCard.MaximumSize.Height);

            // Add padding and a little extra width for the scrollbar to prevent text wrapping.
            int newWidth = Math.Min(preferredSize.Width + _systemInfoCard.Padding.Horizontal + 25, maxWidth);
            int newHeight = Math.Min(preferredSize.Height + _systemInfoCard.Padding.Vertical, maxHeight);

            _systemInfoCard.Size = new Size(newWidth, newHeight);
            _systemInfoCard.ResumeLayout(true);
        }

        private void OnInfoCardMouseEnter(object? sender, EventArgs e)
        {
            _isMouseOverInfoCard = true;
        }

        private void OnInfoCardMouseLeave(object? sender, EventArgs e)
        {
            _isMouseOverInfoCard = false;
            OnMapMouseLeave(sender, e); // Trigger the hide logic now
        }

        private void PositionSystemInfoCard(Point mouseLocation)
        {
            // Position the card centered horizontally and always below the mouse cursor.
            int x = mouseLocation.X - (_systemInfoCard.Width / 2);
            int y = mouseLocation.Y + 20; // A small offset to avoid covering the cursor

            // Clamp the horizontal position to prevent the card from going too far off-screen,
            // while always keeping it below the cursor.
            x = Math.Max(5, x);
            x = Math.Min(x, this.Width - _systemInfoCard.Width - 5);

            _systemInfoCard.Location = new Point(x, y);
        }
    }
}