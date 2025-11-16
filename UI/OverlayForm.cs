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
            Session,
            Exploration,
            Mining,
            Prospector,
            JumpInfo
        }

        public event EventHandler<Point>? PositionChanged;
        private bool _isDragging;
        private Point _dragCursorStartPoint;
        private Point _dragFormStartPoint;

        // Fade animation for Jump overlay
        private System.Windows.Forms.Timer? _fadeTimer;
        private double _fadeDelta;
        private bool _fadeHideOnComplete;
        private double _fadeTarget;
        private bool _suppressOpacityOnLoad;
        private bool _restoreOwnerOnHide;

        private IEnumerable<CargoItem> _cargoItems = Enumerable.Empty<CargoItem>();

        // Bitmap caching for all overlays
        private Panel? _renderPanel;
        private Bitmap? _frameCache;
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
        private TimeSpan _sessionDuration = TimeSpan.Zero;
        private TimeSpan _miningDuration = TimeSpan.Zero;
        private int _systemsVisited;

        // Exploration overlay data
        private SystemExplorationData? _currentExplorationData;
        private ExplorationSessionData? _currentSessionData;
        private SystemInfoData? _currentSystemInfo;
        private MiningOverlayData? _currentMiningData;
        private ProspectorOverlayData? _currentProspectorData;

        private readonly bool _allowDrag;

        // Fonts are IDisposable, so we should keep references to them to dispose of them later.
        private readonly Font _labelFont;
        private readonly Font _listFont;
        // Brushes are also IDisposable and should be cached for performance.
        private readonly SolidBrush _textBrush;
        private readonly SolidBrush _grayBrush;

        private readonly OverlayPosition _position;
        private readonly Form? _hostOwner;

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

        public OverlayForm(OverlayPosition position, bool allowDrag, Form? hostOwner = null)
        {
            _position = position;
            _allowDrag = allowDrag;
            _hostOwner = hostOwner;

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
                case OverlayPosition.Exploration:
                    this.Text = "Elite Data Relay: Exploration";
                    break;
                case OverlayPosition.Mining:
                    this.Text = "Elite Data Relay: Mining";
                    break;
                case OverlayPosition.Prospector:
                    this.Text = "Elite Data Relay: Prospector";
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

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Restore historical behavior: slider controls window opacity, unless we're doing a fade-in
            if (!_suppressOpacityOnLoad)
            {
                this.Opacity = AppConfiguration.OverlayOpacity / 100.0;
            }

        }

        // Public helpers to fade the Next Jump overlay in and out
        public void FadeIn(int durationMs = 200, bool allowAnyOverlay = false)
        {
            if (_position != OverlayPosition.JumpInfo && !allowAnyOverlay)
            {
                // Non-jump overlays: just show
                if (this.InvokeRequired) { this.BeginInvoke(new Action(() => this.Show())); } else { this.Show(); }
                return;
            }
            void DoFadeIn()
            {
                StopFade();
                double target = AppConfiguration.OverlayOpacity / 100.0;
                // If already visible and near target, don't replay fade
                if (this.Visible && Math.Abs(this.Opacity - target) < 0.02)
                {
                    return;
                }
                this.Opacity = 0.0;
                _suppressOpacityOnLoad = true;
                this.Show();
                _suppressOpacityOnLoad = false;
                _fadeHideOnComplete = false;
                StartFadeTo(target, durationMs);
            }
            if (this.InvokeRequired) this.BeginInvoke(new Action(DoFadeIn)); else DoFadeIn();
        }

        public void FadeOutAndHide(int durationMs = 200, bool allowAnyOverlay = false)
        {
            if (_position != OverlayPosition.JumpInfo && !allowAnyOverlay)
            {
                void HideNow()
                {
                    this.Hide();
                    RestoreOwnerZOrder();
                }
                if (this.InvokeRequired) { this.BeginInvoke(new Action(HideNow)); } else { HideNow(); }
                return;
            }
            void DoFadeOut()
            {
                StopFade();
                if (!this.Visible || this.Opacity <= 0.01)
                {
                    // Already hidden or fully transparent
                    this.Hide();
                    this.Opacity = AppConfiguration.OverlayOpacity / 100.0;
                    RestoreOwnerZOrder();
                    return;
                }
                _fadeHideOnComplete = true;
                 _restoreOwnerOnHide = true;
                StartFadeTo(0.0, durationMs);
            }
            if (this.InvokeRequired) this.BeginInvoke(new Action(DoFadeOut)); else DoFadeOut();
        }

        private void StartFadeTo(double targetOpacity, int durationMs)
        {
            double current = this.Opacity;
            int interval = 16; // ~60fps
            int steps = Math.Max(1, durationMs / interval);
            _fadeTarget = targetOpacity;
            _fadeDelta = (targetOpacity - current) / steps;
            _fadeTimer = new System.Windows.Forms.Timer { Interval = interval };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                double next = this.Opacity + _fadeDelta;
                // Clamp when crossing target
                if ((_fadeDelta > 0 && next >= _fadeTarget) || (_fadeDelta < 0 && next <= _fadeTarget))
                {
                    next = Math.Max(0.0, Math.Min(1.0, _fadeTarget));
                }
                this.Opacity = next;

                bool done = Math.Abs(this.Opacity - _fadeTarget) < 0.002;
                if (done)
                {
                    StopFade();
                    if (_fadeHideOnComplete)
                    {
                        this.Hide();
                        // Reset to configured opacity for next show
                        this.Opacity = AppConfiguration.OverlayOpacity / 100.0;
                        if (_restoreOwnerOnHide)
                        {
                            RestoreOwnerZOrder();
                            _restoreOwnerOnHide = false;
                        }
                    }
                }
            }
            catch
            {
                StopFade();
            }
        }

        private void StopFade()
        {
            if (_fadeTimer != null)
            {
                try { _fadeTimer.Stop(); } catch { }
                try { _fadeTimer.Dispose(); } catch { }
                _fadeTimer = null;
            }
        }

        private void RestoreOwnerZOrder()
        {
            if (_hostOwner == null) return;
            void BringOwner()
            {
                try
                {
                    if (_hostOwner.WindowState == FormWindowState.Minimized)
                    {
                        _hostOwner.WindowState = FormWindowState.Normal;
                    }
                    _hostOwner.Activate();
                    bool prevTopMost = _hostOwner.TopMost;
                    _hostOwner.TopMost = true;
                    _hostOwner.TopMost = prevTopMost;
                }
                catch
                {
                    // ignore
                }
            }

            if (_hostOwner.InvokeRequired)
            {
                _hostOwner.BeginInvoke(new Action(BringOwner));
            }
            else
            {
                BringOwner();
            }
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
                _fadeTimer?.Dispose();
                _frameCache?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Hotkeys removed; no special message handling required.
    }
}
