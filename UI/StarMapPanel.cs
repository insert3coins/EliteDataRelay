using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using EliteDataRelay.Models;

namespace EliteDataRelay.UI
{
    public partial class StarMapPanel : Panel
    {
        private List<StarSystem> _systems = new List<StarSystem>();
        private string _currentSystem = string.Empty;
        private string? _searchedSystem;
        private float _zoom = 0.1f;
        private PointF _panOffset = PointF.Empty;
        private Point _lastMousePosition;

        private class BackgroundStar
        {
            public float X, Y, Z;
            public Brush Brush = null!;
        }
        private readonly List<BackgroundStar> _backgroundStars = new List<BackgroundStar>();
        private readonly Brush[] _backgroundBrushes;

        private readonly Brush _systemBrush = new SolidBrush(Color.White);
        private readonly Brush _systemBelowPlaneBrush = new SolidBrush(Color.LightGray);
        private readonly Brush _currentSystemBrush = new SolidBrush(Color.Cyan);
        private readonly Brush _searchedSystemBrush = new SolidBrush(Color.Yellow);
        private readonly Pen _currentSystemPen = new Pen(Color.Cyan, 2);
        private readonly Pen _planeLinePen = new Pen(Color.FromArgb(50, 255, 255, 255), 1);
        private readonly Font _labelFont;
        private readonly Brush _labelBackgroundBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        private readonly ToolTip _toolTip;
        private readonly Timer _pulseTimer;
        private bool _pulseState;

        public StarMapPanel()
        {
            _labelFont = new Font("Consolas", 7.5f);
            _backgroundBrushes = new Brush[]
            {
                new SolidBrush(Color.FromArgb(60, 60, 60)),
                new SolidBrush(Color.FromArgb(100, 100, 100)),
                new SolidBrush(Color.FromArgb(140, 140, 140)),
            };
            GenerateBackgroundStars(5000);
            DoubleBuffered = true;
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;

            MouseDown += OnMapMouseDown;
            MouseUp += OnMapMouseUp;
            MouseMove += OnMapMouseMove;
            MouseWheel += OnMapMouseWheel;
            MouseLeave += OnMapMouseLeave;
            _toolTip = new ToolTip();
            _pulseTimer = new Timer { Interval = 500 };
            _pulseTimer.Tick += OnPulseTimerTick;
            this.Resize += (s, e) => this.Invalidate(); // Redraw on resize
        }

        public void SetSystems(IReadOnlyList<StarSystem> systems, string currentSystem)
        {
            _systems = systems.ToList();
            _currentSystem = currentSystem;
            Invalidate();
        }

        public void ResetView()
        {
            _zoom = 0.1f;
            _panOffset = new PointF(this.Width / 2f, this.Height / 2f);
            _rotationX = 0.5f;
            _rotationY = 0.0f;
            Invalidate();
        }

        public void HighlightSystem(string? systemName)
        {
            _searchedSystem = systemName;
            if (!string.IsNullOrEmpty(_searchedSystem))
            {
                _pulseState = true; // Ensure it starts in the 'on' state
                _pulseTimer.Start();
            }
            else
            {
                _pulseTimer.Stop();
            }
            Invalidate();
        }

        private void GenerateBackgroundStars(int count)
        {
            var rand = new Random();
            // A large cube around the bubble, centered on Sol (0,0,0)
            int range = 40000;

            for (int i = 0; i < count; i++)
            {
                _backgroundStars.Add(new BackgroundStar
                {
                    X = (float)(rand.NextDouble() * 2 - 1) * range,
                    Y = (float)(rand.NextDouble() * 2 - 1) * range,
                    Z = (float)(rand.NextDouble() * 2 - 1) * range,
                    Brush = _backgroundBrushes[rand.Next(_backgroundBrushes.Length)]
                });
            }
        }

        private void OnPulseTimerTick(object? sender, EventArgs e)
        {
            _pulseState = !_pulseState;
            Invalidate(); // Trigger a repaint to show the new pulse state
        }

        public void SetAndCenterOnSystem(string systemName)
        {
            _currentSystem = systemName;
            CenterOnSystemInternal(systemName);
            Invalidate();
        }

        public void CenterOnSystem(string systemName)
        {
            CenterOnSystemInternal(systemName);
            Invalidate();
        }

        private void CenterOnSystemInternal(string systemName)
        {
            if (string.IsNullOrEmpty(systemName) || !_systems.Any()) return;

            var systemToCenter = _systems.FirstOrDefault(s => s.Name.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));
            if (systemToCenter == null) return;

            // Set a default zoom level to make the centered system clearly visible.
            _zoom = 1.5f;

            // We want the system's rotated coordinates to be at the center of the panel.
            // The panel's center is (Width / 2, Height / 2).
            // The final screen position of a point is (rotatedX * zoom + panX, rotatedY * zoom + panY).
            // So, we solve for panX and panY:
            // panX = panelCenterX - rotatedX * zoom
            // panY = panelCenterY - rotatedY * zoom

            // First, we need to get the rotated coordinates of the target system.
            float cosX = (float)Math.Cos(_rotationX);
            float sinX = (float)Math.Sin(_rotationX);
            float cosY = (float)Math.Cos(_rotationY);
            float sinY = (float)Math.Sin(_rotationY);

            double tempX_star = systemToCenter.X * cosY + systemToCenter.Z * sinY;
            double tempZ_star = -systemToCenter.X * sinY + systemToCenter.Z * cosY;
            double rotatedY_star = systemToCenter.Y * cosX - tempZ_star * sinX;

            float rotatedX = (float)tempX_star;
            float rotatedY = (float)rotatedY_star;

            float panelCenterX = this.Width / 2f;
            float panelCenterY = this.Height / 2f;

            _panOffset = new PointF(panelCenterX - (rotatedX * _zoom), panelCenterY - (rotatedY * _zoom));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _systemBrush.Dispose();
                _currentSystemBrush.Dispose();
                _currentSystemPen.Dispose();
                _planeLinePen.Dispose();
                _systemBelowPlaneBrush.Dispose();
                _searchedSystemBrush.Dispose();
                foreach (var brush in _backgroundBrushes)
                {
                    brush.Dispose();
                }
                _labelBackgroundBrush.Dispose();
                _pulseTimer.Dispose();
                _toolTip.Dispose();
                _labelFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}