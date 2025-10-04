using System;
using System.Collections.Generic;
using EliteDataRelay.Configuration;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteDataRelay.Models;
using EliteDataRelay.Services;

namespace EliteDataRelay.UI
{
    public partial class OverlayForm : Form
    {
        public enum OverlayPosition
        {
            Info,
            Cargo,
            ShipIcon
        }

        public event EventHandler<Point>? PositionChanged;
        private bool _isDragging;
        private Point _dragCursorStartPoint;
        private Point _dragFormStartPoint;

        private Label _cmdrValueLabel = null!;
        private Label _shipValueLabel = null!;
        private Label _balanceValueLabel = null!;
        private Label _cargoHeaderLabel = null!;
        private Label _sessionCargoValueLabel = null!;
        private Label _sessionCreditsValueLabel = null!;
        private Panel _cargoListPanel = null!;
        private Label _cargoSizeLabel = null!;
        private Label _cargoBarLabel = null!;
        private PictureBox _shipIconPictureBox = null!;
        private System.Windows.Forms.Timer? _animationTimer;
        private double _animationPhase;
        private const int ANIMATION_AMPLITUDE = 5; // How many pixels up/down it will move
        private const double ANIMATION_SPEED = 0.05; // How fast it will move

        private IEnumerable<CargoItem> _cargoItems = Enumerable.Empty<CargoItem>();

        private readonly bool _allowDrag;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        private readonly OverlayPosition _position;

        public OverlayForm(OverlayPosition position, bool allowDrag)
        {
            _position = position;
            _allowDrag = allowDrag;

            // Enable double buffering to reduce flicker and rendering artifacts. This is a standard
            // technique to prevent visual glitches like stray lines or bars on transparent forms.
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;

            // Apply appearance settings from configuration for semi-transparent background.
            // The BackColor property does not support an alpha channel. We set an opaque color and use the form's Opacity property for transparency.
            this.BackColor = Color.FromArgb(255, AppConfiguration.OverlayBackgroundColor);

            switch (_position)
            {
                case OverlayPosition.Info:
                    this.Text = "Elite Data Relay: Info";
                    break;
                case OverlayPosition.Cargo:
                    this.Text = "Elite Data Relay: Cargo";
                    break;
                case OverlayPosition.ShipIcon:
                    this.Text = "Elite Data Relay: Ship Icon";
                    break;
                default:
                    this.Text = "Elite Data Relay Overlay";
                    break;
            }

            _labelFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _listFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);

            InitializeControls();

            // Wire up dragging for the form and all its children, recursively.
            AttachDragHandlers(this);

            if (_position == OverlayPosition.ShipIcon)
            {
                _animationTimer = new System.Windows.Forms.Timer { Interval = 30 }; // Approx 33 FPS
                _animationTimer.Tick += AnimationTimer_Tick;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Set the form's opacity to achieve a semi-transparent effect.
            this.Opacity = AppConfiguration.OverlayOpacity / 100.0;

            _animationTimer?.Start();
        }

        private void AttachDragHandlers(Control control)
        {
            control.MouseDown += OnOverlayMouseDown;
            control.MouseMove += OnOverlayMouseMove;
            control.MouseUp += OnOverlayMouseUp;
            foreach (Control child in control.Controls)
            {
                AttachDragHandlers(child);
            }
        }

        private void OnOverlayMouseDown(object? sender, MouseEventArgs e)
        {
            if (_allowDrag && e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragCursorStartPoint = Cursor.Position;
                _dragFormStartPoint = this.Location;
                this.Cursor = Cursors.SizeAll;
            }
        }

        private void OnOverlayMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentCursorPos = Cursor.Position;
                int deltaX = currentCursorPos.X - _dragCursorStartPoint.X;
                int deltaY = currentCursorPos.Y - _dragCursorStartPoint.Y;
                this.Location = new Point(_dragFormStartPoint.X + deltaX, _dragFormStartPoint.Y + deltaY);
            }
        }

        private void OnOverlayMouseUp(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.Cursor = Cursors.Default;
                // Raise the event to notify the service to save the new position.
                PositionChanged?.Invoke(this, this.Location);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw a standard border for all overlays
            using (var pen = new Pen(Color.FromArgb(100, 100, 100)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_shipIconPictureBox == null || _shipIconPictureBox.IsDisposed)
            {
                _animationTimer?.Stop();
                return;
            }

            // Calculate the vertical offset using a sine wave for smooth oscillation
            _animationPhase += ANIMATION_SPEED;
            int offsetY = (int)(Math.Sin(_animationPhase) * ANIMATION_AMPLITUDE);
            _shipIconPictureBox.Top = (this.ClientSize.Height - _shipIconPictureBox.Height) / 2 + offsetY;
        }

         // Clean up any resources being used.
         // <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _labelFont?.Dispose();
                _listFont?.Dispose();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}