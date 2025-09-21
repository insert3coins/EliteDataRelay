using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    public partial class StarMapPanel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // --- 3D Transformation and Sorting ---
            var drawableSystems = new List<DrawableSystem>();
            var drawableBgStars = new List<DrawableBackgroundStar>();

            float cosX = (float)Math.Cos(_rotationX);
            float sinX = (float)Math.Sin(_rotationX);
            float cosY = (float)Math.Cos(_rotationY);
            float sinY = (float)Math.Sin(_rotationY);

            // Transform background stars
            foreach (var star in _backgroundStars)
            {
                double tempX = star.X * cosY + star.Z * sinY;
                double tempZ = -star.X * sinY + star.Z * cosY;
                double rotatedY = star.Y * cosX - tempZ * sinX;
                double rotatedZ = star.Y * sinX + tempZ * cosX;

                drawableBgStars.Add(new DrawableBackgroundStar
                {
                    Brush = star.Brush,
                    RotatedX = (float)tempX,
                    RotatedY = (float)rotatedY,
                    RotatedZ = (float)rotatedZ
                });
            }

            foreach (var system in _systems)
            {
                // Rotate the star's position
                double tempX_star = system.X * cosY + system.Z * sinY;
                double tempZ_star = -system.X * sinY + system.Z * cosY;
                double rotatedY_star = system.Y * cosX - tempZ_star * sinX;
                double rotatedZ_star = system.Y * sinX + tempZ_star * cosX;

                // Rotate the position on the galactic plane (Y=0)
                double tempX_plane = system.X * cosY + system.Z * sinY;
                double tempZ_plane = -system.X * sinY + system.Z * cosY;
                double rotatedY_plane = 0 * cosX - tempZ_plane * sinX;

                drawableSystems.Add(new DrawableSystem
                {
                    System = system,
                    RotatedX = (float)tempX_star,
                    RotatedY = (float)rotatedY_star,
                    RotatedZ = (float)rotatedZ_star,
                    PlaneX = (float)tempX_plane,
                    PlaneY = (float)rotatedY_plane
                });
            }

            // Sort systems from back to front to handle occlusion correctly
            drawableBgStars.Sort((a, b) => a.RotatedZ.CompareTo(b.RotatedZ));
            drawableSystems.Sort((a, b) => a.RotatedZ.CompareTo(b.RotatedZ));

            // Apply pan and zoom
            g.TranslateTransform(_panOffset.X, _panOffset.Y);
            g.ScaleTransform(_zoom, _zoom);

            // --- Drawing --- //

            // Draw background stars first
            foreach (var bgStar in drawableBgStars)
            {
                float dotSize = 1.5f / _zoom;
                g.FillEllipse(bgStar.Brush, bgStar.RotatedX - dotSize / 2, bgStar.RotatedY - dotSize / 2, dotSize, dotSize);
            }

            // Draw a simple grid for the galactic plane (Y=0)
            using (var gridPen = new Pen(Color.FromArgb(40, 255, 255, 255)))
            {
                int gridSize = 1000;
                int gridLines = 20;
                float gridLimit = gridLines * gridSize / 2f;

                for (int i = -gridLines / 2; i <= gridLines / 2; i++)
                {
                    float pos = i * gridSize;

                    // Rotate grid lines. We only need to rotate the start and end points.
                    // Line along Z axis
                    PointF p1 = RotatePoint(new PointF(pos, -gridLimit));
                    PointF p2 = RotatePoint(new PointF(pos, gridLimit));
                    g.DrawLine(gridPen, p1, p2);

                    // Line along X axis
                    PointF p3 = RotatePoint(new PointF(-gridLimit, pos));
                    PointF p4 = RotatePoint(new PointF(gridLimit, pos));
                    g.DrawLine(gridPen, p3, p4);
                }
            }

            foreach (var ds in drawableSystems)
            {
                bool isCurrent = ds.System.Name.Equals(_currentSystem, StringComparison.InvariantCultureIgnoreCase);

                // --- Draw line to galactic plane ---
                // Use a less prominent color for the line
                g.DrawLine(_planeLinePen, ds.RotatedX, ds.RotatedY, ds.PlaneX, ds.PlaneY);

                // --- Determine Brush and Size ---
                Brush brush;
                if (isCurrent)
                {
                    brush = _currentSystemBrush;
                }
                else
                {
                    // Use a different color for systems below the galactic plane (Y < 0)
                    brush = ds.System.Y < 0 ? _systemBelowPlaneBrush : _systemBrush;
                }

                float dotSize = isCurrent ? 6f / _zoom : 3f / _zoom;

                // --- Draw the star dot ---
                g.FillEllipse(brush, ds.RotatedX - dotSize / 2, ds.RotatedY - dotSize / 2, dotSize, dotSize);

                // --- Draw labels if zoomed in enough or it's the current system ---
                if (_zoom > 1.0f || isCurrent)
                {
                    var labelSize = g.MeasureString(ds.System.Name, _labelFont);
                    var padding = new SizeF(4, 2);
                    var labelRect = new RectangleF(
                        ds.RotatedX + dotSize + 4,
                        ds.RotatedY - (labelSize.Height / 2f) - (padding.Height / 2f),
                        labelSize.Width + padding.Width,
                        labelSize.Height + padding.Height);

                    g.FillRectangle(_labelBackgroundBrush, labelRect);

                    var textLocation = new PointF(labelRect.X + (padding.Width / 2), labelRect.Y + (padding.Height / 2));

                    g.DrawString(ds.System.Name, _labelFont, brush, textLocation);
                }

                // --- Draw a circle around the current system ---
                if (isCurrent)
                {
                    float circleSize = 20f / _zoom;
                    g.DrawEllipse(_currentSystemPen, ds.RotatedX - circleSize / 2, ds.RotatedY - circleSize / 2, circleSize, circleSize);
                }
            }

        }

        // Helper to rotate a 2D point on the XZ plane for grid drawing
        private PointF RotatePoint(PointF point)
        {
            float cosY = (float)Math.Cos(_rotationY);
            float sinY = (float)Math.Sin(_rotationY);
            float cosX = (float)Math.Cos(_rotationX);
            float sinX = (float)Math.Sin(_rotationX);

            // Rotate around Y axis
            double tempX = point.X * cosY + point.Y * sinY;
            double tempZ = -point.X * sinY + point.Y * cosY;

            // Rotate around X axis (Y coordinate is 0 for the grid)
            double rotatedY = 0 * cosX - tempZ * sinX;

            return new PointF((float)tempX, (float)rotatedY);
        }
    }
}