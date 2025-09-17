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
        private ToolTip? _toolTip;
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
        private Icon? _appIcon;
        private Label? _cargoSizeLabel;
        private MemoryStream? _iconStream;
        private Label? _watchingLabel;
        private System.Windows.Forms.Timer? _watchingTimer;
        private int _watchingFrame = 0;
        private string _currentLocation = "Unknown";

        private string _baseTitle = "";
        private readonly Random _random = new Random();

        // Our working when we hit start
        private static readonly string[] WatchingCargo = new[]
		{
			"⢀⠀",
			"⡀⠀",
			"⠄⠀",
			"⢂⠀",
			"⡂⠀",
			"⠅⠀",
			"⢃⠀",
			"⡃⠀",
			"⠍⠀",
			"⢋⠀",
			"⡋⠀",
			"⠍⠁",
			"⢋⠁",
			"⡋⠁",
			"⠍⠉",
			"⠋⠉",
			"⠋⠉",
			"⠉⠙",
			"⠉⠙",
			"⠉⠩",
			"⠈⢙",
			"⠈⡙",
			"⢈⠩",
			"⡀⢙",
			"⠄⡙",
			"⢂⠩",
			"⡂⢘",
			"⠅⡘",
			"⢃⠨",
			"⡃⢐",
			"⠍⡐",
			"⢋⠠",
			"⡋⢀",
			"⠍⡁",
			"⢋⠁",
			"⡋⠁",
			"⠍⠉",
			"⠋⠉",
			"⠋⠉",
			"⠉⠙",
			"⠉⠙",
			"⠉⠩",
			"⠈⢙",
			"⠈⡙",
			"⠈⠩",
			"⠀⢙",
			"⠀⡙",
			"⠀⠩",
			"⠀⢘",
			"⠀⡘",
			"⠀⠨",
			"⠀⢐",
			"⠀⡐",
			"⠀⠠",
			"⠀⢀",
			"⠀⡀"
		};
        // Cargo storage sizes for bottom right of our ui
        private static readonly string[] CargoSize = new[]
        {
            "▱▱▱▱▱▱▱▱▱▱",
            "▰▱▱▱▱▱▱▱▱▱",
            "▰▰▱▱▱▱▱▱▱▱",
            "▰▰▰▱▱▱▱▱▱▱",
            "▰▰▰▰▱▱▱▱▱▱",
            "▰▰▰▰▰▱▱▱▱▱",
            "▰▰▰▰▰▰▱▱▱▱",
            "▰▰▰▰▰▰▰▱▱▱",
            "▰▰▰▰▰▰▰▰▱▱",
            "▰▰▰▰▰▰▰▰▰▱",
            "▰▰▰▰▰▰▰▰▰▰",
        };

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

            InitializeIcon();
            InitializeFonts();
            InitializeAnimation();
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
            }
        }

        private void InitializeIcon()
        {
            try
            {
                // Create a MemoryStream from the icon resource. This stream must be kept open
                // for the lifetime of the Icon object. We store it in a field and dispose of
                // it when the UI is disposed. This prevents heap corruption (0xc0000374) that
                // can occur if the stream is garbage collected while the Icon is still in use.
                _iconStream = new MemoryStream(Properties.Resources.AppIcon);
                _appIcon = new Icon(_iconStream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error initializing application icon: {ex}");
                // If icon fails to load, _appIcon will remain null, and the form/tray will use defaults.
            }
        }

        private void InitializeAnimation()
        {
            _watchingTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _watchingTimer.Tick += WatchingTimer_Tick;
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

            // Create ToolTip and assign to buttons
            _toolTip = new ToolTip();
            _toolTip.SetToolTip(_startBtn, "Start monitoring for cargo changes");
            _toolTip.SetToolTip(_stopBtn, "Stop monitoring for cargo changes");
            _toolTip.SetToolTip(_exitBtn, "Exit the application");
            _toolTip.SetToolTip(_settingsBtn, "Configure application settings");
            _toolTip.SetToolTip(_aboutBtn, "Show information about the application");

            _cargoSizeLabel = new Label
            {
                Text = $"{CargoSize[0]}",
                Font = _consolasFont,
                Anchor = AnchorStyles.Right,
                AutoSize = true,
            };

            // Calculate a fixed width for the animation label to ensure consistent layout.
            // Using "WW" as a reference for two wide characters in a monospaced font.
            var animationWidth = TextRenderer.MeasureText("WW", _consolasFont).Width;

            _watchingLabel = new Label
            {
                Text = "",
                Font = _consolasFont,
                AutoSize = false, // Disable AutoSize to manually control alignment
                TextAlign = ContentAlignment.MiddleCenter, // Center the animation character vertically and horizontally
                Height = AppConfiguration.ButtonHeight, // Match the height of the buttons
                Width = animationWidth,
                Margin = new Padding(3) // Use default button margins for consistent spacing
            };

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
            UpdateFullTitleText();

            // Set application icon from pre-loaded resource
            if (_appIcon != null) _form.Icon = _appIcon;
        }
        private void SetupLayout()
        {
            if (_form == null || _textBox == null || _startBtn == null || 
                _stopBtn == null || _aboutBtn == null || _settingsBtn == null || _exitBtn == null || _watchingLabel == null ||
                _cargoSizeLabel == null) return;

            // Create a FlowLayoutPanel for the buttons
            var buttonFlowPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };

            // Add buttons in order: Spinner | Start | Stop | About | Settings | Exit
            buttonFlowPanel.Controls.Add(_watchingLabel);
            buttonFlowPanel.Controls.Add(_startBtn);
            buttonFlowPanel.Controls.Add(_stopBtn);
            buttonFlowPanel.Controls.Add(_aboutBtn);
            buttonFlowPanel.Controls.Add(_settingsBtn);
            buttonFlowPanel.Controls.Add(_exitBtn);

            // Use a TableLayoutPanel to hold the buttons at the bottom, which helps with vertical alignment.
            var bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = AppConfiguration.ButtonPanelHeight,
                ColumnCount = 2,
                RowCount = 1,
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomPanel.Controls.Add(buttonFlowPanel, 0, 0);
            bottomPanel.Controls.Add(_cargoSizeLabel, 1, 0);

            // Add controls to form
            _form.Controls.Add(_textBox);
            _form.Controls.Add(bottomPanel);
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

        private void WatchingTimer_Tick(object? sender, EventArgs e)
        {
            if (_watchingLabel == null) return;

            _watchingFrame = (_watchingFrame + 1) % WatchingCargo.Length;
            _watchingLabel.Text = WatchingCargo[_watchingFrame];
        }

        /// <summary>
        /// Update the UI with new cargo data
        /// </summary>
        /// <param name="snapshot">The cargo snapshot to display</param>
        /// <param name="cargoCapacity">The total cargo capacity</param>
        public void UpdateCargoDisplay(CargoSnapshot snapshot, int? cargoCapacity)
        {
            if (_cargoSizeLabel == null) return;

            int count = snapshot.Inventory.Sum(item => item.Count);
            int index = 0;

            // Calculate index based on percentage if capacity is known
            if (cargoCapacity is > 0)
            {
                double percentage = (double)count / cargoCapacity.Value;
                percentage = Math.Clamp(percentage, 0.0, 1.0);

                index = (int)Math.Round(percentage * (CargoSize.Length - 1));
                index = Math.Clamp(index, 0, CargoSize.Length - 1);
            }

            _cargoSizeLabel.Text = $"Hold: {CargoSize[index]}";
        }

        /// <summary>
        /// Append text to the display
        /// </summary>
        /// <param name="text">Text to append</param>
        public void AppendText(string text)
        {
            if (_textBox == null) return;

            string textToAppend = text;
            if (AppConfiguration.UseShipPrefix && AppConfiguration.ShipDesigns.Any())
            {
                var randomShip = AppConfiguration.ShipDesigns[_random.Next(AppConfiguration.ShipDesigns.Count)];

                // Remove trailing newline if it exists, we'll add it back later.
                bool hadTrailingNewline = text.EndsWith(Environment.NewLine);
                string content = hadTrailingNewline ? text.Substring(0, text.Length - Environment.NewLine.Length) : text;

                var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                var prefixedLines = lines.Select(line =>
                    string.IsNullOrWhiteSpace(line) ? line : $"{randomShip}{line}"
                );

                textToAppend = string.Join(Environment.NewLine, prefixedLines);

                if (hadTrailingNewline)
                {
                    textToAppend += Environment.NewLine;
                }
            }

            try
            {
                _textBox.AppendText(textToAppend);
                TrimTextBoxLines();
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error appending text: {ex}");
            }
        }

        /// <summary>
        /// Update the location display on the status bar.
        /// </summary>
        /// <param name="starSystem">The name of the star system.</param>
        public void UpdateLocation(string starSystem)
        {
            _currentLocation = starSystem;

            UpdateFullTitleText();
        }

        /// <summary>
        /// Update the form title
        /// </summary>
        /// <param name="title">New title text</param>
        public void UpdateTitle(string title)
        {
            _baseTitle = title;

            UpdateFullTitleText();
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

            // Control the animation
            if (_watchingTimer != null && _watchingLabel != null)
            {
                if (stopEnabled) // This means monitoring is now active
                {
                    _watchingFrame = 0;
                    _watchingTimer.Start();
                }
                else // Monitoring is stopped
                {
                    _watchingTimer.Stop();
                    _watchingLabel.Text = "";
                }
            }
        }

        private void UpdateFullTitleText()
        {
            if (_form == null) return;

            _form.Text = $"{_baseTitle} - Location: {_currentLocation}";
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

            // Set tray icon from pre-loaded resource
            if (_appIcon != null) _notifyIcon.Icon = _appIcon;
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
            _watchingLabel?.Dispose();
            _cargoSizeLabel?.Dispose();
            _toolTip?.Dispose();
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
            _watchingTimer?.Dispose();
            _privateFonts?.Dispose();
            if (_fontMemoryPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_fontMemoryPtr);
                _fontMemoryPtr = IntPtr.Zero;
            }
            _appIcon?.Dispose();
            _iconStream?.Dispose();
        }
    }
}