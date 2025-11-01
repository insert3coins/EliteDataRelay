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
            ShipIcon,
            Exploration,
            JumpInfo
        }

        public event EventHandler<Point>? PositionChanged;
        private bool _isDragging;
        private Point _dragCursorStartPoint;
        private Point _dragFormStartPoint;

        private System.Windows.Forms.Timer? _animationTimer;
        private double _animationPhase;
        private const int ANIMATION_AMPLITUDE = 5; // How many pixels up/down it will move
        private const double ANIMATION_SPEED = 0.05; // How fast it will move

        private IEnumerable<CargoItem> _cargoItems = Enumerable.Empty<CargoItem>();

        // Bitmap caching for all overlays
        private Panel? _renderPanel;
        private Bitmap? _frameCache;
        private Bitmap? _shipIconBackgroundCache; // Separate background cache for ship icon animation
        private bool _stale = true;

        // Info overlay data
        private string _commanderName = "";
        private string _shipType = "";
        private long _balance = 0;

        // Cargo overlay data
        private int _cargoCount = 0;
        private int? _cargoCapacity = null;
        private string _cargoBarText = "";
        private long _sessionCargo = 0;
        private long _sessionCredits = 0;

        // Ship icon overlay data
        private Image? _shipIcon;

        // Exploration overlay data
        private SystemExplorationData? _currentExplorationData;
        private ExplorationSessionData? _currentSessionData;
        private SystemInfoData? _currentSystemInfo;

        private readonly bool _allowDrag;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        // Brushes are also IDisposable and should be cached for performance.
        private readonly SolidBrush _textBrush;
        private readonly SolidBrush _grayBrush;

        private readonly OverlayPosition _position;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                // Always use WS_EX_LAYERED for smooth transparency; browser overlays replace OBS window-capture needs.
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED

                // WS_EX_NOACTIVATE: Prevent the form from stealing focus
                // WS_EX_TRANSPARENT: Allow click-through (optional, currently disabled for dragging)
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                // Uncomment for click-through: cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

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
 
            // Set the form icon from embedded resources. This is more reliable than loading from a file.
            // The icon might be used by the OS in task switchers or other UI elements.
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // The resource name is formatted as <DefaultNamespace>.<FolderPath>.<FileName>
                using (var stream = assembly.GetManifestResourceStream("EliteDataRelay.Resources.Appicon.ico"))
                {
                    if (stream != null)
                    {
                        this.Icon = new Icon(stream);
                    }
                }
            }
            catch (Exception)
            {
                // Failed to load icon, continue without it.
            }

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
                case OverlayPosition.Exploration:
                    this.Text = "Elite Data Relay: Exploration";
                    break;
                case OverlayPosition.JumpInfo:
                    this.Text = "Elite Data Relay: Next Jump";
                    break;
                default:
                    this.Text = "Elite Data Relay Overlay";
                    break;
            }

            _labelFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize, FontStyle.Bold);
            _listFont = new Font(AppConfiguration.OverlayFontName, AppConfiguration.OverlayFontSize);

            // Create brushes once and reuse them in the Paint event to improve performance.
            _textBrush = new SolidBrush(AppConfiguration.OverlayTextColor);
            _grayBrush = new SolidBrush(SystemColors.GrayText);

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

            // Restore historical behavior: slider controls window opacity.
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
            // Base painting only; borders and backgrounds are drawn in panel renderers.
            base.OnPaint(e);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_renderPanel == null || _renderPanel.IsDisposed || _position != OverlayPosition.ShipIcon)
            {
                _animationTimer?.Stop();
                return;
            }

            // Only animate if we have a ship icon to display
            if (_shipIcon == null)
                return;

            // Calculate the vertical offset using a sine wave for smooth oscillation
            _animationPhase += ANIMATION_SPEED;

            // Trigger repaint for animation (ShipIcon overlay)
            _renderPanel?.Invalidate();
        }

         // Clean up any resources being used.
         // <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _labelFont?.Dispose();
                _listFont?.Dispose();
                _textBrush?.Dispose();
                _grayBrush?.Dispose();
                _animationTimer?.Dispose();
                _frameCache?.Dispose();
                _shipIconBackgroundCache?.Dispose();
                // Don't dispose _shipIcon - it's managed by the service layer
            }
            base.Dispose(disposing);
        }
    }
}
