using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.UI
{
    /// <summary>
    /// UI management class for the cargo form interface
    /// </summary>
    public class CargoFormUI : ICargoFormUI
    {
        private TextBox? _textBox;
        private Button? _startBtn;
        private Button? _stopBtn;
        private Button? _exitBtn;
        private Button? _aboutBtn;
        private Button? _settingsBtn;
        private Font? _verdanaFont;
        private Font? _consolasFont;
        private PrivateFontCollection? _privateFonts;
        private IntPtr _fontMemoryPtr = IntPtr.Zero;
        private Form? _form;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _trayMenu;
        private ToolStripMenuItem? _trayMenuShow;
        private ToolStripMenuItem? _trayMenuStart;
        private ToolStripMenuItem? _trayMenuStop;
        private ToolStripMenuItem? _trayMenuExit;

        // Animation fields
        private System.Windows.Forms.Timer? _animationTimer;
        private string _baseTitle = "";
        private int _animationFrame = 0;
        private bool _animationForward = true; // To control direction
        private const string Ship = ">-=>"; // Ship design for forward travel
        private const string ReversedShip = "<=-<"; // Ship design for reverse travel
        private int _animationWidth = 30; // Width of the animation area in characters

        /// <summary>
        /// Event raised when the start button is clicked
        /// </summary>
        public event EventHandler? StartClicked;

        /// <summary>
        /// Event raised when the stop button is clicked  
        /// </summary>
        public event EventHandler? StopClicked;

        /// <summary>
        /// Event raised when the exit button is clicked
        /// </summary>
        public event EventHandler? ExitClicked;

        /// <summary>
        /// Event raised when the about button is clicked
        /// </summary>
        public event EventHandler? AboutClicked;

        /// <summary>
        /// Event raised when the settings button is clicked
        /// </summary>
        public event EventHandler? SettingsClicked;

        /// <summary>
        /// Initialize the UI components and layout
        /// </summary>
        /// <param name="form">The main form to initialize</param>
        public void InitializeUI(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _form.Resize += OnFormResize;

            InitializeFonts();
            InitializeAnimationTimer();
            CreateControls();
            SetupFormProperties();
            SetupLayout();
            SetupEventHandlers();
            DisplayWelcomeMessage();
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            if (_form?.WindowState == FormWindowState.Minimized)
            {
                _form.Hide();
                _notifyIcon?.ShowBalloonTip(1000, "Elite Cargo Monitor", "Minimized to tray.", ToolTipIcon.Info);
            }
            else if (_form?.WindowState == FormWindowState.Normal || _form?.WindowState == FormWindowState.Maximized)
            {
                // Recalculate animation width when window is resized
                UpdateAnimationWidth();
            }
        }

        private void InitializeFonts()
        {
            try
            {
                // Initialize Verdana font from embedded resources.
                byte[] fontData = Properties.Resources.VerdanaFont;

                // Allocate unmanaged memory and copy font data. This memory must not be freed until the PrivateFontCollection is disposed.
                _fontMemoryPtr = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, _fontMemoryPtr, fontData.Length);

                _privateFonts = new PrivateFontCollection();
                _privateFonts.AddMemoryFont(_fontMemoryPtr, fontData.Length);

                _verdanaFont = new Font(_privateFonts.Families[0], AppConfiguration.DefaultFontSize);
            }
            catch
            {
                // Fallback to default font if custom font fails
                _verdanaFont = new Font(FontFamily.GenericSansSerif, AppConfiguration.DefaultFontSize);
                _privateFonts?.Dispose();
            }

            // Initialize Consolas font from system
            try
            {
                _consolasFont = new Font(AppConfiguration.ConsolasFontName, AppConfiguration.DefaultFontSize);
            }
            catch
            {
                _consolasFont = new Font(FontFamily.GenericMonospace, AppConfiguration.DefaultFontSize);
            }
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new System.Windows.Forms.Timer { Interval = 150 }; // Animation speed
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_form == null || string.IsNullOrEmpty(_baseTitle)) return;

            // Choose ship design and length based on direction
            string currentShip = _animationForward ? Ship : ReversedShip;
            int shipLength = currentShip.Length;

            // Total width for the animation cycle (ship starts off-screen, moves across, and exits)
            int totalCycleWidth = _animationWidth + shipLength;

            // Current position of the ship's first character.
            // This calculation remains the same for both directions.
            int shipPosition = _animationFrame - shipLength;

            var frameChars = new char[_animationWidth];
            // Draw the part of the ship that is visible in the frame
            for (int i = 0; i < _animationWidth; i++)
            {
                int shipCharIndex = i - shipPosition;
                if (shipCharIndex >= 0 && shipCharIndex < shipLength)
                {
                    frameChars[i] = currentShip[shipCharIndex];
                }
                else
                {
                    frameChars[i] = ' ';
                }
            }

            // Update frame and direction for ping-pong effect
            if (_animationForward)
            {
                _animationFrame++;
                if (_animationFrame >= totalCycleWidth)
                {
                    _animationForward = false;
                    _animationFrame = totalCycleWidth - 1; // Start moving back from the end
                }
            }
            else // Moving backward
            {
                _animationFrame--;
                if (_animationFrame <= 0)
                {
                    _animationForward = true;
                    _animationFrame = 1; // Start moving forward from the beginning
                }
            }

            _form.Text = $"{_baseTitle} [{new string(frameChars)}]";
        }

        private void UpdateAnimationWidth()
        {
            if (_form?.IsDisposed != false || _form.Handle == IntPtr.Zero) return;

            // To calculate the available animation width, we subtract the width of all other title bar elements
            // from the total width of the form.

            // A constant to estimate the total width of the right-side window chrome (minimize, maximize, close buttons, and their padding).
            // This value is an approximation, as the exact width can vary based on Windows version, themes, and DPI settings.
            // There is no direct .NET API to get this value perfectly. This value has been adjusted
            // to be more conservative and provide more space for the animation on most systems.
            const int RightSideChromeWidth = 115;
            int iconWidth = _form.ShowIcon ? SystemInformation.SmallIconSize.Width : 0;
            int frameBorderWidth = _form.Width - _form.ClientSize.Width; // Get the exact total border width.

            // Measure the width of the static part of the title.
            Size titleSize = TextRenderer.MeasureText(_baseTitle + " []", SystemFonts.CaptionFont);

            // Calculate the remaining width available for the animation.
            int availableWidth = _form.Width - titleSize.Width - RightSideChromeWidth - iconWidth - frameBorderWidth;

            int newWidth = _animationWidth;
            if (availableWidth > 0)
            {
                // Estimate character width using a space character in the caption font.
                int charWidth = TextRenderer.MeasureText(" ", SystemFonts.CaptionFont).Width;
                if (charWidth > 0)
                {
                    newWidth = availableWidth / charWidth;
                }
            }

            // Ensure a minimum width for the animation.
            if (newWidth < 10)
            {
                newWidth = 10;
            }

            if (newWidth != _animationWidth)
            {
                _animationWidth = newWidth;
                _animationFrame = 0; // Reset animation on resize to prevent graphical glitches.
                _animationForward = true;
            }
        }

        private void CreateControls()
        {
            // Create main text display
            _textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = _consolasFont,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Margin = Padding.Empty
            };

            // Set Verdana font for text box if available
            if (_verdanaFont != null)
            {
                _textBox.Font = _verdanaFont;
            }

            // Create control buttons
            _startBtn = new Button { Text = "Start", Height = AppConfiguration.ButtonHeight, Font = _consolasFont };
            _stopBtn = new Button { Text = "Stop", Height = AppConfiguration.ButtonHeight, Enabled = false, Font = _consolasFont };
            _exitBtn = new Button { Text = "Exit", Height = AppConfiguration.ButtonHeight, Font = _consolasFont };
            _settingsBtn = new Button { Text = "Settings", Height = AppConfiguration.ButtonHeight, Font = _consolasFont };
            _aboutBtn = new Button { Text = "About", Height = AppConfiguration.ButtonHeight, Font = _consolasFont };

            CreateTrayIcon();
        }

        private void SetupFormProperties()
        {
            if (_form == null) return;

            // Basic form properties
            _form.Text = "Cargo Monitor – Stopped";
            _form.Width = AppConfiguration.FormWidth;
            _form.Height = AppConfiguration.FormHeight;
            _form.Padding = Padding.Empty;
            _baseTitle = _form.Text;

            // Set application icon
            try
            {
                _form.Icon = new Icon(new MemoryStream(Properties.Resources.AppIcon));
            }
            catch
            {
                // Ignore icon errors - form will use default icon
            }
        }

        private void SetupLayout()
        {
            if (_form == null || _textBox == null || _startBtn == null || 
                _stopBtn == null || _aboutBtn == null || _settingsBtn == null || _exitBtn == null) return;

            // Create button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = AppConfiguration.ButtonPanelHeight,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            // Add buttons in order: Start | Stop | About | Settings | Exit
            buttonPanel.Controls.Add(_startBtn);
            buttonPanel.Controls.Add(_stopBtn);
            buttonPanel.Controls.Add(_aboutBtn);
            buttonPanel.Controls.Add(_settingsBtn);
            buttonPanel.Controls.Add(_exitBtn);

            // Add controls to form
            _form.Controls.Add(_textBox);
            _form.Controls.Add(buttonPanel);
        }

        private void SetupEventHandlers()
        {
            if (_startBtn != null) _startBtn.Click += (s, e) => StartClicked?.Invoke(s, e);
            if (_stopBtn != null) _stopBtn.Click += (s, e) => StopClicked?.Invoke(s, e);
            if (_exitBtn != null) _exitBtn.Click += (s, e) => ExitClicked?.Invoke(s, e);
            if (_settingsBtn != null) _settingsBtn.Click += (s, e) => SettingsClicked?.Invoke(s, e);
            if (_aboutBtn != null) _aboutBtn.Click += (s, e) => AboutClicked?.Invoke(s, e);

            // Tray icon event handlers
            if (_notifyIcon != null) _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
            if (_trayMenuShow != null) _trayMenuShow.Click += OnTrayMenuShowClick;
            if (_trayMenuStart != null) _trayMenuStart.Click += (s, e) => StartClicked?.Invoke(s, e);
            if (_trayMenuStop != null) _trayMenuStop.Click += (s, e) => StopClicked?.Invoke(s, e);
            if (_trayMenuExit != null) _trayMenuExit.Click += (s, e) => ExitClicked?.Invoke(s, e);
        }

        private void DisplayWelcomeMessage()
        {
            AppendText(AppConfiguration.WelcomeMessage + Environment.NewLine);
        }

        /// <summary>
        /// Update the UI with new cargo data
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to display</param>
        /// <param name="cargoCapacity">The total cargo capacity</param>
        public void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity)
        {
            // This method is now obsolete as formatting is handled by FileOutputService
            // and text is appended directly in CargoForm.
            // We keep the method to satisfy the interface but it does nothing.
            TrimTextBoxLines();
            ScrollToBottom();
        }

        /// <summary>
        /// Append text to the display
        /// </summary>
        /// <param name="text">Text to append</param>
        public void AppendText(string text)
        {
            if (_textBox == null) return;

            try
            {
                _textBox.AppendText(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error appending text: {ex}");
            }
        }

        /// <summary>
        /// Update the form title
        /// </summary>
        /// <param name="title">New title text</param>
        public void UpdateTitle(string title)
        {
            _baseTitle = title;

            if (_form != null)
            {
                _form.Text = _baseTitle;
            }

            // Start/stop animation based on monitoring state
            if (title.Contains("Watching") && _animationTimer?.Enabled == false)
            {
                UpdateAnimationWidth(); // Calculate width before starting
                _animationFrame = 0;
                _animationForward = true; // Ensure animation starts by moving forward
                _animationTimer.Start();
            }
            else if (!title.Contains("Watching") && _animationTimer?.Enabled == true)
            {
                _animationTimer.Stop();
                // Restore the title without animation
                if (_form != null)
                    _form.Text = _baseTitle;
            }
        }

        /// <summary>
        /// Set the enabled state of the start and stop buttons
        /// </summary>
        /// <param name="startEnabled">Whether start button should be enabled</param>
        /// <param name="stopEnabled">Whether stop button should be enabled</param>
        public void SetButtonStates(bool startEnabled, bool stopEnabled)
        {
            if (_startBtn != null) _startBtn.Enabled = startEnabled;
            if (_stopBtn != null) _stopBtn.Enabled = stopEnabled;

            // Also update tray menu items
            if (_trayMenuStart != null) _trayMenuStart.Enabled = startEnabled;
            if (_trayMenuStop != null) _trayMenuStop.Enabled = stopEnabled;
        }

        private void TrimTextBoxLines()
        {
            if (_textBox?.Lines == null) return;

            try
            {
                string[] lines = _textBox.Lines;
                if (lines.Length <= AppConfiguration.MaxTextBoxLines) return;

                _textBox.Lines = lines.Skip(lines.Length - AppConfiguration.MaxTextBoxLines).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error trimming text box lines: {ex}");
            }
        }

        private void ScrollToBottom()
        {
            if (_textBox == null) return;

            try
            {
                _textBox.SelectionStart = _textBox.TextLength;
                _textBox.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error scrolling to bottom: {ex}");
            }
        }

        private void CreateTrayIcon()
        {
            _trayMenuShow = new ToolStripMenuItem("Show");
            _trayMenuStart = new ToolStripMenuItem("Start");
            _trayMenuStop = new ToolStripMenuItem("Stop") { Enabled = false };
            _trayMenuExit = new ToolStripMenuItem("Exit");

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add(_trayMenuShow);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuStart);
            _trayMenu.Items.Add(_trayMenuStop);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuExit);

            _notifyIcon = new NotifyIcon
            {
                Text = "Elite Cargo Monitor",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };

            try
            {
                _notifyIcon.Icon = new Icon(new MemoryStream(Properties.Resources.AppIcon));
            }
            catch
            {
                // Ignore icon errors
            }
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e) => ShowForm();

        private void OnTrayMenuShowClick(object? sender, EventArgs e) => ShowForm();

        private void ShowForm()
        {
            if (_form == null || _notifyIcon == null) return;

            _form.Show();
            _form.WindowState = FormWindowState.Normal;
            _form.Activate();
        }

        public void Dispose()
        {
            _verdanaFont?.Dispose();
            _consolasFont?.Dispose();
            _textBox?.Dispose();
            _startBtn?.Dispose();
            _stopBtn?.Dispose();
            _exitBtn?.Dispose();
            _aboutBtn?.Dispose();
            _settingsBtn?.Dispose();
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
            _animationTimer?.Dispose();
            _privateFonts?.Dispose();
        }
    }
}