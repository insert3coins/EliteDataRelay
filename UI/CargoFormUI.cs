using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;
using EliteCargoMonitor.Models;

namespace EliteCargoMonitor.UI
{
    /// <summary>
    /// UI management class for the cargo form interface
    /// </summary>
    public class CargoFormUI : ICargoFormUI, IDisposable
    {
        private TextBox? _textBox;
        private Button? _startBtn;
        private Button? _stopBtn;
        private Button? _exitBtn;
        private Button? _aboutBtn;
        private Font? _verdanaFont;
        private Form? _form;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _trayMenu;
        private ToolStripMenuItem? _trayMenuShow;
        private ToolStripMenuItem? _trayMenuStart;
        private ToolStripMenuItem? _trayMenuStop;
        private ToolStripMenuItem? _trayMenuExit;

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
        /// Initialize the UI components and layout
        /// </summary>
        /// <param name="form">The main form to initialize</param>
        public void InitializeUI(Form form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _form.Resize += OnFormResize;

            InitializeFonts();
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
        }

        private void InitializeFonts()
        {
            try
            {
                // Initialize Verdana font from embedded resources or fallback to system font
                _verdanaFont = new Font(AppConfiguration.VerdanaFontName, AppConfiguration.DefaultFontSize);
            }
            catch
            {
                // Fallback to default font if custom font fails
                _verdanaFont = new Font(FontFamily.GenericSansSerif, AppConfiguration.DefaultFontSize);
            }
        }

        private void CreateControls()
        {
            // Create main text display
            _textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font(AppConfiguration.ConsolasFontName, AppConfiguration.DefaultFontSize),
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
            _startBtn = new Button { Text = "Start", Height = AppConfiguration.ButtonHeight };
            _stopBtn = new Button { Text = "Stop", Height = AppConfiguration.ButtonHeight, Enabled = false };
            _exitBtn = new Button { Text = "Exit", Height = AppConfiguration.ButtonHeight };
            _aboutBtn = new Button { Text = "About", Height = AppConfiguration.ButtonHeight };

            CreateTrayIcon();
        }

        private void SetupFormProperties()
        {
            if (_form == null) return;

            // Basic form properties
            _form.Text = $"Cargo Monitor – Stopped: {AppConfiguration.CargoPath}";
            _form.Width = AppConfiguration.FormWidth;
            _form.Height = AppConfiguration.FormHeight;
            _form.Padding = Padding.Empty;

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
                _stopBtn == null || _aboutBtn == null || _exitBtn == null) return;

            // Create button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = AppConfiguration.ButtonPanelHeight,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            // Add buttons in order: Start | Stop | About | Exit
            buttonPanel.Controls.Add(_startBtn);
            buttonPanel.Controls.Add(_stopBtn);
            buttonPanel.Controls.Add(_aboutBtn);
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
            string cargoString = FormatCargoString(snapshot, cargoCapacity);
            string entry = $"{cargoString}{Environment.NewLine}";
            
            AppendText(entry);
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
            if (_form != null)
            {
                _form.Text = title;
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

        private string FormatCargoString(CargoSnapshot snapshot, int? cargoCapacity)
        {
            string capacityString = cargoCapacity.HasValue ? $"/{cargoCapacity.Value}" : "";
            string cargoString = $"Total Cargo {snapshot.Count}{capacityString}: ";
            cargoString += string.Join(
                " ",
                snapshot.Inventory.Select(item =>
                    $"{(string.IsNullOrEmpty(item.Localised) ? item.Name : item.Localised)} ({item.Count})"));

            return cargoString;
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
            _textBox?.Dispose();
            _startBtn?.Dispose();
            _stopBtn?.Dispose();
            _exitBtn?.Dispose();
            _aboutBtn?.Dispose();
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
        }
    }
}