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
    public class CargoFormUI : ICargoFormUI
    {
        private ListView? _listView;
        private Button? _startBtn;
        private Button? _stopBtn;
        private Button? _exitBtn;
        private Button? _aboutBtn;
        private Button? _settingsBtn;
        private ToolTip? _toolTip;
        private Font? _verdanaFont;
        private Font? _consolasFont;
        private Font? _animationFont;
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
        private Button? _cargoSizeLabel;
        private Button? _cargoHeaderLabel;
        private MemoryStream? _iconStream;
        private Button? _watchingLabel;
        private System.Windows.Forms.Timer? _watchingTimer;
        private int _watchingFrame = 0;
        private string _currentLocation = "Unknown";

        private string _baseTitle = "";

        // Colors for button states to provide better visual feedback.
        private static readonly Color DefaultButtonBackColor = Color.FromArgb(240, 240, 240);
        private static readonly Color StartButtonActiveColor = Color.FromArgb(232, 245, 233); // A subtle light green
        private static readonly Color StopButtonActiveColor = Color.FromArgb(252, 232, 232); // A subtle light red

        // Ship designs, kept for potential future use.
        private static readonly string[] ShipDesigns =
        {
            "[<=#=>] ", // Hauler
            ">--=--< ", // Fighter
            ">--^--< ", // Interceptor
            "(#####) ", // Freighter
            "<(-O-)> ", // Explorer
            ">--o--< ", // Courier
            " ~<o>~  ", // Alien Ship
            ">-(*)-< ", // Heavy Fighter
            " /_O_\\  ", // Shuttle
            "<==*==> "  // Corvette
        };

        // Our working when we hit start
        private static readonly string[] WatchingCargo = new[]
		{
			"⢄",
			"⢂",
			"⢁",
			" ",
			"⡈",
			"⡐",
			"⡠",
			"⡰",
			"⣠",
			"⣐",
			"⣈",
			"⣁",
			"⣂",
			"⣄",
			"⣆",
			"⣇",
			"⣧",
			"⣷",
			"⣾",
			"⣶",
			"⣼",
			"⣸",
			"⣙",
			"⣉",
			"⣁"
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

        public event EventHandler? StartClicked;

        public event EventHandler? StopClicked;

        public event EventHandler? ExitClicked;

        public event EventHandler? AboutClicked;

        public event EventHandler? SettingsClicked;

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
                // If the icon resource is missing or corrupt, an exception will be thrown.
                // We catch it here to prevent the application from crashing on startup.
                System.Diagnostics.Debug.WriteLine($"[CargoFormUI] Error initializing application icon: {ex.Message}");
                // By leaving _appIcon as null, the form and notify icon will gracefully fall back to using their default icons.
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
                _animationFont = new Font(AppConfiguration.ConsolasFontName, 12f); // Larger font for animation
            }
            catch
            {
                _consolasFont = new Font(FontFamily.GenericMonospace, AppConfiguration.DefaultFontSize);
                _animationFont = new Font(FontFamily.GenericMonospace, 12f); // Fallback for animation font
            }
        }

        private void CreateControls()
        {
            // Main ListView to display cargo items
            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                Font = _verdanaFont,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window, // Use standard window background
                GridLines = false // Cleaner look without grid lines
            };

            // Define columns for the ListView
            _listView.Columns.Add("Commodity", -2, HorizontalAlignment.Left); // -2 makes it auto-size
            _listView.Columns.Add("Count", 80, HorizontalAlignment.Right);

            // Create control buttons
            _startBtn = new Button { Text = "Start", Font = _consolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _stopBtn = new Button { Text = "Stop", Enabled = false, Font = _consolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _exitBtn = new Button { Text = "Exit", Font = _consolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _settingsBtn = new Button { Text = "Settings", Font = _consolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _aboutBtn = new Button { Text = "About", Font = _consolasFont, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            // Apply a modern, flat style to the buttons to make them "pop"
            var buttonsToStyle = new[] { _startBtn, _stopBtn, _exitBtn, _settingsBtn, _aboutBtn };
            foreach (var btn in buttonsToStyle)
            {
                if (btn == null) continue;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0; // We'll draw our own border to keep it consistent
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 241, 251); // Light blue hover
                btn.BackColor = DefaultButtonBackColor;
                btn.Paint += Button_Paint;
            }

            // Set the initial "active" color for the Start button to guide the user.
            if (_startBtn != null)
            {
                _startBtn.BackColor = StartButtonActiveColor;
            }

            // Create ToolTip and assign to buttons
            _toolTip = new ToolTip();
            if (_startBtn != null) _toolTip.SetToolTip(_startBtn, "Start monitoring for cargo changes");
            if (_stopBtn != null) _toolTip.SetToolTip(_stopBtn, "Stop monitoring for cargo changes");
            if (_exitBtn != null) _toolTip.SetToolTip(_exitBtn, "Exit the application");
            if (_settingsBtn != null) _toolTip.SetToolTip(_settingsBtn, "Configure application settings");
            if (_aboutBtn != null) _toolTip.SetToolTip(_aboutBtn, "Show information about the application");

            // Create a "label" for the cargo meter using a styled, disabled button for alignment.
            _cargoSizeLabel = new Button
            {
                Text = CargoSize[0],
                Font = _consolasFont,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(5, 3, 3, 3),
            };

            // Create a "label" for the watching animation using a styled, disabled button.
            // Calculate the max width needed for the animation to prevent layout shifts.
            int animationWidth = 0;
            if (_animationFont != null)
            {
                animationWidth = WatchingCargo.Max(frame => TextRenderer.MeasureText(frame, _animationFont).Width);
            }

            _watchingLabel = new Button
            {
                Text = "",
                Font = _animationFont,
                AutoSize = false, // Must be false to set a fixed size and prevent resizing
                Width = animationWidth > 0 ? animationWidth : 20, // Set fixed width, with a fallback
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(3),
            };

            // Create a "label" for the cargo count header using a styled, disabled button.
            _cargoHeaderLabel = new Button
            {
                Text = "Cargo: 0",
                Font = _verdanaFont, // Use more readable font
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Enabled = true, // Keep enabled to preserve color
                Cursor = Cursors.Default, // Make it look non-interactive
                FlatAppearance = {
                    BorderSize = 0,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Margin = new Padding(0, 3, 0, 3),
            };

            CreateTrayIcon();
        }

        private void Button_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            // Define the border color.
            Color borderColor = Color.FromArgb(0, 120, 215);

            // Draw a 1px solid border inside the button's client rectangle.
            // This ensures the border is always visible and has a consistent color,
            // regardless of the button's focus state.
            ControlPaint.DrawBorder(e.Graphics, btn.ClientRectangle,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid,
                                    borderColor, 1, ButtonBorderStyle.Solid);
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
            if (_form == null || _listView == null || _startBtn == null || 
                _stopBtn == null || _aboutBtn == null || _settingsBtn == null || _exitBtn == null || _watchingLabel == null || _cargoHeaderLabel == null ||
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
                Anchor = AnchorStyles.Left, // Vertically center and align left
            };

            // Add controls to the button panel
            buttonFlowPanel.Controls.Add(_watchingLabel);
            buttonFlowPanel.Controls.Add(_startBtn);
            buttonFlowPanel.Controls.Add(_stopBtn);
            buttonFlowPanel.Controls.Add(_aboutBtn);
            buttonFlowPanel.Controls.Add(_settingsBtn);
            buttonFlowPanel.Controls.Add(_exitBtn);

            // Create a panel for the right-aligned items
            var rightPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Right, // Vertically center and align right
            };
            rightPanel.Controls.Add(_cargoHeaderLabel);
            rightPanel.Controls.Add(_cargoSizeLabel);

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
            bottomPanel.Controls.Add(rightPanel, 1, 0);

            // Add controls to form
            _form.Controls.Add(_listView);
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
            if (_listView == null) return;
            _listView.Items.Clear();
            var welcomeItem = new ListViewItem(AppConfiguration.WelcomeMessage);
            welcomeItem.ForeColor = SystemColors.GrayText;
            _listView.Items.Add(welcomeItem);

            if (_cargoHeaderLabel != null) _cargoHeaderLabel.Text = "Cargo: 0";
        }

        private void WatchingTimer_Tick(object? sender, EventArgs e)
        {
            if (_watchingLabel == null) return;

            _watchingFrame = (_watchingFrame + 1) % WatchingCargo.Length;
            _watchingLabel.Text = WatchingCargo[_watchingFrame];
        }

        public void UpdateCargoHeader(int currentCount, int? capacity)
        {
            if (_cargoHeaderLabel == null) return;

            string headerText = capacity.HasValue
                ? $"Cargo: {currentCount}/{capacity.Value}"
                : $"Cargo: {currentCount}";
            _cargoHeaderLabel.Text = headerText;
        }

        public void UpdateCargoList(CargoSnapshot snapshot)
        {
            if (_listView == null) return;

            _listView.BeginUpdate();
            _listView.Items.Clear();

            if (snapshot.Inventory.Any())
            {
                var sortedInventory = snapshot.Inventory.OrderBy(i => !string.IsNullOrEmpty(i.Localised) ? i.Localised : i.Name);
                foreach (var item in sortedInventory)
                {
                    string displayName = !string.IsNullOrEmpty(item.Localised) ? item.Localised : item.Name;

                    // Capitalize the first letter for consistent display.
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpperInvariant(displayName[0]) + displayName.Substring(1);
                    }

                    var listViewItem = new ListViewItem(displayName);
                    listViewItem.SubItems.Add(item.Count.ToString());
                    _listView.Items.Add(listViewItem);
                }
            }
            else
            {
                var emptyItem = new ListViewItem("Cargo hold is empty.") { ForeColor = SystemColors.GrayText };
                _listView.Items.Add(emptyItem);
            }
            _listView.EndUpdate();
        }

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

            _cargoSizeLabel.Text = CargoSize[index];
        }

        public void UpdateLocation(string starSystem)
        {
            _currentLocation = starSystem;

            UpdateFullTitleText();
        }

        public void UpdateTitle(string title)
        {
            _baseTitle = title;

            UpdateFullTitleText();
        }

        public void SetButtonStates(bool startEnabled, bool stopEnabled)
        {
            if (_startBtn != null)
            {
                _startBtn.Enabled = startEnabled;
                // Use a different background color to indicate this is the primary action.
                _startBtn.BackColor = startEnabled ? StartButtonActiveColor : DefaultButtonBackColor;
            }

            if (_stopBtn != null)
            {
                _stopBtn.Enabled = stopEnabled;
                // Use a different background color to indicate this is the primary action.
                _stopBtn.BackColor = stopEnabled ? StopButtonActiveColor : DefaultButtonBackColor;
            }

            // Also update tray menu items
            if (_trayMenuStart != null) _trayMenuStart.Enabled = startEnabled;
            if (_trayMenuStop != null) _trayMenuStop.Enabled = stopEnabled;

            // Control the animation
            if (_watchingTimer != null && _watchingLabel != null)
            {
                if (stopEnabled) // This means monitoring is now active
                {
                    _watchingFrame = 0;
                    _watchingLabel.Text = WatchingCargo[_watchingFrame];
                    _watchingLabel.ForeColor = Color.Black; // Use a distinct color for visibility
                    _watchingTimer.Start();
                }
                else // Monitoring is stopped
                {
                    _watchingTimer.Stop();
                    _watchingLabel.Text = "";
                    _watchingLabel.ForeColor = SystemColors.ControlText; // Reset to default color
                }
            }
        }

        private void UpdateFullTitleText()
        {
            if (_form == null) return;

            _form.Text = $"{_baseTitle} - Location: {_currentLocation}";
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
            _animationFont?.Dispose();
            _listView?.Dispose();

            // Detach paint handlers to be tidy
            if (_startBtn != null) _startBtn.Paint -= Button_Paint;
            if (_stopBtn != null) _stopBtn.Paint -= Button_Paint;
            if (_exitBtn != null) _exitBtn.Paint -= Button_Paint;
            if (_aboutBtn != null) _aboutBtn.Paint -= Button_Paint;
            if (_settingsBtn != null) _settingsBtn.Paint -= Button_Paint;

            _startBtn?.Dispose();
            _stopBtn?.Dispose();
            _exitBtn?.Dispose();
            _aboutBtn?.Dispose();
            _settingsBtn?.Dispose();
            _watchingLabel?.Dispose();
            _cargoHeaderLabel?.Dispose();
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