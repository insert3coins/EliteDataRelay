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
        private readonly Timer _pulseTimer;
        private readonly Panel _infoCardPanel;
        private readonly Panel _systemInfoCard;
        private readonly Font _infoTitleFont;
        private bool _pulseState;

        public StarMapPanel()
        {
            _labelFont = new Font("Consolas", 9f);
            _backgroundBrushes = new Brush[]
            {
                new SolidBrush(Color.FromArgb(60, 60, 60)),
                new SolidBrush(Color.FromArgb(100, 100, 100)),
                new SolidBrush(Color.FromArgb(140, 140, 140)),
            };
            GenerateBackgroundStars(5000);
            _infoTitleFont = new Font("Consolas", 10f, FontStyle.Bold);
            DoubleBuffered = true;
            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;

            MouseDown += OnMapMouseDown;
            MouseUp += OnMapMouseUp;
            MouseMove += OnMapMouseMove;
            MouseWheel += OnMapMouseWheel;
            MouseLeave += OnMapMouseLeave;
            _pulseTimer = new Timer { Interval = 500 };
            _pulseTimer.Tick += OnPulseTimerTick;
            this.Resize += (s, e) => this.Invalidate(); // Redraw on resize

            _infoCardPanel = CreateControlsInfoCard();
            this.Controls.Add(_infoCardPanel);
            _infoCardPanel.BringToFront();

            _systemInfoCard = CreateSystemInfoCard();
            this.Controls.Add(_systemInfoCard);
            _systemInfoCard.BringToFront();

            this.Resize += (s, e) => {
                _infoCardPanel.Location = new Point(this.Width - _infoCardPanel.Width - 10, 10);
            };
            // Set initial position after the panel has been added and sized.
            _infoCardPanel.Location = new Point(this.Width - _infoCardPanel.Width - 10, 10);
        }

        private Panel CreateControlsInfoCard()
        {
            var infoPanel = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(180, 20, 20, 20),
                Padding = new Padding(10),
            };

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = 1,
            };

            var titleLabel = new Label { Text = "Map Controls", Font = _infoTitleFont, ForeColor = Color.White, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            var panLabel = new Label { Text = "• Pan: Left-click + Drag", Font = _labelFont, ForeColor = Color.LightGray, AutoSize = true };
            var rotateLabel = new Label { Text = "• Rotate: Right-click + Drag", Font = _labelFont, ForeColor = Color.LightGray, AutoSize = true };
            var zoomLabel = new Label { Text = "• Zoom: Mouse Wheel", Font = _labelFont, ForeColor = Color.LightGray, AutoSize = true };

            layout.Controls.Add(titleLabel);
            layout.Controls.Add(panLabel);
            layout.Controls.Add(rotateLabel);
            layout.Controls.Add(zoomLabel);

            infoPanel.Controls.Add(layout);
            return infoPanel;
        }

        private Panel CreateSystemInfoCard()
        {
            var infoPanel = new Panel
            {
                Visible = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(220, 10, 10, 10),
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                MaximumSize = new Size(400, 500), // Constrain the size
                AutoScroll = true, // Add a scrollbar if content overflows
            };
            return infoPanel;
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
                _infoCardPanel?.Dispose();
                _systemInfoCard?.Dispose();
                _infoTitleFont?.Dispose();
                _pulseTimer.Dispose();
                _labelFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}